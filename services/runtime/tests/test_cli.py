from __future__ import annotations

import subprocess
import sys
from pathlib import Path

SERVICE_ROOT = Path(__file__).resolve().parents[1]


def test_cli_help_runs() -> None:
    completed = subprocess.run(
        [sys.executable, "-m", "runtime_service", "--help"],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )

    assert "run-dry" in completed.stdout
    assert "plan-simcli" in completed.stdout


def test_cli_run_dry_writes_outputs(tmp_path: Path) -> None:
    scenario = tmp_path / "scenario.json"
    scenario.write_text('{"scenario_id":"cli","schema_version":"warehouse-scenario.v0"}\n')
    output_dir = tmp_path / "out"

    completed = subprocess.run(
        [
            sys.executable,
            "-m",
            "runtime_service",
            "run-dry",
            "--scenario",
            str(scenario),
            "--output",
            str(output_dir),
        ],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )

    assert "status: succeeded" in completed.stdout
    assert "runtime-result.json" in completed.stdout
    assert (output_dir / "runtime-result.json").is_file()
    assert (output_dir / "artifact-index.json").is_file()


def test_cli_plan_simcli_writes_outputs(tmp_path: Path) -> None:
    scenario = tmp_path / "scenario.json"
    scenario.write_text('{"scenario_id":"cli","schema_version":"warehouse-scenario.v0"}\n')
    output_dir = tmp_path / "out"

    completed = subprocess.run(
        [
            sys.executable,
            "-m",
            "runtime_service",
            "plan-simcli",
            "--scenario",
            str(scenario),
            "--output",
            str(output_dir),
        ],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )

    assert "mode: dry-plan" in completed.stdout
    assert "simcli-plan.json" in completed.stdout
    assert (output_dir / "simcli-plan.json").is_file()
    assert (output_dir / "artifact-index.json").is_file()
