"""Deterministic run manifest model for runtime outputs."""

from __future__ import annotations

import hashlib
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from runtime_service.models import RunStatus

RUN_MANIFEST_FILE = "run-manifest.json"
RUN_MANIFEST_SCHEMA_VERSION = "runtime-run-manifest.v0"


@dataclass(frozen=True)
class RunInput:
    scenario_path: str
    scenario_name: str
    scenario_content_hash: str

    def __post_init__(self) -> None:
        _reject_absolute_path(self.scenario_path, "scenario_path")

    def to_json_dict(self) -> dict[str, object]:
        return {
            "scenario_content_hash": self.scenario_content_hash,
            "scenario_name": self.scenario_name,
            "scenario_path": self.scenario_path,
        }

    @classmethod
    def from_json_dict(cls, document: dict[str, Any]) -> RunInput:
        return cls(
            scenario_path=_required_str(document, "scenario_path"),
            scenario_name=_required_str(document, "scenario_name"),
            scenario_content_hash=_required_str(document, "scenario_content_hash"),
        )


@dataclass(frozen=True)
class RunEnvironment:
    service_name: str = "runtime_service"
    execution_environment: str = "local"

    def to_json_dict(self) -> dict[str, object]:
        return {
            "execution_environment": self.execution_environment,
            "service_name": self.service_name,
        }

    @classmethod
    def from_json_dict(cls, document: dict[str, Any]) -> RunEnvironment:
        return cls(
            service_name=_required_str(document, "service_name"),
            execution_environment=_required_str(document, "execution_environment"),
        )


@dataclass(frozen=True)
class RunOutputSummary:
    artifact_index_path: str
    output_files: tuple[str, ...]

    def __post_init__(self) -> None:
        _reject_absolute_path(self.artifact_index_path, "artifact_index_path")
        for output_file in self.output_files:
            _reject_absolute_path(output_file, "output_file")
        object.__setattr__(self, "output_files", tuple(sorted(self.output_files)))

    def to_json_dict(self) -> dict[str, object]:
        return {
            "artifact_index_path": self.artifact_index_path,
            "output_file_count": len(self.output_files),
            "output_files": list(self.output_files),
        }

    @classmethod
    def from_json_dict(cls, document: dict[str, Any]) -> RunOutputSummary:
        output_files = _required_list_str(document, "output_files")
        return cls(
            artifact_index_path=_required_str(document, "artifact_index_path"),
            output_files=tuple(output_files),
        )


@dataclass(frozen=True)
class RunManifest:
    run_id: str
    run_input: RunInput
    environment: RunEnvironment
    runner_name: str
    runner_mode: str
    status: RunStatus
    artifact_index_path: str
    output_summary: RunOutputSummary
    schema_version: str = RUN_MANIFEST_SCHEMA_VERSION

    def __post_init__(self) -> None:
        _validate_run_id(self.run_id)
        _reject_absolute_path(self.artifact_index_path, "artifact_index_path")

    def to_json_dict(self) -> dict[str, object]:
        return {
            "artifact_index_path": self.artifact_index_path,
            "environment": self.environment.to_json_dict(),
            "input": self.run_input.to_json_dict(),
            "output_summary": self.output_summary.to_json_dict(),
            "run_id": self.run_id,
            "runner": {
                "mode": self.runner_mode,
                "name": self.runner_name,
            },
            "schema_version": self.schema_version,
            "status": self.status.value,
        }

    @classmethod
    def from_json_dict(cls, document: dict[str, Any]) -> RunManifest:
        runner = _required_dict(document, "runner")
        return cls(
            run_id=_required_str(document, "run_id"),
            run_input=RunInput.from_json_dict(_required_dict(document, "input")),
            environment=RunEnvironment.from_json_dict(_required_dict(document, "environment")),
            runner_name=_required_str(runner, "name"),
            runner_mode=_required_str(runner, "mode"),
            status=RunStatus(_required_str(document, "status")),
            artifact_index_path=_required_str(document, "artifact_index_path"),
            output_summary=RunOutputSummary.from_json_dict(
                _required_dict(document, "output_summary")
            ),
            schema_version=_required_str(document, "schema_version"),
        )

    def to_json_text(self) -> str:
        return json.dumps(self.to_json_dict(), indent=2, sort_keys=True) + "\n"


def build_run_manifest(
    *,
    scenario_path: Path,
    runner_name: str,
    runner_mode: str,
    status: RunStatus,
    artifact_index_path: str,
    output_files: tuple[str, ...],
) -> RunManifest:
    scenario_bytes = read_scenario_bytes(scenario_path)
    scenario_hash = scenario_content_hash(scenario_bytes)
    return RunManifest(
        run_id=deterministic_run_id(runner_mode=runner_mode, scenario_bytes=scenario_bytes),
        run_input=RunInput(
            scenario_path=scenario_path.name,
            scenario_name=scenario_path.name,
            scenario_content_hash=scenario_hash,
        ),
        environment=RunEnvironment(),
        runner_name=runner_name,
        runner_mode=runner_mode,
        status=status,
        artifact_index_path=artifact_index_path,
        output_summary=RunOutputSummary(
            artifact_index_path=artifact_index_path,
            output_files=output_files,
        ),
    )


def deterministic_run_id(*, runner_mode: str, scenario_bytes: bytes) -> str:
    digest = _scenario_digest(scenario_bytes)
    return f"{_slug(runner_mode)}-{digest}"


def read_scenario_bytes(path: Path) -> bytes:
    if not path.is_file():
        raise FileNotFoundError(f"scenario file not found: {path}")
    return path.read_bytes()


def scenario_content_hash(scenario_bytes: bytes) -> str:
    return f"sha256:{_scenario_digest(scenario_bytes)}"


def _scenario_digest(scenario_bytes: bytes) -> str:
    return hashlib.sha256(scenario_bytes).hexdigest()[:16]


def _slug(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-")


def _reject_absolute_path(value: str, field_name: str) -> None:
    if Path(value).is_absolute():
        raise ValueError(f"{field_name} must be relative")


def _validate_run_id(value: str) -> None:
    if not value or value in {".", ".."} or "/" in value or "\\" in value:
        raise ValueError("run_id must be a stable directory name")


def _required_str(document: dict[str, Any], key: str) -> str:
    value = document.get(key)
    if not isinstance(value, str):
        raise ValueError(f"manifest field {key} must be a string")
    return value


def _required_dict(document: dict[str, Any], key: str) -> dict[str, Any]:
    value = document.get(key)
    if not isinstance(value, dict):
        raise ValueError(f"manifest field {key} must be an object")
    return value


def _required_list_str(document: dict[str, Any], key: str) -> list[str]:
    value = document.get(key)
    if not isinstance(value, list) or not all(isinstance(item, str) for item in value):
        raise ValueError(f"manifest field {key} must be a string list")
    return value
