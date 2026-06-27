#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

output="$tmp_dir/sample-each-pick.json"

dotnet run --project src/Sim.Cli -- \
  run-file datasets/sample-each-pick/scenario.json >"$output"

python3 - "$output" <<'PY'
import json
import sys
from pathlib import Path

payload = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))

expected = {
    "scenario_id": "sample-each-pick",
    "seed": 20240628,
    "started_at_ms": 0,
    "finished_at_ms": 100,
    "completed_receipts": 0,
    "completed_outbound_orders": 0,
    "completed_each_pick_orders": 1,
    "total_quantity_available": 0,
    "total_quantity_shipped": 0,
    "total_quantity_picked": 5,
    "final_world_time_ms": 100,
}

for key, value in expected.items():
    actual = payload.get(key)
    if actual != value:
        raise SystemExit(f"FAIL: {key}: expected {value!r}, got {actual!r}")

event_lines = payload.get("event_log_text", "").splitlines()
required_events = [
    "each_pick|0|0|each_pick.order_released.each-order-1|EachPickOrderReleased",
    "each_pick|1|30|each_pick.at_station.each-order-1|EachPickAtStation",
    "each_pick|2|60|each_pick.completed.each-order-1|EachPickCompleted",
    "each_pick|3|100|each_pick.staged.each-order-1|EachPickStaged",
]

if event_lines != required_events:
    raise SystemExit(
        f"FAIL: expected deterministic each-pick event log {required_events!r}, "
        f"got {event_lines!r}"
    )

orders_released = sum(
    "EachPickOrderReleased" in event for event in event_lines
)
orders_completed = payload.get("completed_each_pick_orders")
units_picked = payload.get("total_quantity_picked")
sim_completed = (
    orders_completed == 1
    and payload.get("finished_at_ms") == payload.get("final_world_time_ms")
)

if orders_released != 1:
    raise SystemExit(
        f"FAIL: expected one released each-pick order, got {orders_released}"
    )
if orders_completed != 1:
    raise SystemExit(
        f"FAIL: expected one completed each-pick order, got {orders_completed!r}"
    )
if units_picked != 5:
    raise SystemExit(
        f"FAIL: expected five picked units, got {units_picked!r}"
    )
if not sim_completed:
    raise SystemExit("FAIL: each-pick simulation did not complete")

kpi = payload.get("kpi_summary", {})
if kpi.get("total_duration_ms") != 100:
    raise SystemExit("FAIL: expected total duration of 100 ms")
if kpi.get("total_completed_work_items") != 1:
    raise SystemExit("FAIL: expected one completed work item")
if kpi.get("event_log_line_count") != 4:
    raise SystemExit("FAIL: expected four event log lines")
throughput = float(kpi.get("each_pick_order_throughput_per_hour", 0))
if abs(throughput - 36000.0) > 1e-6:
    raise SystemExit(
        f"FAIL: expected each-pick throughput near 36000 orders/hour, "
        f"got {throughput!r}"
    )

print("PASS: sample each-pick scenario ran")
print(f"orders released: {orders_released}")
print(f"orders completed: {orders_completed}")
print(f"units picked: {units_picked}")
print(f"events: {len(event_lines)}")
print(f"sim completed: {str(sim_completed).lower()}")
PY
