from __future__ import annotations

import subprocess
import sys
from pathlib import Path

SERVICE_ROOT = Path(__file__).resolve().parents[1]
REPO_ROOT = Path(__file__).resolve().parents[3]


def test_cli_help_runs() -> None:
    completed = subprocess.run(
        [sys.executable, "-m", "ingestion", "--help"],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )

    assert "print-schema-info" in completed.stdout
    assert "csv-to-scenario" in completed.stdout
    assert "mock-wms-to-scenario" in completed.stdout


def test_cli_print_schema_info_reads_repo_schema() -> None:
    completed = subprocess.run(
        [
            sys.executable,
            "-m",
            "ingestion",
            "print-schema-info",
            "--repo-root",
            str(REPO_ROOT),
        ],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )

    assert "title: WarehouseScenario" in completed.stdout
    assert "schema_version: warehouse-scenario.v0" in completed.stdout


def test_cli_csv_to_scenario_runs(tmp_path: Path) -> None:
    completed = subprocess.run(
        [
            sys.executable,
            "-m",
            "ingestion",
            "csv-to-scenario",
            "--input",
            str(REPO_ROOT / "datasets/ingestion-cases/csv-basic/input"),
            "--output",
            str(tmp_path),
            "--repo-root",
            str(REPO_ROOT),
        ],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )

    assert "scenario.json" in completed.stdout
    assert (tmp_path / "scenario.json").is_file()
    assert (tmp_path / "data-quality-report.md").is_file()


def test_cli_mock_wms_to_scenario_runs(tmp_path: Path) -> None:
    completed = subprocess.run(
        [
            sys.executable,
            "-m",
            "ingestion",
            "mock-wms-to-scenario",
            "--input",
            str(REPO_ROOT / "datasets/ingestion-cases/mock-wms-basic/input"),
            "--output",
            str(tmp_path),
            "--repo-root",
            str(REPO_ROOT),
        ],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )

    assert "scenario.json" in completed.stdout
    assert (tmp_path / "scenario.json").is_file()
    assert (tmp_path / "data-quality-report.md").is_file()
