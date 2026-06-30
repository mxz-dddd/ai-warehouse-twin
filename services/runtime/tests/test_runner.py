from __future__ import annotations

import json
import re
from pathlib import Path

from runtime_service import run_dry


def test_dry_run_outputs_are_byte_stable(tmp_path: Path) -> None:
    scenario = tmp_path / "scenario.json"
    scenario.write_text('{"scenario_id":"unit","schema_version":"warehouse-scenario.v0"}\n')
    first = tmp_path / "first"
    second = tmp_path / "second"

    first_result = run_dry(scenario, first)
    second_result = run_dry(scenario, second)

    assert first_result.job_id == second_result.job_id
    assert (first / "runtime-result.json").read_bytes() == (
        second / "runtime-result.json"
    ).read_bytes()
    assert (first / "artifact-index.json").read_bytes() == (
        second / "artifact-index.json"
    ).read_bytes()
    assert (first / "run-manifest.json").read_bytes() == (
        second / "run-manifest.json"
    ).read_bytes()


def test_dry_run_outputs_do_not_contain_local_noise(tmp_path: Path) -> None:
    scenario = tmp_path / "scenario.json"
    scenario.write_text('{"scenario_id":"unit","schema_version":"warehouse-scenario.v0"}\n')
    output_dir = tmp_path / "out"

    run_dry(scenario, output_dir)

    for output_file in ("runtime-result.json", "artifact-index.json", "run-manifest.json"):
        text = (output_dir / output_file).read_text(encoding="utf-8")
        assert str(tmp_path) not in text
        assert "scenario.json" in text or output_file == "artifact-index.json"
        assert not re.search(r"\d{4}-\d{2}-\d{2}", text)
        assert not re.search(r"\b[0-9a-f]{32,}\b", text)

    result = json.loads((output_dir / "runtime-result.json").read_text(encoding="utf-8"))
    assert result["status"] == "succeeded"
    assert result["scenario_name"] == "scenario.json"

    manifest = json.loads((output_dir / "run-manifest.json").read_text(encoding="utf-8"))
    assert manifest["run_id"] == result["job_id"]
    assert manifest["runner"]["mode"] == "dry-run"
    assert manifest["artifact_index_path"] == "artifact-index.json"
