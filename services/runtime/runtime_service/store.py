"""Local deterministic runtime run store."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from runtime_service.manifest import RUN_MANIFEST_FILE, RunManifest


class LocalRunStore:
    def __init__(self, root_dir: Path) -> None:
        self._root_dir = root_dir

    def create_run_dir(self, run_id: str) -> Path:
        _validate_run_id(run_id)
        run_dir = self._root_dir / run_id
        run_dir.mkdir(parents=True, exist_ok=True)
        return run_dir

    def write_manifest(self, manifest: RunManifest) -> Path:
        run_dir = self.create_run_dir(manifest.run_id)
        return write_manifest_to_dir(run_dir, manifest)

    def read_manifest(self, run_id: str) -> RunManifest:
        _validate_run_id(run_id)
        return read_manifest_from_dir(self._root_dir / run_id)

    def list_runs(self) -> tuple[str, ...]:
        if not self._root_dir.exists():
            return ()
        run_ids = [
            item.name
            for item in self._root_dir.iterdir()
            if item.is_dir() and (item / RUN_MANIFEST_FILE).is_file()
        ]
        return tuple(sorted(run_ids))


def write_manifest_to_dir(run_dir: Path, manifest: RunManifest) -> Path:
    run_dir.mkdir(parents=True, exist_ok=True)
    manifest_path = run_dir / RUN_MANIFEST_FILE
    manifest_path.write_text(manifest.to_json_text(), encoding="utf-8")
    return manifest_path


def read_manifest_from_dir(run_dir: Path) -> RunManifest:
    manifest_path = run_dir / RUN_MANIFEST_FILE
    if not manifest_path.is_file():
        raise FileNotFoundError(f"run manifest not found: {RUN_MANIFEST_FILE}")
    document = json.loads(manifest_path.read_text(encoding="utf-8"))
    if not isinstance(document, dict):
        raise ValueError("run manifest must be a JSON object")
    return RunManifest.from_json_dict(_string_key_dict(document))


def _string_key_dict(document: dict[Any, Any]) -> dict[str, Any]:
    if not all(isinstance(key, str) for key in document):
        raise ValueError("run manifest keys must be strings")
    return document


def _validate_run_id(value: str) -> None:
    if Path(value).is_absolute() or not value or value in {".", ".."}:
        raise ValueError("run_id must be a stable relative directory name")
    if "/" in value or "\\" in value:
        raise ValueError("run_id must be a stable relative directory name")
