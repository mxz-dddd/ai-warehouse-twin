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
