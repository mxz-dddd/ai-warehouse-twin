"""Deterministic Sim.Cli command planning without executing dotnet."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from runtime_service.artifacts import ArtifactRegistry
from runtime_service.external import CommandSpec
from runtime_service.manifest import RUN_MANIFEST_FILE, build_run_manifest
from runtime_service.models import RunStatus
from runtime_service.runner import ARTIFACT_INDEX_FILE
from runtime_service.store import write_manifest_to_dir

SIMCLI_PLAN_FILE = "simcli-plan.json"
SIMCLI_RUNNER_NAME = "simcli"
SIMCLI_DRY_PLAN_MODE = "dry-plan"


@dataclass(frozen=True)
class SimCliPlan:
    mode: str
    command: CommandSpec
    scenario_path: str
    output_path: str

    def to_json_dict(self) -> dict[str, object]:
        return {
            "command": self.command.to_json_dict(),
            "mode": self.mode,
            "output_path": self.output_path,
            "scenario_path": self.scenario_path,
            "schema_version": "runtime-simcli-plan.v0",
        }


class SimCliPlanner:
    def __init__(
        self,
        *,
        working_directory: str = ".",
        project_path: str = "src/Sim.Cli",
        timeout_seconds: int = 60,
    ) -> None:
        self._working_directory = working_directory
        self._project_path = project_path
        self._timeout_seconds = timeout_seconds

    def plan(
        self,
        *,
        scenario_path: Path,
        output_dir: Path,
        mode: str = "dry-plan",
    ) -> SimCliPlan:
        scenario_name = scenario_path.name
        output_path = "simcli-run-artifact.json"
        command = CommandSpec(
            executable="dotnet",
            args=(
                "run",
                "--project",
                self._project_path,
                "--",
                "export-artifact",
                scenario_name,
                "-o",
                output_path,
            ),
            working_directory=self._working_directory,
            timeout_seconds=self._timeout_seconds,
        )
        return SimCliPlan(
            mode=mode,
            command=command,
            scenario_path=scenario_name,
            output_path=output_path,
        )


def write_simcli_plan(
    *,
    scenario_path: Path,
    output_dir: Path,
    mode: str = SIMCLI_DRY_PLAN_MODE,
    planner: SimCliPlanner | None = None,
) -> SimCliPlan:
    if not scenario_path.is_file():
        raise FileNotFoundError(f"scenario file not found: {scenario_path}")
    output_dir.mkdir(parents=True, exist_ok=True)
    plan = (planner or SimCliPlanner()).plan(
        scenario_path=scenario_path,
        output_dir=output_dir,
        mode=mode,
    )
    _write_json(output_dir / SIMCLI_PLAN_FILE, plan.to_json_dict())
    manifest = build_run_manifest(
        scenario_path=scenario_path,
        runner_name=SIMCLI_RUNNER_NAME,
        runner_mode=mode,
        status=RunStatus.SUCCEEDED,
        artifact_index_path=ARTIFACT_INDEX_FILE,
        output_files=(SIMCLI_PLAN_FILE, ARTIFACT_INDEX_FILE, RUN_MANIFEST_FILE),
    )
    write_manifest_to_dir(output_dir, manifest)

    registry = ArtifactRegistry(output_dir)
    registry.register("simcli-plan", "simcli-plan", SIMCLI_PLAN_FILE)
    registry.register("run-manifest", "run-manifest", RUN_MANIFEST_FILE)
    registry.register("artifact-index", "artifact-index", ARTIFACT_INDEX_FILE)
    _write_json(output_dir / ARTIFACT_INDEX_FILE, registry.to_json_dict())
    return plan


def _write_json(path: Path, document: dict[str, object]) -> None:
    path.write_text(json.dumps(document, indent=2, sort_keys=True) + "\n", encoding="utf-8")
