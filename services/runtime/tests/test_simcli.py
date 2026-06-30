from __future__ import annotations

import json
import re
from pathlib import Path

from runtime_service.simcli import SimCliPlanner, write_simcli_plan


def test_simcli_planner_generates_stable_plan(tmp_path: Path) -> None:
    scenario = tmp_path / "warehouse.json"
    scenario.write_text("{}\n", encoding="utf-8")

    plan = SimCliPlanner().plan(scenario_path=scenario, output_dir=tmp_path / "out")

    assert plan.to_json_dict() == {
        "command": {
            "args": [
                "run",
                "--project",
                "src/Sim.Cli",
                "--",
                "export-artifact",
                "warehouse.json",
                "-o",
                "simcli-run-artifact.json",
            ],
            "executable": "dotnet",
            "timeout_seconds": 60,
            "working_directory": ".",
        },
        "mode": "dry-plan",
        "output_path": "simcli-run-artifact.json",
        "scenario_path": "warehouse.json",
        "schema_version": "runtime-simcli-plan.v0",
    }


def test_write_simcli_plan_outputs_are_byte_stable(tmp_path: Path) -> None:
    scenario = tmp_path / "scenario.json"
    scenario.write_text("{}\n", encoding="utf-8")
    first = tmp_path / "first"
    second = tmp_path / "second"

    write_simcli_plan(scenario_path=scenario, output_dir=first)
    write_simcli_plan(scenario_path=scenario, output_dir=second)

    assert (first / "simcli-plan.json").read_bytes() == (
        second / "simcli-plan.json"
    ).read_bytes()
    assert (first / "artifact-index.json").read_bytes() == (
        second / "artifact-index.json"
    ).read_bytes()


def test_simcli_plan_output_has_no_local_noise(tmp_path: Path) -> None:
    scenario = tmp_path / "scenario.json"
    scenario.write_text("{}\n", encoding="utf-8")
    output_dir = tmp_path / "out"

    write_simcli_plan(scenario_path=scenario, output_dir=output_dir)

    plan_text = (output_dir / "simcli-plan.json").read_text(encoding="utf-8")
    plan = json.loads(plan_text)

    assert str(tmp_path) not in plan_text
    assert not re.search(r"\d{4}-\d{2}-\d{2}", plan_text)
    assert not re.search(r"\b[0-9a-f]{32,}\b", plan_text)
    assert plan["command"]["executable"] == "dotnet"
