from __future__ import annotations

import json
from pathlib import Path

import pytest

from runtime_service.artifacts import ArtifactRegistry


def test_artifact_registry_outputs_stable_relative_records(tmp_path: Path) -> None:
    (tmp_path / "runtime-result.json").write_text("{}", encoding="utf-8")
    registry = ArtifactRegistry(tmp_path)

    registry.register("runtime-result", "runtime-result", "runtime-result.json")
    registry.register("missing-report", "report", "reports/missing.md")

    assert json.dumps(registry.to_json_dict(), sort_keys=True) == json.dumps(
        {
            "artifacts": [
                {
                    "artifact_type": "report",
                    "exists": False,
                    "name": "missing-report",
                    "relative_path": "reports/missing.md",
                },
                {
                    "artifact_type": "runtime-result",
                    "exists": True,
                    "name": "runtime-result",
                    "relative_path": "runtime-result.json",
                },
            ],
            "schema_version": "runtime-artifact-index.v0",
        },
        sort_keys=True,
    )


def test_artifact_registry_rejects_absolute_paths(tmp_path: Path) -> None:
    registry = ArtifactRegistry(tmp_path)

    with pytest.raises(ValueError, match="must not be absolute"):
        registry.register("bad", "bad", str(tmp_path / "bad.json"))
