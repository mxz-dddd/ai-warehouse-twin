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
assert payload["seed"] == 20240627
assert payload["started_at_ms"] == 10
assert payload["finished_at_ms"] == 220
assert payload["completed_receipts"] == 1
assert payload["completed_outbound_orders"] == 1
assert payload["completed_each_pick_orders"] == 1
assert payload["total_quantity_available"] == 7
assert payload["total_quantity_shipped"] == 8
assert payload["total_quantity_picked"] == 9
assert payload["final_world_time_ms"] == 220

event_log = payload["event_log_text"]
lines = event_log.splitlines()

assert len(lines) == 10
assert "inbound|0|10|inbound.receipt_arrived.receipt-1|InboundReceiptArrived" in lines
assert "outbound|0|20|outbound.order_released.order-1|OutboundOrderReleased" in lines
assert "each_pick|0|30|each_pick.order_released.each-order-1|EachPickOrderReleased" in lines

print("PASS: sample warehouse CLI smoke")
PY
