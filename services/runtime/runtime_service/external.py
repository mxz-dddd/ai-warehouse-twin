"""External command abstractions for future runtime runner integrations."""

from __future__ import annotations

import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Protocol


@dataclass(frozen=True)
class CommandSpec:
    executable: str
    args: tuple[str, ...]
    working_directory: str
    timeout_seconds: int

    def __post_init__(self) -> None:
        if Path(self.working_directory).is_absolute():
            raise ValueError("working_directory must be relative")

    def argv(self) -> list[str]:
        return [self.executable, *self.args]

    def to_json_dict(self) -> dict[str, object]:
        return {
            "args": list(self.args),
            "executable": self.executable,
            "timeout_seconds": self.timeout_seconds,
            "working_directory": self.working_directory,
        }


@dataclass(frozen=True)
class CommandResult:
    exit_code: int
    stdout: str
    stderr: str
    timed_out: bool

    @property
    def succeeded(self) -> bool:
        return self.exit_code == 0 and not self.timed_out

    def to_json_dict(self) -> dict[str, object]:
        return {
            "exit_code": self.exit_code,
            "stderr": self.stderr,
            "stdout": self.stdout,
            "succeeded": self.succeeded,
            "timed_out": self.timed_out,
        }


class CommandExecutor(Protocol):
    def run(self, spec: CommandSpec) -> CommandResult:
        """Run a command and return structured output."""


class SubprocessCommandExecutor:
    def __init__(self, repo_root: Path) -> None:
        self._repo_root = repo_root

    def run(self, spec: CommandSpec) -> CommandResult:
        try:
            completed = subprocess.run(
                spec.argv(),
                cwd=self._repo_root / spec.working_directory,
                capture_output=True,
                text=True,
                timeout=spec.timeout_seconds,
                check=False,
                shell=False,
            )
        except subprocess.TimeoutExpired as error:
            return CommandResult(
                exit_code=124,
                stdout=_string_output(error.stdout),
                stderr=_string_output(error.stderr),
                timed_out=True,
            )
        except OSError as error:
            return CommandResult(
                exit_code=127,
                stdout="",
                stderr=str(error),
                timed_out=False,
            )

        return CommandResult(
            exit_code=completed.returncode,
            stdout=completed.stdout,
            stderr=completed.stderr,
            timed_out=False,
        )


def _string_output(value: str | bytes | None) -> str:
    if value is None:
        return ""
    if isinstance(value, bytes):
        return value.decode("utf-8", errors="replace")
    return value
