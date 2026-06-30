"""CSV table adapter for deterministic warehouse scenario ingestion."""

from __future__ import annotations

import csv
from pathlib import Path
from typing import Any

from ingestion.adapters.common import (
    CONFIG_KEYS,
    NON_NEGATIVE_CONFIG_KEYS,
    POSITIVE_CONFIG_KEYS,
    VALID_LOCATION_TYPES,
    VALID_STATUSES,
    DataQualityIssue,
    DataQualityReport,
    IngestionInputError,
    add_issue,
    build_outbound_scenario,
    build_quality_report,
    parse_int,
    require_known,
    require_known_location_type,
    require_location_type_coverage,
    require_text_value,
    write_scenario_outputs,
)
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
            raise IngestionInputError(report, "CSV")
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
        report = build_quality_report(
            source_name=self.source_name,
            input_files=tuple(sorted(CSV_TABLE_COLUMNS)),
            record_counts=record_counts,
            issues=issues,
            skus=skus,
            location_types=location_types,
            inventory=inventory,
            orders=orders,
        )
        return scenario, report


def write_csv_scenario_outputs(
    input_dir: Path,
    output_dir: Path,
    repo_root: Path | None = None,
) -> int:
    output_dir.mkdir(parents=True, exist_ok=True)
    source = CsvExcelIngestSource(input_dir)
    return write_scenario_outputs(output_dir, source._convert, repo_root, "CSV")


