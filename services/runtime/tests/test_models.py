from __future__ import annotations

from pathlib import Path

import pytest

from runtime_service.models import RunJob, RunRequest, RunResult, RunStatus


def test_run_job_status_flow() -> None:
    request = RunRequest(
        scenario_path=Path("scenario.json"),
        output_dir=Path("out"),
    )
    job = RunJob(job_id="dry-run-test", request=request)

    assert job.status is RunStatus.PENDING

    job.mark_running()
    assert job.status is RunStatus.RUNNING

    result = RunResult(
        job_id=job.job_id,
        status=RunStatus.SUCCEEDED,
        scenario_name="scenario.json",
        runner_name="local-dry-run",
        artifacts=("runtime-result.json",),
        message="dry run completed",
    )
    job.succeed(result)

    assert job.status is RunStatus.SUCCEEDED
    assert job.result == result
    assert job.error_message is None


def test_run_job_rejects_invalid_transition() -> None:
    request = RunRequest(
        scenario_path=Path("scenario.json"),
        output_dir=Path("out"),
    )
    job = RunJob(job_id="dry-run-test", request=request)

    with pytest.raises(ValueError, match="cannot succeed job"):
        job.succeed(
            RunResult(
                job_id=job.job_id,
                status=RunStatus.SUCCEEDED,
                scenario_name="scenario.json",
                runner_name="local-dry-run",
                artifacts=(),
                message="dry run completed",
            )
        )
