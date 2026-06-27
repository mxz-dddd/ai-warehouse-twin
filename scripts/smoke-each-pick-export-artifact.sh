#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

scenario="datasets/sample-each-pick/scenario.json"
artifact_a="$tmp_dir/artifact-a.json"
artifact_b="$tmp_dir/artifact-b.json"
run_output="$tmp_dir/run-output.json"

dotnet run --project src/Sim.Cli -- \
  export-artifact "$scenario" -o "$artifact_a" >/dev/null
dotnet run --project src/Sim.Cli -- \
  export-artifact "$scenario" -o "$artifact_b" >/dev/null
dotnet run --project src/Sim.Cli -- \
  run-file "$scenario" >"$run_output"

cmp "$artifact_a" "$artifact_b"

python3 - "$artifact_a" "$run_output" <<'PY'
import json
import sys
from pathlib import Path

artifact = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
run_output = json.loads(Path(sys.argv[2]).read_text(encoding="utf-8"))
kpi = artifact.get("kpi_summary", {})
event_log = artifact.get("event_log", [])

expected_artifact = {
    "schema_version": "run-artifact.v1",
    "artifact_kind": "warehouse-simulation-run",
    "scenario_id": "sample-each-pick",
    "seed": 20240628,
    "started_at_ms": 0,
    "finished_at_ms": 100,
    "final_world_time_ms": 100,
}

for key, value in expected_artifact.items():
    actual = artifact.get(key)
    if actual != value:
        raise SystemExit(f"FAIL: {key}: expected {value!r}, got {actual!r}")

if kpi.get("total_completed_work_items") != 1:
    raise SystemExit("FAIL: expected one completed each-pick order")
if kpi.get("event_log_line_count") != 4:
    raise SystemExit("FAIL: expected four each-pick artifact events")

required_events = [
    "EachPickOrderReleased",
    "EachPickAtStation",
    "EachPickCompleted",
    "EachPickStaged",
]

if not isinstance(event_log, list) or len(event_log) != 4:
    raise SystemExit(f"FAIL: expected four event log entries, got {event_log!r}")

for event_type in required_events:
    if not any(entry.endswith(f"|{event_type}") for entry in event_log):
        raise SystemExit(f"FAIL: missing each-pick artifact event: {event_type}")

orders_completed = run_output.get("completed_each_pick_orders")
units_picked = run_output.get("total_quantity_picked")

if orders_completed != 1:
    raise SystemExit(
        f"FAIL: expected one completed each-pick order, got {orders_completed!r}"
    )
if units_picked != 5:
    raise SystemExit(
        f"FAIL: expected five picked units, got {units_picked!r}"
    )

print("PASS: each-pick export artifact deterministic")
print(f"orders completed: {orders_completed}")
print(f"units picked: {units_picked}")
print(f"events: {len(event_log)}")
PY
