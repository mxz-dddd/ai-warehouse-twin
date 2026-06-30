from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

from jsonschema import Draft202012Validator

from ingestion.adapters import CsvExcelIngestSource, IngestionInputError, write_csv_scenario_outputs
from ingestion.schema import load_scenario_schema
from ingestion.sources import IngestSource

REPO_ROOT = Path(__file__).resolve().parents[3]
SERVICE_ROOT = Path(__file__).resolve().parents[1]
CSV_BASIC = REPO_ROOT / "datasets/ingestion-cases/csv-basic"
CSV_INVALID = REPO_ROOT / "datasets/ingestion-cases/csv-invalid-negative-quantity"
CSV_INVALID_MISSING_COLUMN = REPO_ROOT / "datasets/ingestion-cases/csv-invalid-missing-column"


def test_csv_fixture_converts_to_schema_valid_scenario(tmp_path: Path) -> None:
    exit_code = write_csv_scenario_outputs(CSV_BASIC / "input", tmp_path, REPO_ROOT)

    assert exit_code == 0
    scenario = json.loads((tmp_path / "scenario.json").read_text(encoding="utf-8"))
    Draft202012Validator(load_scenario_schema(REPO_ROOT)).validate(scenario)


def test_csv_fixture_outputs_match_golden(tmp_path: Path) -> None:
    exit_code = write_csv_scenario_outputs(CSV_BASIC / "input", tmp_path, REPO_ROOT)

    assert exit_code == 0
    assert (tmp_path / "scenario.json").read_bytes() == (
        CSV_BASIC / "expected/scenario.json"
    ).read_bytes()
    assert (tmp_path / "data-quality-report.md").read_bytes() == (
        CSV_BASIC / "expected/data-quality-report.md"
    ).read_bytes()


def test_csv_adapter_is_ingest_source() -> None:
    source: IngestSource = CsvExcelIngestSource(CSV_BASIC / "input")

    assert source.source_name == "csv-excel:input"
    assert "CSV directory adapter" in source.describe()
    assert source.to_scenario()["scenario_id"] == "csv-basic-outbound"


def test_invalid_csv_writes_readable_report(tmp_path: Path) -> None:
    exit_code = write_csv_scenario_outputs(CSV_INVALID / "input", tmp_path, REPO_ROOT)
    report = (tmp_path / "data-quality-report.md").read_text(encoding="utf-8")

    assert exit_code == 1
    assert not (tmp_path / "scenario.json").exists()
    assert "Status: FAIL" in report
    assert "| error | negative_quantity | inventory.csv | 2 | quantity |" in report
    assert "| error | unknown_sku | orders.csv | 2 | sku_id |" in report
    assert "| error | unknown_location | orders.csv | 2 | dock_id |" in report
    assert "| error | negative_time | orders.csv | 2 | released_at_ms |" in report
    assert "| error | missing_location_type | locations.csv | - | location_type |" in report


def test_invalid_csv_source_raises_report_error() -> None:
    source = CsvExcelIngestSource(CSV_INVALID / "input")

    try:
        source.to_scenario()
    except IngestionInputError as error:
        assert error.report.error_count == 5
        assert {issue.code for issue in error.report.issues} >= {
            "negative_quantity",
            "negative_time",
            "unknown_location",
            "unknown_sku",
        }
    else:
        raise AssertionError("expected invalid CSV input to raise IngestionInputError")


def test_csv_missing_column_reports_stable_issue_code(tmp_path: Path) -> None:
    exit_code = write_csv_scenario_outputs(
        CSV_INVALID_MISSING_COLUMN / "input",
        tmp_path,
        REPO_ROOT,
    )
    report = (tmp_path / "data-quality-report.md").read_text(encoding="utf-8")

    assert exit_code == 1
    assert "missing_required_column" in report
    assert "| error | missing_required_column | orders.csv | 1 | dock_id |" in report


def test_csv_to_scenario_cli_reports_invalid_input(tmp_path: Path) -> None:
    completed = subprocess.run(
        [
            sys.executable,
            "-m",
            "ingestion",
            "csv-to-scenario",
            "--input",
            str(CSV_INVALID / "input"),
            "--output",
            str(tmp_path),
            "--repo-root",
            str(REPO_ROOT),
        ],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )

    assert completed.returncode == 1
    assert "CSV ingestion failed" in completed.stderr
    assert "Traceback" not in completed.stderr
    assert (tmp_path / "data-quality-report.md").is_file()
    assert "negative_quantity" in (tmp_path / "data-quality-report.md").read_text(
        encoding="utf-8"
    )


def test_csv_outputs_are_byte_stable(tmp_path: Path) -> None:
    first = tmp_path / "first"
    second = tmp_path / "second"

    assert write_csv_scenario_outputs(CSV_BASIC / "input", first, REPO_ROOT) == 0
    assert write_csv_scenario_outputs(CSV_BASIC / "input", second, REPO_ROOT) == 0

    assert (first / "scenario.json").read_bytes() == (second / "scenario.json").read_bytes()
    assert (first / "data-quality-report.md").read_bytes() == (
        second / "data-quality-report.md"
    ).read_bytes()
