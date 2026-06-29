#!/usr/bin/env bash
set -euo pipefail

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

builtin_output="$tmp_dir/sample-small-warehouse.builtin.json"
run_file_output="$tmp_dir/sample-small-warehouse.run-file.json"

dotnet run --project src/Sim.Cli -- sample-small-warehouse > "$builtin_output"
dotnet run --project src/Sim.Cli -- run-file datasets/sample-small-warehouse/scenario.json > "$run_file_output"

python3 - "$builtin_output" "$run_file_output" <<'PY'
import json
import sys
from pathlib import Path

builtin_path = Path(sys.argv[1])
run_file_path = Path(sys.argv[2])

builtin = json.loads(builtin_path.read_text(encoding="utf-8"))
run_file = json.loads(run_file_path.read_text(encoding="utf-8"))

if builtin != run_file:
    raise SystemExit("FAIL: built-in output and run-file output differ")

expected = {
    "runner_mode": "unified",
    "scenario_id": "sample-small-warehouse",
    "seed": 20240627,
    "started_at_ms": 10,
    "finished_at_ms": 410,
    "completed_receipts": 1,
    "completed_outbound_orders": 1,
    "completed_each_pick_orders": 1,
    "total_quantity_available": 7,
    "total_quantity_shipped": 8,
    "total_quantity_picked": 9,
    "final_world_time_ms": 410,
}

for key, value in expected.items():
    actual = run_file.get(key)
    if actual != value:
        raise SystemExit(f"FAIL: {key}: expected {value!r}, got {actual!r}")

event_log_text = run_file.get("event_log_text", "")
event_lines = event_log_text.splitlines()

if len(event_lines) != 13:
    raise SystemExit(f"FAIL: expected 13 event log lines, got {len(event_lines)}")

required_events = [
    "10|resource.requested|resource_id=dock-1|owner=inbound:inbound:receipt-1",
    "20|resource.queued|resource_id=dock-1|owner=outbound:outbound:order-1",
    "30|resource.acquired|resource_id=station-1|owner=each_pick:each_pick:each-order-1",
]

for event in required_events:
    if event not in event_lines:
        raise SystemExit(f"FAIL: missing event log line: {event}")

print("PASS: sample warehouse run-file CLI smoke")
PY
