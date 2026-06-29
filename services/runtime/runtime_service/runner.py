"""Local deterministic dry-run runner."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from runtime_service.artifacts import ArtifactRegistry
from runtime_service.models import RunJob, RunRequest, RunResult, RunStatus

RUNTIME_RESULT_FILE = "runtime-result.json"
ARTIFACT_INDEX_FILE = "artifact-index.json"


class LocalDryRunRunner:
    runner_name = "local-dry-run"

    def run(self, request: RunRequest) -> RunResult:
        scenario_bytes = _read_scenario_bytes(request.scenario_path)
        request.output_dir.mkdir(parents=True, exist_ok=True)

        job = RunJob(job_id=_job_id(scenario_bytes), request=request)
        job.mark_running()

        result = RunResult(
            job_id=job.job_id,
            status=RunStatus.SUCCEEDED,
            scenario_name=request.scenario_name,
            runner_name=self.runner_name,
            artifacts=(RUNTIME_RESULT_FILE, ARTIFACT_INDEX_FILE),
            message="dry run completed without invoking simulation core",
        )
        _write_json(request.output_dir / RUNTIME_RESULT_FILE, result.to_json_dict())

        registry = ArtifactRegistry(request.output_dir)
        registry.register("runtime-result", "runtime-result", RUNTIME_RESULT_FILE)
        registry.register("artifact-index", "artifact-index", ARTIFACT_INDEX_FILE)
        _write_json(request.output_dir / ARTIFACT_INDEX_FILE, registry.to_json_dict())

        job.succeed(result)
        return result


def run_dry(scenario_path: Path, output_dir: Path) -> RunResult:
    return LocalDryRunRunner().run(
        RunRequest(
            scenario_path=scenario_path,
            output_dir=output_dir,
        )
    )


def _read_scenario_bytes(path: Path) -> bytes:
    if not path.is_file():
        raise FileNotFoundError(f"scenario file not found: {path}")
    return path.read_bytes()


def _job_id(scenario_bytes: bytes) -> str:
    digest = hashlib.sha256(scenario_bytes).hexdigest()[:16]
    return f"dry-run-{digest}"


def _write_json(path: Path, document: dict[str, object]) -> None:
    path.write_text(json.dumps(document, indent=2, sort_keys=True) + "\n", encoding="utf-8")
