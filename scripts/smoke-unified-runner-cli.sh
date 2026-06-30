#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

sample_output="$tmp_dir/sample-unified.json"
run_file_output="$tmp_dir/run-file-unified.json"
bad_output="$tmp_dir/bad-runner.stderr"

dotnet run --project src/Sim.Cli -- sample-small-warehouse --runner unified > "$sample_output"
dotnet run --project src/Sim.Cli -- run-file datasets/sample-small-warehouse/scenario.json --runner unified > "$run_file_output"

python3 - "$sample_output" "$run_file_output" <<'PY'
import json
import sys
from pathlib import Path

sample = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
run_file = json.loads(Path(sys.argv[2]).read_text(encoding="utf-8"))

for name, payload in [("sample", sample), ("run-file", run_file)]:
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
        actual = payload.get(key)
        if actual != value:
            raise SystemExit(
                f"FAIL: {name} {key}: expected {value!r}, got {actual!r}"
            )

    event_lines = payload.get("event_log_text", "").splitlines()
    if len(event_lines) != 13:
        raise SystemExit(
            f"FAIL: {name} expected 13 unified event log lines, got {len(event_lines)}"
        )

    required_fragments = [
        "owner=inbound:inbound:receipt-1",
        "owner=outbound:outbound:order-1",
        "owner=each_pick:each_pick:each-order-1",
    ]
    for fragment in required_fragments:
        if fragment not in payload.get("event_log_text", ""):
            raise SystemExit(f"FAIL: {name} missing event fragment: {fragment}")

if sample != run_file:
    raise SystemExit("FAIL: unified sample output and unified run-file output differ")

print("PASS: unified runner CLI outputs are stable")
PY

if dotnet run --project src/Sim.Cli -- sample-small-warehouse --runner bad-value > /dev/null 2> "$bad_output"; then
    echo "FAIL: bad runner mode unexpectedly succeeded"
    exit 1
fi

if ! grep -q "Unknown runner mode 'bad-value'. Allowed values: legacy, unified." "$bad_output"; then
    echo "FAIL: bad runner mode did not print the expected error"
    cat "$bad_output"
    exit 1
fi

echo "PASS: bad runner mode rejected"
echo "PASS: unified runner CLI smoke"
