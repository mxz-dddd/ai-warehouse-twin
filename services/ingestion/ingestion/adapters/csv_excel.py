"""CSV table adapter for deterministic warehouse scenario ingestion."""

from __future__ import annotations

import csv
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from jsonschema import Draft202012Validator

from ingestion.schema import load_scenario_schema
from ingestion.sources import ScenarioDocument

CSV_TABLE_COLUMNS = {
    "config.csv": ("key", "value"),
    "inventory.csv": ("inventory_id", "sku_id", "quantity", "location_id", "status"),
    "locations.csv": ("location_id", "location_type"),
    "orders.csv": (
        "order_id",
        "warehouse_id",
        "sku_id",
        "quantity",
        "pick_location_id",
        "staging_location_id",
        "dock_id",
        "released_at_ms",
    ),
    "skus.csv": ("sku_id", "name"),
}

CONFIG_KEYS = (
    "scenario_id",
    "seed",
    "description",
    "worker_count",
    "dock_count",
    "pick_duration_ms",
    "stage_duration_ms",
    "dock_travel_duration_ms",
    "load_duration_ms",
)

POSITIVE_CONFIG_KEYS = ("worker_count", "dock_count")
NON_NEGATIVE_CONFIG_KEYS = (
    "seed",
    "pick_duration_ms",
    "stage_duration_ms",
    "dock_travel_duration_ms",
    "load_duration_ms",
)
VALID_STATUSES = {
    "expected",
    "received",
    "qc_hold",
    "available",
    "allocated",
    "picking",
    "picked",
    "consolidating",
    "staged",
    "loaded",
    "shipped",
}


@dataclass(frozen=True)
class DataQualityIssue:
    severity: str
    file_name: str
    row_number: int | None
    message: str

    def render(self) -> str:
        location = self.file_name
        if self.row_number is not None:
            location = f"{location} row {self.row_number}"
        return f"- {self.severity.upper()} {location}: {self.message}"


@dataclass(frozen=True)
class DataQualityReport:
    source_name: str
    input_files: tuple[str, ...]
    record_counts: dict[str, int]
    issues: tuple[DataQualityIssue, ...]

    @property
    def error_count(self) -> int:
        return sum(1 for issue in self.issues if issue.severity == "error")

    @property
    def warning_count(self) -> int:
        return sum(1 for issue in self.issues if issue.severity == "warning")

    @property
    def status(self) -> str:
        return "FAIL" if self.error_count else "PASS"

    def render_markdown(self) -> str:
        lines = [
            "# Data Quality Report",
            "",
            f"Source: {self.source_name}",
            f"Status: {self.status}",
            "",
            "## Input Files",
        ]
        for file_name in self.input_files:
            table_name = Path(file_name).stem
            lines.append(f"- {file_name}: {self.record_counts.get(table_name, 0)} records")

        lines.extend(
            [
                "",
                "## Record Counts",
            ]
        )
        for table_name in sorted(self.record_counts):
            lines.append(f"- {table_name}: {self.record_counts[table_name]}")

        lines.extend(
            [
                "",
                "## Issue Summary",
                f"- warnings: {self.warning_count}",
                f"- errors: {self.error_count}",
                "",
                "## Issues",
            ]
        )
        if self.issues:
            lines.extend(issue.render() for issue in self.issues)
        else:
            lines.append("- None")

        return "\n".join(lines) + "\n"


class IngestionInputError(Exception):
    """Raised when CSV input cannot be converted into a valid scenario."""

    def __init__(self, report: DataQualityReport) -> None:
        self.report = report
        super().__init__(f"CSV ingestion failed with {report.error_count} error(s)")


