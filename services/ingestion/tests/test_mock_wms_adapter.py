from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

from jsonschema import Draft202012Validator

from ingestion.adapters import MockWmsIngestSource, write_mock_wms_scenario_outputs
from ingestion.adapters.common import IngestionInputError
from ingestion.schema import load_scenario_schema
from ingestion.sources import IngestSource

REPO_ROOT = Path(__file__).resolve().parents[3]
SERVICE_ROOT = Path(__file__).resolve().parents[1]
MOCK_BASIC = REPO_ROOT / "datasets/ingestion-cases/mock-wms-basic"
MOCK_INVALID = REPO_ROOT / "datasets/ingestion-cases/mock-wms-invalid-reference"


def test_mock_wms_fixture_converts_to_schema_valid_scenario(tmp_path: Path) -> None:
    exit_code = write_mock_wms_scenario_outputs(MOCK_BASIC / "input", tmp_path, REPO_ROOT)

    assert exit_code == 0
    scenario = json.loads((tmp_path / "scenario.json").read_text(encoding="utf-8"))
    Draft202012Validator(load_scenario_schema(REPO_ROOT)).validate(scenario)


def test_mock_wms_fixture_outputs_match_golden(tmp_path: Path) -> None:
    exit_code = write_mock_wms_scenario_outputs(MOCK_BASIC / "input", tmp_path, REPO_ROOT)

    assert exit_code == 0
    assert (tmp_path / "scenario.json").read_bytes() == (
        MOCK_BASIC / "expected/scenario.json"
    ).read_bytes()
    assert (tmp_path / "data-quality-report.md").read_bytes() == (
        MOCK_BASIC / "expected/data-quality-report.md"
    ).read_bytes()


def test_mock_wms_adapter_is_ingest_source() -> None:
    source: IngestSource = MockWmsIngestSource(MOCK_BASIC / "input")

    assert source.source_name == "mock-wms:input"
    assert "Mock WMS JSON directory adapter" in source.describe()
    assert source.to_scenario()["scenario_id"] == "mock-wms-basic-outbound"


def test_invalid_mock_wms_writes_readable_report(tmp_path: Path) -> None:
    exit_code = write_mock_wms_scenario_outputs(MOCK_INVALID / "input", tmp_path, REPO_ROOT)
    report = (tmp_path / "data-quality-report.md").read_text(encoding="utf-8")

    assert exit_code == 1
    assert not (tmp_path / "scenario.json").exists()
    assert "Status: FAIL" in report
    assert "| error | unknown_location | inventory.json | 1 | location_id |" in report
    assert "| error | negative_quantity | inventory.json | 1 | quantity |" in report
    assert "| error | unknown_sku | orders.json | 1 | sku_id |" in report
    assert "| error | invalid_location_type | orders.json | 1 | pick_location_id |" in report
    assert "| error | invalid_location_type | orders.json | 1 | staging_location_id |" in report
    assert "| error | unknown_location | orders.json | 1 | dock_id |" in report
    assert "| error | missing_location_type | locations.json | - | location_type |" in report


def test_invalid_mock_wms_source_raises_report_error() -> None:
    source = MockWmsIngestSource(MOCK_INVALID / "input")

    try:
        source.to_scenario()
    except IngestionInputError as error:
        assert error.report.error_count == 9
        assert {issue.code for issue in error.report.issues} >= {
            "invalid_location_type",
            "missing_location_type",
            "negative_quantity",
            "unknown_location",
            "unknown_sku",
        }
    else:
        raise AssertionError("expected invalid Mock WMS input to raise IngestionInputError")


def test_mock_wms_to_scenario_cli_reports_invalid_input(tmp_path: Path) -> None:
    completed = subprocess.run(
        [
            sys.executable,
            "-m",
            "ingestion",
            "mock-wms-to-scenario",
            "--input",
            str(MOCK_INVALID / "input"),
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
    assert "Mock WMS ingestion failed" in completed.stderr
    assert "Traceback" not in completed.stderr
    assert (tmp_path / "data-quality-report.md").is_file()
    assert "invalid_location_type" in (tmp_path / "data-quality-report.md").read_text(
        encoding="utf-8"
    )


def test_mock_wms_outputs_are_byte_stable(tmp_path: Path) -> None:
    first = tmp_path / "first"
    second = tmp_path / "second"

    assert write_mock_wms_scenario_outputs(MOCK_BASIC / "input", first, REPO_ROOT) == 0
    assert write_mock_wms_scenario_outputs(MOCK_BASIC / "input", second, REPO_ROOT) == 0

    assert (first / "scenario.json").read_bytes() == (second / "scenario.json").read_bytes()
    assert (first / "data-quality-report.md").read_bytes() == (
        second / "data-quality-report.md"
    ).read_bytes()
