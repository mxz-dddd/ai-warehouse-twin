"""Structured data-quality reporting primitives for ingestion sources."""

from __future__ import annotations

from dataclasses import dataclass, field

SEVERITY_ORDER = {"error": 0, "warning": 1}


@dataclass(frozen=True)
class QualityIssue:
    severity: str
    code: str
    message: str
    source_file: str
    row_number: int | None = None
    field: str | None = None

    def sort_key(self) -> tuple[int, str, int, str, str, str]:
        return (
            SEVERITY_ORDER.get(self.severity, 99),
            self.source_file,
            self.row_number or 0,
            self.field or "",
            self.code,
            self.message,
        )


@dataclass(frozen=True)
class QualityReport:
    source_name: str
    input_files: tuple[str, ...]
    record_counts: dict[str, int]
    issues: tuple[QualityIssue, ...]
    coverage_summary: dict[str, str] = field(default_factory=dict)
    scenario_output_summary: dict[str, str] = field(default_factory=dict)

    @property
    def sorted_issues(self) -> tuple[QualityIssue, ...]:
        return tuple(sorted(self.issues, key=lambda issue: issue.sort_key()))

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
            table_name = _table_name(file_name)
            lines.append(f"- {file_name}: {self.record_counts.get(table_name, 0)} records")

        lines.extend(["", "## Record Counts"])
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
                "| severity | code | file | row | field | message |",
                "| --- | --- | --- | --- | --- | --- |",
            ]
        )
        if self.issues:
            for issue in self.sorted_issues:
                lines.append(
                    "| "
                    + " | ".join(
                        (
                            _escape_markdown_cell(issue.severity),
                            _escape_markdown_cell(issue.code),
                            _escape_markdown_cell(issue.source_file),
                            str(issue.row_number) if issue.row_number is not None else "-",
                            _escape_markdown_cell(issue.field or "-"),
                            _escape_markdown_cell(issue.message),
                        )
                    )
                    + " |"
                )
        else:
            lines.append("| - | - | - | - | - | None |")

        lines.extend(["", "## Coverage Summary"])
        if self.coverage_summary:
            for key in sorted(self.coverage_summary):
                lines.append(f"- {key}: {self.coverage_summary[key]}")
        else:
            lines.append("- None")

        lines.extend(["", "## Scenario Output Summary"])
        if self.scenario_output_summary:
            for key in sorted(self.scenario_output_summary):
                lines.append(f"- {key}: {self.scenario_output_summary[key]}")
        else:
            lines.append("- None")

        return "\n".join(lines) + "\n"


DataQualityIssue = QualityIssue
DataQualityReport = QualityReport


def _table_name(file_name: str) -> str:
    return file_name.rsplit(".", maxsplit=1)[0]


def _escape_markdown_cell(value: str) -> str:
    return value.replace("\\", "\\\\").replace("|", "\\|").replace("\n", " ")
