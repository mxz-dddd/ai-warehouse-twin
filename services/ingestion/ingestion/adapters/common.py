"""Shared adapter utilities for deterministic ingestion outputs."""

from __future__ import annotations

import json
from collections.abc import Callable, Mapping
from pathlib import Path
from typing import Any

from jsonschema import Draft202012Validator

from ingestion.quality import DataQualityIssue, DataQualityReport
from ingestion.schema import load_scenario_schema
from ingestion.sources import ScenarioDocument

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

VALID_LOCATION_TYPES = {"dock", "pick", "staging"}


def add_issue(
    issues: list[DataQualityIssue],
    *,
    code: str,
    message: str,
    source_file: str,
    row_number: int | None = None,
    field: str | None = None,
    severity: str = "error",
) -> None:
    issues.append(
        DataQualityIssue(
            severity=severity,
            code=code,
            message=message,
            source_file=source_file,
            row_number=row_number,
            field=field,
        )
    )


def build_quality_report(
    *,
    source_name: str,
    input_files: tuple[str, ...],
    record_counts: dict[str, int],
    issues: list[DataQualityIssue],
    skus: set[str],
    location_types: Mapping[str, str],
    inventory: list[dict[str, Any]],
    orders: list[dict[str, Any]],
) -> DataQualityReport:
    return DataQualityReport(
        source_name=source_name,
        input_files=input_files,
        record_counts=record_counts,
        issues=tuple(issues),
        coverage_summary=build_coverage_summary(skus, location_types, inventory, orders),
        scenario_output_summary=build_scenario_output_summary(inventory, orders),
    )


def build_coverage_summary(
    skus: set[str],
    location_types: Mapping[str, str],
    inventory: list[dict[str, Any]],
    orders: list[dict[str, Any]],
) -> dict[str, str]:
    order_skus = {str(order.get("sku_id", "")) for order in orders if order.get("sku_id")}
    inventory_skus = {
        str(item.get("sku_id", "")) for item in inventory if item.get("sku_id")
    }
    inventory_locations = {
        str(item.get("location_id", "")) for item in inventory if item.get("location_id")
    }
    known_locations = set(location_types)
    return {
        "dock_locations": str(_count_location_type(location_types, "dock")),
        "inventory_locations_covered": _coverage_count(inventory_locations, known_locations),
        "inventory_records": str(len(inventory)),
        "inventory_sku_location_pairs": str(
            len(
                {
                    (str(item.get("sku_id", "")), str(item.get("location_id", "")))
                    for item in inventory
                }
            )
        ),
        "inventory_skus_covered": _coverage_count(inventory_skus, skus),
        "locations": str(len(location_types)),
        "order_records": str(len(orders)),
        "order_skus_covered": _coverage_count(order_skus, skus),
        "pick_locations": str(_count_location_type(location_types, "pick")),
        "skus": str(len(skus)),
        "staging_locations": str(_count_location_type(location_types, "staging")),
    }


def build_scenario_output_summary(
    inventory: list[dict[str, Any]],
    orders: list[dict[str, Any]],
) -> dict[str, str]:
    return {
        "inventory_items": str(len(inventory)),
        "orders": str(len(orders)),
        "process_fields": "4",
    }


def require_location_type_coverage(
    location_types: Mapping[str, str],
    source_file: str,
    issues: list[DataQualityIssue],
) -> None:
    for location_type in sorted(VALID_LOCATION_TYPES):
        if not any(item == location_type for item in location_types.values()):
            add_issue(
                issues,
                code="missing_location_type",
                source_file=source_file,
                field="location_type",
                message=f"at least one {location_type} location is required",
            )


def issue_code_for_integer_minimum(field_name: str, minimum: int) -> str:
    if field_name == "quantity" and minimum == 1:
        return "negative_quantity"
    if field_name.endswith("_ms") and minimum == 0:
        return "negative_time"
    if minimum == 1:
        return "invalid_positive_integer"
    return "invalid_non_negative_integer"


class IngestionInputError(Exception):
    """Raised when adapter input cannot be converted into a valid scenario."""

    def __init__(self, report: DataQualityReport, source_label: str) -> None:
        self.report = report
        super().__init__(f"{source_label} ingestion failed with {report.error_count} error(s)")


