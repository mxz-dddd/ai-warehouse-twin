#!/usr/bin/env bash
set -euo pipefail

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

scenario="datasets/sample-small-warehouse/scenario.json"
golden="datasets/sample-small-warehouse/artifacts/run-artifact.v1.json"
artifact_a="$tmpdir/artifact-a.json"
artifact_b="$tmpdir/artifact-b.json"

dotnet run --project src/Sim.Cli -- export-artifact "$scenario" -o "$artifact_a" >/dev/null
dotnet run --project src/Sim.Cli -- export-artifact "$scenario" -o "$artifact_b" >/dev/null

cmp "$artifact_a" "$artifact_b"
cmp "$artifact_a" "$golden"

python3 - "$artifact_a" <<'PY'
import json
import sys
from pathlib import Path

artifact = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
kpi = artifact["kpi_summary"]

assert artifact["schema_version"] == "run-artifact.v1"
assert artifact["artifact_kind"] == "warehouse-simulation-run"
assert artifact["scenario_id"] == "sample-small-warehouse"
assert artifact["seed"] == 20240627
assert artifact["started_at_ms"] == 10
assert artifact["finished_at_ms"] == 410
assert artifact["final_world_time_ms"] == 410
assert kpi["total_duration_ms"] == 400
assert kpi["total_completed_work_items"] == 3
assert kpi["event_log_line_count"] == 13
assert isinstance(artifact["event_log"], list)
assert len(artifact["event_log"]) == 13
assert [resource["resource_id"] for resource in artifact["layout"]["resources"]] == [
    "dock-1",
    "station-1",
]
assert len(artifact["position_timeline"]) == 6
PY

echo "PASS: export artifact smoke"
