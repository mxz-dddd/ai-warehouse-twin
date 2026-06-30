from __future__ import annotations

import subprocess
from pathlib import Path
from typing import Any

from pytest import MonkeyPatch

from runtime_service.external import CommandResult, CommandSpec, SubprocessCommandExecutor


class FakeExecutor:
    def __init__(self, result: CommandResult) -> None:
        self.result = result
        self.seen_specs: list[CommandSpec] = []

    def run(self, spec: CommandSpec) -> CommandResult:
        self.seen_specs.append(spec)
        return self.result


def test_command_spec_serializes_stably() -> None:
    spec = CommandSpec(
        executable="dotnet",
        args=("run", "--project", "src/Sim.Cli"),
        working_directory=".",
        timeout_seconds=30,
    )

    assert spec.argv() == ["dotnet", "run", "--project", "src/Sim.Cli"]
    assert spec.to_json_dict() == {
        "args": ["run", "--project", "src/Sim.Cli"],
        "executable": "dotnet",
        "timeout_seconds": 30,
        "working_directory": ".",
    }


def test_fake_executor_success_result() -> None:
    spec = CommandSpec("fake", ("ok",), ".", 5)
    executor = FakeExecutor(CommandResult(0, "done", "", timed_out=False))

    result = executor.run(spec)

    assert result.succeeded
    assert result.to_json_dict()["succeeded"] is True
    assert executor.seen_specs == [spec]


def test_fake_executor_failure_result() -> None:
    spec = CommandSpec("fake", ("fail",), ".", 5)
    executor = FakeExecutor(CommandResult(2, "", "bad input", timed_out=False))

    result = executor.run(spec)

    assert not result.succeeded
    assert result.exit_code == 2
    assert result.stderr == "bad input"


def test_subprocess_executor_uses_shell_false(monkeypatch: MonkeyPatch, tmp_path: Path) -> None:
    captured: dict[str, Any] = {}

    def fake_run(argv: list[str], **kwargs: object) -> subprocess.CompletedProcess[str]:
        captured["argv"] = argv
        captured.update(kwargs)
        return subprocess.CompletedProcess(argv, 0, stdout="ok", stderr="")

    monkeypatch.setattr(subprocess, "run", fake_run)
    spec = CommandSpec("echo", ("hello",), ".", 3)

    result = SubprocessCommandExecutor(tmp_path).run(spec)

    assert result.succeeded
    assert captured["argv"] == ["echo", "hello"]
    assert captured["shell"] is False
    assert captured["timeout"] == 3
    assert captured["capture_output"] is True


def test_subprocess_executor_reports_timeout(
    monkeypatch: MonkeyPatch,
    tmp_path: Path,
) -> None:
    def fake_run(argv: list[str], **kwargs: object) -> subprocess.CompletedProcess[str]:
        raise subprocess.TimeoutExpired(argv, timeout=1, output="partial", stderr="too slow")

    monkeypatch.setattr(subprocess, "run", fake_run)
    spec = CommandSpec("slow", (), ".", 1)

    result = SubprocessCommandExecutor(tmp_path).run(spec)

    assert not result.succeeded
    assert result.timed_out
    assert result.exit_code == 124
    assert result.stdout == "partial"
    assert result.stderr == "too slow"
