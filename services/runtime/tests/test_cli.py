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
    assert "inspect-run" in completed.stdout


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
    assert "run-manifest.json" in completed.stdout
    assert (output_dir / "runtime-result.json").is_file()
    assert (output_dir / "artifact-index.json").is_file()
    assert (output_dir / "run-manifest.json").is_file()


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
    assert "run-manifest.json" in completed.stdout
    assert (output_dir / "simcli-plan.json").is_file()
    assert (output_dir / "artifact-index.json").is_file()
    assert (output_dir / "run-manifest.json").is_file()


def test_cli_inspect_run_prints_manifest(tmp_path: Path) -> None:
    scenario = tmp_path / "scenario.json"
    scenario.write_text('{"scenario_id":"cli","schema_version":"warehouse-scenario.v0"}\n')
    output_dir = tmp_path / "out"
    subprocess.run(
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

    completed = subprocess.run(
        [
            sys.executable,
            "-m",
            "runtime_service",
            "inspect-run",
            "--run-dir",
            str(output_dir),
        ],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )

    assert '"schema_version": "runtime-run-manifest.v0"' in completed.stdout
    assert '"run_id": "dry-run-' in completed.stdout


def test_cli_inspect_run_missing_manifest_is_readable(tmp_path: Path) -> None:
    completed = subprocess.run(
        [
            sys.executable,
            "-m",
            "runtime_service",
            "inspect-run",
            "--run-dir",
            str(tmp_path),
        ],
        cwd=SERVICE_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )

    assert completed.returncode == 2
    assert "run manifest not found" in completed.stderr
    assert "Traceback" not in completed.stderr