class CsvExcelIngestSource:
    """Read standard CSV export tables and produce an outbound scenario document.

    Excel files are intentionally not parsed in INGEST-002; the adapter name reserves
    that extension point while keeping the first implementation dependency-light.
    """

    def __init__(self, input_dir: Path) -> None:
        self.input_dir = input_dir

    @property
    def source_name(self) -> str:
        return f"csv-excel:{self.input_dir.name}"

    def describe(self) -> str:
        return "CSV directory adapter; Excel workbook support is reserved for a later card."

    def to_scenario(self) -> ScenarioDocument:
        scenario, report = self._convert()
        if report.error_count:
            raise IngestionInputError(report)
        return scenario

    def build_report(self) -> DataQualityReport:
        _scenario, report = self._convert()
        return report

    def _convert(self) -> tuple[ScenarioDocument, DataQualityReport]:
        issues: list[DataQualityIssue] = []
        rows_by_file = {
            file_name: _read_csv_rows(
                self.input_dir / file_name,
                CSV_TABLE_COLUMNS[file_name],
                issues,
            )
            for file_name in sorted(CSV_TABLE_COLUMNS)
        }
        record_counts = {
            Path(file_name).stem: len(rows)
            for file_name, rows in sorted(rows_by_file.items())
        }

        config = _read_config(rows_by_file["config.csv"], issues)
        skus = _id_set(rows_by_file["skus.csv"], "sku_id", "skus.csv", issues)
        location_types = _location_types(rows_by_file["locations.csv"], issues)

        inventory = _inventory_rows(rows_by_file["inventory.csv"], skus, location_types, issues)
        orders = _order_rows(rows_by_file["orders.csv"], skus, location_types, issues)

        scenario = _scenario_from_rows(config, inventory, orders)
        report = DataQualityReport(
            source_name=self.source_name,
            input_files=tuple(sorted(CSV_TABLE_COLUMNS)),
            record_counts=record_counts,
            issues=tuple(issues),
        )
        return scenario, report


def write_csv_scenario_outputs(
    input_dir: Path,
    output_dir: Path,
    repo_root: Path | None = None,
) -> int:
    output_dir.mkdir(parents=True, exist_ok=True)
    source = CsvExcelIngestSource(input_dir)

    try:
        scenario = source.to_scenario()
        report = source.build_report()
        Draft202012Validator(load_scenario_schema(repo_root)).validate(scenario)
    except IngestionInputError as error:
        _write_text(output_dir / "data-quality-report.md", error.report.render_markdown())
        return 1

    _write_json(output_dir / "scenario.json", scenario)
    _write_text(output_dir / "data-quality-report.md", report.render_markdown())
    return 0


def _read_csv_rows(
    path: Path,
    required_columns: tuple[str, ...],
    issues: list[DataQualityIssue],
) -> list[dict[str, str]]:
    if not path.is_file():
        issues.append(DataQualityIssue("error", path.name, None, "required input file is missing"))
        return []

    with path.open("r", encoding="utf-8", newline="") as csv_file:
        reader = csv.DictReader(csv_file)
        columns = tuple(reader.fieldnames or ())
        missing = [column for column in required_columns if column not in columns]
        if missing:
            issues.append(
                DataQualityIssue(
                    "error",
                    path.name,
                    1,
                    f"missing required column(s): {', '.join(missing)}",
                )
            )
            return []

        return [
            {key: _clean_cell(value) for key, value in row.items() if key is not None}
            for row in reader
        ]


def _read_config(rows: list[dict[str, str]], issues: list[DataQualityIssue]) -> dict[str, str]:
    config: dict[str, str] = {}
    for index, row in enumerate(rows, start=2):
        key = row["key"]
        value = row["value"]
        if not key:
            issues.append(DataQualityIssue("error", "config.csv", index, "key must not be empty"))
            continue
        config[key] = value

    for key in CONFIG_KEYS:
        if key not in config or not config[key]:
            issues.append(
                DataQualityIssue("error", "config.csv", None, f"missing required key '{key}'")
            )

    for key in POSITIVE_CONFIG_KEYS:
        _parse_int(config.get(key, ""), "config.csv", None, key, minimum=1, issues=issues)

    for key in NON_NEGATIVE_CONFIG_KEYS:
        _parse_int(config.get(key, ""), "config.csv", None, key, minimum=0, issues=issues)

    return config


