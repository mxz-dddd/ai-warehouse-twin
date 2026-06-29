"""Shared adapter utilities for deterministic ingestion outputs."""

from __future__ import annotations

import json
from collections.abc import Callable, Mapping
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from jsonschema import Draft202012Validator

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
        issues.append(
            DataQualityIssue(
                "error",
                file_name,
                row_number,
                f"{column_name} references unknown {label} '{value}'",
            )
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
        issues.append(
            DataQualityIssue(
                "error",
                file_name,
                row_number,
                f"{expected_type} location references unknown location '{location_id}'",
            )
        )
    elif actual_type != expected_type:
        issues.append(
            DataQualityIssue(
                "error",
                file_name,
                row_number,
                f"{location_id} must be a {expected_type} location; got '{actual_type}'",
            )
        )


def safe_int(value: str) -> int:
    try:
        return int(value)
    except ValueError:
        return 0


def write_json(path: Path, document: ScenarioDocument) -> None:
    path.write_text(json.dumps(document, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def write_text(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8")
