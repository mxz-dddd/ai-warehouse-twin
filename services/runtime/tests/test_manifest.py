from __future__ import annotations

import json
import re
from pathlib import Path

import pytest

from runtime_service.manifest import (
    RUN_MANIFEST_SCHEMA_VERSION,
    RunEnvironment,
    RunInput,
    RunManifest,
    RunOutputSummary,
    build_run_manifest,
    deterministic_run_id,
    scenario_content_hash,
)
from runtime_service.models import RunStatus


def test_run_manifest_json_output_is_stable() -> None:
    manifest = RunManifest(
        run_id="dry-run-abc123",
        run_input=RunInput(
            scenario_path="scenario.json",
            scenario_name="scenario.json",
            scenario_content_hash="sha256:abc123",
        ),
        environment=RunEnvironment(),
        runner_name="local-dry-run",
        runner_mode="dry-run",
        status=RunStatus.SUCCEEDED,
        artifact_index_path="artifact-index.json",
        output_summary=RunOutputSummary(
            artifact_index_path="artifact-index.json",
            output_files=("run-manifest.json", "runtime-result.json", "artifact-index.json"),
        ),
    )

    assert manifest.to_json_text() == json.dumps(
        manifest.to_json_dict(), indent=2, sort_keys=True
    ) + "\n"
    assert RunManifest.from_json_dict(manifest.to_json_dict()) == manifest
    assert manifest.to_json_dict()["schema_version"] == RUN_MANIFEST_SCHEMA_VERSION
    assert manifest.to_json_dict()["output_summary"] == {
        "artifact_index_path": "artifact-index.json",
        "output_file_count": 3,
        "output_files": [
            "artifact-index.json",
            "run-manifest.json",
            "runtime-result.json",
        ],
    }


def test_scenario_content_hash_and_run_id_are_stable() -> None:
    scenario_bytes = b'{"scenario_id":"unit"}\n'

    assert scenario_content_hash(scenario_bytes) == scenario_content_hash(scenario_bytes)
    assert deterministic_run_id(
        runner_mode="dry-run", scenario_bytes=scenario_bytes
    ) == deterministic_run_id(runner_mode="dry-run", scenario_bytes=scenario_bytes)
    assert deterministic_run_id(runner_mode="dry-run", scenario_bytes=scenario_bytes).startswith(
        "dry-run-"
    )


def test_built_manifest_has_no_absolute_paths_or_noise(tmp_path: Path) -> None:
    scenario = tmp_path / "scenario.json"
    scenario.write_text('{"scenario_id":"unit"}\n', encoding="utf-8")

    manifest = build_run_manifest(
        scenario_path=scenario,
        runner_name="local-dry-run",
        runner_mode="dry-run",
        status=RunStatus.SUCCEEDED,
        artifact_index_path="artifact-index.json",
        output_files=("runtime-result.json", "artifact-index.json", "run-manifest.json"),
    )
    manifest_text = manifest.to_json_text()

    assert str(tmp_path) not in manifest_text
    assert not re.search(r"\d{4}-\d{2}-\d{2}", manifest_text)
    assert not re.search(r"\b[0-9a-f]{32,}\b", manifest_text)
    assert manifest.run_input.scenario_path == "scenario.json"
    assert manifest.artifact_index_path == "artifact-index.json"


def test_manifest_rejects_absolute_paths(tmp_path: Path) -> None:
    with pytest.raises(ValueError, match="scenario_path must be relative"):
        RunInput(
            scenario_path=str(tmp_path / "scenario.json"),
            scenario_name="scenario.json",
            scenario_content_hash="sha256:abc123",
        )