def _id_set(
    rows: list[dict[str, str]],
    id_column: str,
    file_name: str,
    issues: list[DataQualityIssue],
) -> set[str]:
    ids: set[str] = set()
    for index, row in enumerate(rows, start=2):
        value = row[id_column]
        if not value:
            issues.append(
                DataQualityIssue("error", file_name, index, f"{id_column} must not be empty")
            )
            continue
        if value in ids:
            issues.append(
                DataQualityIssue("error", file_name, index, f"duplicate {id_column} '{value}'")
            )
        ids.add(value)
    return ids


def _location_types(
    rows: list[dict[str, str]],
    issues: list[DataQualityIssue],
) -> dict[str, str]:
    locations: dict[str, str] = {}
    for index, row in enumerate(rows, start=2):
        location_id = row["location_id"]
        location_type = row["location_type"]
        if not location_id:
            issues.append(
                DataQualityIssue("error", "locations.csv", index, "location_id must not be empty")
            )
            continue
        if not location_type:
            issues.append(
                DataQualityIssue("error", "locations.csv", index, "location_type must not be empty")
            )
        if location_id in locations:
            issues.append(
                DataQualityIssue(
                    "error",
                    "locations.csv",
                    index,
                    f"duplicate location_id '{location_id}'",
                )
            )
        locations[location_id] = location_type
    return locations


def _inventory_rows(
    rows: list[dict[str, str]],
    skus: set[str],
    location_types: dict[str, str],
    issues: list[DataQualityIssue],
) -> list[dict[str, Any]]:
    inventory: list[dict[str, Any]] = []
    inventory_ids: set[str] = set()
    for index, row in enumerate(rows, start=2):
        inventory_id = row["inventory_id"]
        if inventory_id in inventory_ids:
            issues.append(
                DataQualityIssue(
                    "error",
                    "inventory.csv",
                    index,
                    f"duplicate inventory_id '{inventory_id}'",
                )
            )
        inventory_ids.add(inventory_id)
        _require_known(row["sku_id"], skus, "inventory.csv", index, "sku_id", "sku", issues)
        _require_known(
            row["location_id"],
            set(location_types),
            "inventory.csv",
            index,
            "location_id",
            "location",
            issues,
        )
        if row["status"] not in VALID_STATUSES:
            issues.append(
                DataQualityIssue(
                    "error",
                    "inventory.csv",
                    index,
                    f"status must be one of schema enum values; got '{row['status']}'",
                )
            )
        quantity = _parse_int(
            row["quantity"],
            "inventory.csv",
            index,
            "quantity",
            minimum=1,
            issues=issues,
        )
        inventory.append(
            {
                "inventory_id": inventory_id,
                "sku_id": row["sku_id"],
                "quantity": quantity,
                "location_id": row["location_id"],
                "status": row["status"],
            }
        )

    return sorted(inventory, key=lambda item: str(item["inventory_id"]))


def _order_rows(
    rows: list[dict[str, str]],
    skus: set[str],
    location_types: dict[str, str],
    issues: list[DataQualityIssue],
) -> list[dict[str, Any]]:
    orders: list[dict[str, Any]] = []
    order_ids: set[str] = set()
    for index, row in enumerate(rows, start=2):
        order_id = row["order_id"]
        if order_id in order_ids:
            issues.append(
                DataQualityIssue("error", "orders.csv", index, f"duplicate order_id '{order_id}'")
            )
        order_ids.add(order_id)
        _require_known(row["sku_id"], skus, "orders.csv", index, "sku_id", "sku", issues)
        _require_known_location_type(row["pick_location_id"], "pick", location_types, index, issues)
        _require_known_location_type(
            row["staging_location_id"],
            "staging",
            location_types,
            index,
            issues,
        )
        _require_known_location_type(row["dock_id"], "dock", location_types, index, issues)
        quantity = _parse_int(
            row["quantity"],
            "orders.csv",
            index,
            "quantity",
            minimum=1,
            issues=issues,
        )
        released_at_ms = _parse_int(
            row["released_at_ms"],
            "orders.csv",
            index,
            "released_at_ms",
            minimum=0,
            issues=issues,
        )
        orders.append(
            {
                "order_id": order_id,
                "warehouse_id": row["warehouse_id"],
                "sku_id": row["sku_id"],
                "quantity": quantity,
                "pick_location_id": row["pick_location_id"],
                "staging_location_id": row["staging_location_id"],
                "dock_id": row["dock_id"],
                "released_at_ms": released_at_ms,
            }
        )

    return sorted(orders, key=lambda item: str(item["order_id"]))