def _read_csv_rows(
    path: Path,
    required_columns: tuple[str, ...],
    issues: list[DataQualityIssue],
) -> list[dict[str, str]]:
    if not path.is_file():
        add_issue(
            issues,
            code="missing_required_file",
            source_file=path.name,
            message="required input file is missing",
        )
        return []

    with path.open("r", encoding="utf-8", newline="") as csv_file:
        reader = csv.DictReader(csv_file)
        columns = tuple(reader.fieldnames or ())
        missing = [column for column in required_columns if column not in columns]
        if missing:
            for column in missing:
                add_issue(
                    issues,
                    code="missing_required_column",
                    source_file=path.name,
                    row_number=1,
                    field=column,
                    message=f"missing required column '{column}'",
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
        if not require_text_value(
            key,
            file_name="config.csv",
            row_number=index,
            field_name="key",
            issues=issues,
        ):
            continue
        config[key] = value

    for key in CONFIG_KEYS:
        if key not in config or not config[key]:
            add_issue(
                issues,
                code="missing_required_field",
                source_file="config.csv",
                field=key,
                message=f"missing required key '{key}'",
            )

    for key in POSITIVE_CONFIG_KEYS:
        parse_int(config.get(key, ""), "config.csv", None, key, minimum=1, issues=issues)

    for key in NON_NEGATIVE_CONFIG_KEYS:
        parse_int(config.get(key, ""), "config.csv", None, key, minimum=0, issues=issues)

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
        if not require_text_value(
            value,
            file_name=file_name,
            row_number=index,
            field_name=id_column,
            issues=issues,
        ):
            continue
        if value in ids:
            add_issue(
                issues,
                code=f"duplicate_{id_column}",
                source_file=file_name,
                row_number=index,
                field=id_column,
                message=f"duplicate {id_column} '{value}'",
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
        if not require_text_value(
            location_id,
            file_name="locations.csv",
            row_number=index,
            field_name="location_id",
            issues=issues,
        ):
            continue
        if require_text_value(
            location_type,
            file_name="locations.csv",
            row_number=index,
            field_name="location_type",
            issues=issues,
        ) and location_type not in VALID_LOCATION_TYPES:
            add_issue(
                issues,
                code="invalid_location_type",
                source_file="locations.csv",
                row_number=index,
                field="location_type",
                message=(
                    "location_type must be one of dock, pick, staging; "
                    f"got '{location_type}'"
                ),
            )
        if location_id in locations:
            add_issue(
                issues,
                code="duplicate_location_id",
                source_file="locations.csv",
                row_number=index,
                field="location_id",
                message=f"duplicate location_id '{location_id}'",
            )
        locations[location_id] = location_type
    require_location_type_coverage(locations, "locations.csv", issues)
    return locations


def _inventory_rows(
    rows: list[dict[str, str]],
    skus: set[str],
    location_types: dict[str, str],
    issues: list[DataQualityIssue],
) -> list[dict[str, Any]]:
    inventory: list[dict[str, Any]] = []
    inventory_ids: set[str] = set()
    sku_location_pairs: set[tuple[str, str]] = set()
    for index, row in enumerate(rows, start=2):
        inventory_id = row["inventory_id"]
        require_text_value(
            inventory_id,
            file_name="inventory.csv",
            row_number=index,
            field_name="inventory_id",
            issues=issues,
        )
        if inventory_id in inventory_ids:
            add_issue(
                issues,
                code="duplicate_inventory_id",
                source_file="inventory.csv",
                row_number=index,
                field="inventory_id",
                message=f"duplicate inventory_id '{inventory_id}'",
            )
        inventory_ids.add(inventory_id)
        require_text_value(
            row["sku_id"],
            file_name="inventory.csv",
            row_number=index,
            field_name="sku_id",
            issues=issues,
        )
        require_text_value(
            row["location_id"],
            file_name="inventory.csv",
            row_number=index,
            field_name="location_id",
            issues=issues,
        )
        require_text_value(
            row["status"],
            file_name="inventory.csv",
            row_number=index,
            field_name="status",
            issues=issues,
        )
        require_text_value(
            row["quantity"],
            file_name="inventory.csv",
            row_number=index,
            field_name="quantity",
            issues=issues,
        )
        require_known(row["sku_id"], skus, "inventory.csv", index, "sku_id", "sku", issues)
        require_known(
            row["location_id"],
            set(location_types),
            "inventory.csv",
            index,
            "location_id",
            "location",
            issues,
        )
        if row["status"] not in VALID_STATUSES:
            add_issue(
                issues,
                code="invalid_enum_value",
                source_file="inventory.csv",
                row_number=index,
                field="status",
                message=f"status must be one of schema enum values; got '{row['status']}'",
            )
        sku_location_pair = (row["sku_id"], row["location_id"])
        if sku_location_pair in sku_location_pairs:
            add_issue(
                issues,
                code="duplicate_inventory_sku_location",
                source_file="inventory.csv",
                row_number=index,
                field="sku_id,location_id",
                message=(
                    "duplicate inventory sku/location pair "
                    f"'{row['sku_id']}'/'{row['location_id']}'"
                ),
            )
        sku_location_pairs.add(sku_location_pair)
        quantity = parse_int(
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
        for field_name in (
            "order_id",
            "warehouse_id",
            "sku_id",
            "pick_location_id",
            "staging_location_id",
            "dock_id",
            "quantity",
            "released_at_ms",
        ):
            require_text_value(
                row[field_name],
                file_name="orders.csv",
                row_number=index,
                field_name=field_name,
                issues=issues,
            )
        if order_id in order_ids:
            add_issue(
                issues,
                code="duplicate_order_id",
                source_file="orders.csv",
                row_number=index,
                field="order_id",
                message=f"duplicate order_id '{order_id}'",
            )
        order_ids.add(order_id)
        require_known(row["sku_id"], skus, "orders.csv", index, "sku_id", "sku", issues)
        require_known_location_type(
            row["pick_location_id"],
            "pick",
            location_types,
            "orders.csv",
            index,
            issues,
        )
        require_known_location_type(
            row["staging_location_id"],
            "staging",
            location_types,
            "orders.csv",
            index,
            issues,
        )
        require_known_location_type(
            row["dock_id"],
            "dock",
            location_types,
            "orders.csv",
            index,
            issues,
        )
        quantity = parse_int(
            row["quantity"],
            "orders.csv",
            index,
            "quantity",
            minimum=1,
            issues=issues,
        )
        released_at_ms = parse_int(
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
    return build_outbound_scenario(config, inventory, orders)


def _clean_cell(value: str | None) -> str:
    return "" if value is None else value.strip()