def write_scenario_outputs(
    output_dir: Path,
    build_outputs: Callable[[], tuple[ScenarioDocument, DataQualityReport]],
    repo_root: Path | None,
    source_label: str,
) -> int:
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        scenario, report = build_outputs()
        if report.error_count:
            raise IngestionInputError(report, source_label)
        Draft202012Validator(load_scenario_schema(repo_root)).validate(scenario)
    except IngestionInputError as error:
        write_text(output_dir / "data-quality-report.md", error.report.render_markdown())
        return 1

    write_json(output_dir / "scenario.json", scenario)
    write_text(output_dir / "data-quality-report.md", report.render_markdown())
    return 0


def build_outbound_scenario(
    config: Mapping[str, str],
    inventory: list[dict[str, Any]],
    orders: list[dict[str, Any]],
) -> ScenarioDocument:
    scenario_id = config.get("scenario_id", "")
    return {
        "schema_version": "warehouse-scenario.v0",
        "scenario_id": scenario_id,
        "seed": safe_int(config.get("seed", "0")),
        "description": config.get("description", ""),
        "outbound": {
            "scenario_id": f"{scenario_id}.outbound",
            "worker_count": safe_int(config.get("worker_count", "0")),
            "dock_count": safe_int(config.get("dock_count", "0")),
            "process": {
                "pick_duration_ms": safe_int(config.get("pick_duration_ms", "0")),
                "stage_duration_ms": safe_int(config.get("stage_duration_ms", "0")),
                "dock_travel_duration_ms": safe_int(config.get("dock_travel_duration_ms", "0")),
                "load_duration_ms": safe_int(config.get("load_duration_ms", "0")),
            },
            "inventory": inventory,
            "orders": orders,
        },
    }


def parse_int(
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
        add_issue(
            issues,
            code="invalid_number_format",
            source_file=file_name,
            row_number=row_number,
            field=field_name,
            message=f"{field_name} must be an integer; got '{value}'",
        )
        return 0

    if parsed < minimum:
        label = "positive" if minimum == 1 else "non-negative"
        add_issue(
            issues,
            code=issue_code_for_integer_minimum(field_name, minimum),
            source_file=file_name,
            row_number=row_number,
            field=field_name,
            message=f"{field_name} must be a {label} integer; got {parsed}",
        )
    return parsed


def require_known(
    value: str,
    known_values: set[str],
    file_name: str,
    row_number: int,
    column_name: str,
    label: str,
    issues: list[DataQualityIssue],
) -> None:
    if value not in known_values:
        code = "unknown_sku" if label == "sku" else "unknown_location"
        add_issue(
            issues,
            code=code,
            source_file=file_name,
            row_number=row_number,
            field=column_name,
            message=f"{column_name} references unknown {label} '{value}'",
        )


def require_known_location_type(
    location_id: str,
    expected_type: str,
    location_types: Mapping[str, str],
    file_name: str,
    row_number: int,
    issues: list[DataQualityIssue],
) -> None:
    actual_type = location_types.get(location_id)
    if actual_type is None:
        add_issue(
            issues,
            code="unknown_location",
            source_file=file_name,
            row_number=row_number,
            field=f"{expected_type}_location_id" if expected_type != "dock" else "dock_id",
            message=f"{expected_type} location references unknown location '{location_id}'",
        )
    elif actual_type != expected_type:
        add_issue(
            issues,
            code="invalid_location_type",
            source_file=file_name,
            row_number=row_number,
            field=f"{expected_type}_location_id" if expected_type != "dock" else "dock_id",
            message=f"{location_id} must be a {expected_type} location; got '{actual_type}'",
        )


def require_text_value(
    value: str,
    *,
    file_name: str,
    row_number: int,
    field_name: str,
    issues: list[DataQualityIssue],
) -> bool:
    if value:
        return True
    add_issue(
        issues,
        code="missing_required_value",
        source_file=file_name,
        row_number=row_number,
        field=field_name,
        message=f"{field_name} must not be empty",
    )
    return False


def _coverage_count(values: set[str], known_values: set[str]) -> str:
    return f"{len(values & known_values)}/{len(values)}"


def _count_location_type(location_types: Mapping[str, str], location_type: str) -> int:
    return sum(1 for item in location_types.values() if item == location_type)


def safe_int(value: str) -> int:
    try:
        return int(value)
    except ValueError:
        return 0


def write_json(path: Path, document: ScenarioDocument) -> None:
    path.write_text(json.dumps(document, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def write_text(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8")