def _scenario_from_rows(
    config: dict[str, str],
    inventory: list[dict[str, Any]],
    orders: list[dict[str, Any]],
) -> ScenarioDocument:
    scenario_id = config.get("scenario_id", "")
    return {
        "schema_version": "warehouse-scenario.v0",
        "scenario_id": scenario_id,
        "seed": _safe_int(config.get("seed", "0")),
        "description": config.get("description", ""),
        "outbound": {
            "scenario_id": f"{scenario_id}.outbound",
            "worker_count": _safe_int(config.get("worker_count", "0")),
            "dock_count": _safe_int(config.get("dock_count", "0")),
            "process": {
                "pick_duration_ms": _safe_int(config.get("pick_duration_ms", "0")),
                "stage_duration_ms": _safe_int(config.get("stage_duration_ms", "0")),
                "dock_travel_duration_ms": _safe_int(config.get("dock_travel_duration_ms", "0")),
                "load_duration_ms": _safe_int(config.get("load_duration_ms", "0")),
            },
            "inventory": inventory,
            "orders": orders,
        },
    }


def _require_known(
    value: str,
    known_values: set[str],
    file_name: str,
    row_number: int,
    column_name: str,
    label: str,
    issues: list[DataQualityIssue],
) -> None:
    if value not in known_values:
        issues.append(
            DataQualityIssue(
                "error",
                file_name,
                row_number,
                f"{column_name} references unknown {label} '{value}'",
            )
        )


def _require_known_location_type(
    location_id: str,
    expected_type: str,
    location_types: dict[str, str],
    row_number: int,
    issues: list[DataQualityIssue],
) -> None:
    actual_type = location_types.get(location_id)
    if actual_type is None:
        issues.append(
            DataQualityIssue(
                "error",
                "orders.csv",
                row_number,
                f"{expected_type} location references unknown location '{location_id}'",
            )
        )
    elif actual_type != expected_type:
        issues.append(
            DataQualityIssue(
                "error",
                "orders.csv",
                row_number,
                f"{location_id} must be a {expected_type} location; got '{actual_type}'",
            )
        )


def _parse_int(
    value: str,
    file_name: str,
    row_number: int | None,
    field_name: str,
    minimum: int,
    issues: list[DataQualityIssue],
) -> int:
    try:
        parsed = int(value)
    except ValueError:
        issues.append(
            DataQualityIssue(
                "error",
                file_name,
                row_number,
                f"{field_name} must be an integer; got '{value}'",
            )
        )
        return 0

    if parsed < minimum:
        label = "positive" if minimum == 1 else "non-negative"
        issues.append(
            DataQualityIssue(
                "error",
                file_name,
                row_number,
                f"{field_name} must be a {label} integer; got {parsed}",
            )
        )
    return parsed


def _safe_int(value: str) -> int:
    try:
        return int(value)
    except ValueError:
        return 0


def _clean_cell(value: str | None) -> str:
    return "" if value is None else value.strip()


def _write_json(path: Path, document: ScenarioDocument) -> None:
    path.write_text(json.dumps(document, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def _write_text(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8")
