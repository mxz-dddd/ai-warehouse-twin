"""Deterministic artifact registry for runtime dry runs."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class ArtifactRecord:
    name: str
    artifact_type: str
    relative_path: str
    exists: bool

    def to_json_dict(self) -> dict[str, object]:
        return {
            "artifact_type": self.artifact_type,
            "exists": self.exists,
            "name": self.name,
            "relative_path": self.relative_path,
        }


class ArtifactRegistry:
    def __init__(self, output_dir: Path) -> None:
        self._output_dir = output_dir
        self._records: list[ArtifactRecord] = []

    def register(self, name: str, artifact_type: str, relative_path: str) -> ArtifactRecord:
        if Path(relative_path).is_absolute():
            raise ValueError("artifact relative_path must not be absolute")
        record = ArtifactRecord(
            name=name,
            artifact_type=artifact_type,
            relative_path=relative_path,
            exists=(self._output_dir / relative_path).is_file(),
        )
        self._records.append(record)
        return record

    @property
    def records(self) -> tuple[ArtifactRecord, ...]:
        return tuple(sorted(self._records, key=lambda item: (item.name, item.relative_path)))

    def to_json_dict(self) -> dict[str, object]:
        return {
            "artifacts": [record.to_json_dict() for record in self.records],
            "schema_version": "runtime-artifact-index.v0",
        }
