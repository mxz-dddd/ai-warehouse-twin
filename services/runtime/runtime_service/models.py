"""Runtime run job model."""

from __future__ import annotations

from dataclasses import dataclass
from enum import StrEnum
from pathlib import Path


class RunStatus(StrEnum):
    PENDING = "pending"
    RUNNING = "running"
    SUCCEEDED = "succeeded"
    FAILED = "failed"


@dataclass(frozen=True)
class RunRequest:
    scenario_path: Path
    output_dir: Path
    runner_name: str = "local-dry-run"

    @property
    def scenario_name(self) -> str:
        return self.scenario_path.name


@dataclass(frozen=True)
class RunResult:
    job_id: str
    status: RunStatus
    scenario_name: str
    runner_name: str
    artifacts: tuple[str, ...]
    message: str

    def to_json_dict(self) -> dict[str, object]:
        return {
            "artifacts": list(self.artifacts),
            "job_id": self.job_id,
            "message": self.message,
            "runner_name": self.runner_name,
            "scenario_name": self.scenario_name,
            "status": self.status.value,
        }


@dataclass
class RunJob:
    job_id: str
    request: RunRequest
    status: RunStatus = RunStatus.PENDING
    result: RunResult | None = None
    error_message: str | None = None

    def mark_running(self) -> None:
        if self.status is not RunStatus.PENDING:
            raise ValueError(f"cannot start job from {self.status.value}")
        self.status = RunStatus.RUNNING

    def succeed(self, result: RunResult) -> None:
        if self.status is not RunStatus.RUNNING:
            raise ValueError(f"cannot succeed job from {self.status.value}")
        self.status = RunStatus.SUCCEEDED
        self.result = result
        self.error_message = None

    def fail(self, message: str) -> None:
        if self.status is RunStatus.SUCCEEDED:
            raise ValueError("cannot fail a succeeded job")
        self.status = RunStatus.FAILED
        self.error_message = message
