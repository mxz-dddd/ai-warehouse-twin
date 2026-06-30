#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

OUTPUT="$(dotnet run --project src/Sim.Cli -- sample-small-warehouse)"

SMOKE_OUTPUT="$OUTPUT" python3 - <<'PY'
import json
import os

payload = json.loads(os.environ["SMOKE_OUTPUT"])

assert payload["scenario_id"] == "sample-small-warehouse"
assert payload["runner_mode"] == "unified"
assert payload["seed"] == 20240627
assert payload["started_at_ms"] == 10
assert payload["finished_at_ms"] == 410
assert payload["completed_receipts"] == 1
assert payload["completed_outbound_orders"] == 1
assert payload["completed_each_pick_orders"] == 1
assert payload["total_quantity_available"] == 7
assert payload["total_quantity_shipped"] == 8
assert payload["total_quantity_picked"] == 9
assert payload["final_world_time_ms"] == 410
assert payload["kpi_summary"]["total_duration_ms"] == 400
assert payload["kpi_summary"]["total_completed_work_items"] == 3
assert payload["kpi_summary"]["event_log_line_count"] == 13

event_log = payload["event_log_text"]
lines = event_log.splitlines()

assert len(lines) == 13
assert "10|resource.requested|resource_id=dock-1|owner=inbound:inbound:receipt-1" in lines
assert "20|resource.queued|resource_id=dock-1|owner=outbound:outbound:order-1" in lines
assert "30|resource.acquired|resource_id=station-1|owner=each_pick:each_pick:each-order-1" in lines

print("PASS: sample warehouse CLI smoke")
PY
