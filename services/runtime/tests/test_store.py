from __future__ import annotations

from pathlib import Path

import pytest

from runtime_service.manifest import (
    RunEnvironment,
    RunInput,
    RunManifest,
    RunOutputSummary,
)
from runtime_service.models import RunStatus
from runtime_service.store import LocalRunStore


def test_local_run_store_creates_stable_run_dir(tmp_path: Path) -> None:
    store = LocalRunStore(tmp_path)

    first = store.create_run_dir("dry-run-abc123")
    second = store.create_run_dir("dry-run-abc123")

    assert first == second
    assert first == tmp_path / "dry-run-abc123"


def test_local_run_store_reads_writes_and_lists_manifests(tmp_path: Path) -> None:
    store = LocalRunStore(tmp_path)
    manifest_b = _manifest("dry-run-b")
    manifest_a = _manifest("dry-run-a")

    store.write_manifest(manifest_b)
    store.write_manifest(manifest_a)

    assert store.read_manifest("dry-run-a") == manifest_a
    assert store.read_manifest("dry-run-b") == manifest_b
    assert store.list_runs() == ("dry-run-a", "dry-run-b")


def test_local_run_store_rejects_absolute_run_id(tmp_path: Path) -> None:
    store = LocalRunStore(tmp_path)

    with pytest.raises(ValueError, match="stable relative"):
        store.create_run_dir(str(tmp_path / "bad"))


def _manifest(run_id: str) -> RunManifest:
    return RunManifest(
        run_id=run_id,
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
            output_files=("runtime-result.json", "artifact-index.json", "run-manifest.json"),
        ),
    )
