#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

scenario="datasets/sample-small-warehouse/scenario.json"
golden="datasets/sample-small-warehouse/artifacts/run-artifact.v1.json"
legacy_default="$tmp_dir/legacy-default.json"
legacy_explicit="$tmp_dir/legacy-explicit.json"
unified_artifact="$tmp_dir/unified.json"
bad_output="$tmp_dir/bad-runner.stderr"

dotnet run --project src/Sim.Cli -- export-artifact "$scenario" -o "$legacy_default" >/dev/null
dotnet run --project src/Sim.Cli -- export-artifact "$scenario" -o "$legacy_explicit" --runner legacy >/dev/null
dotnet run --project src/Sim.Cli -- export-artifact "$scenario" -o "$unified_artifact" --runner unified >/dev/null

cmp "$legacy_default" "$golden"
cmp "$legacy_explicit" "$golden"

python3 -m json.tool "$legacy_default" >/dev/null
python3 -m json.tool "$legacy_explicit" >/dev/null
python3 -m json.tool "$unified_artifact" >/dev/null

python3 - "$unified_artifact" <<'PY'
import json
import math
import sys
from pathlib import Path

artifact = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
kpi = artifact["kpi_summary"]

expected = {
    "schema_version": "run-artifact.v1",
    "artifact_kind": "warehouse-simulation-run",
    "scenario_id": "sample-small-warehouse",
    "seed": 20240627,
    "started_at_ms": 10,
    "finished_at_ms": 410,
    "final_world_time_ms": 410,
}

for key, value in expected.items():
    actual = artifact.get(key)
    if actual != value:
        raise SystemExit(
            f"FAIL: unified artifact {key}: expected {value!r}, got {actual!r}"
        )

if kpi.get("total_duration_ms") != 400:
    raise SystemExit(
        f"FAIL: expected unified total_duration_ms 400, got {kpi.get('total_duration_ms')!r}"
    )

if kpi.get("total_completed_work_items") != 3:
    raise SystemExit(
        "FAIL: expected unified total_completed_work_items 3, "
        f"got {kpi.get('total_completed_work_items')!r}"
    )

if kpi.get("event_log_line_count") != 13:
    raise SystemExit(
        f"FAIL: expected unified event_log_line_count 13, got {kpi.get('event_log_line_count')!r}"
    )

if not math.isclose(float(kpi.get("total_work_item_throughput_per_hour", 0)), 27000.0):
    raise SystemExit(
        "FAIL: expected unified total_work_item_throughput_per_hour near 27000, "
        f"got {kpi.get('total_work_item_throughput_per_hour')!r}"
    )

event_log = artifact.get("event_log", [])
if len(event_log) != 13:
    raise SystemExit(f"FAIL: expected 13 unified event log lines, got {len(event_log)}")

required_event_fragments = [
    "owner=inbound:inbound:receipt-1",
    "owner=outbound:outbound:order-1",
    "owner=each_pick:each_pick:each-order-1",
]
joined_event_log = "\n".join(event_log)
for fragment in required_event_fragments:
    if fragment not in joined_event_log:
        raise SystemExit(f"FAIL: unified event log missing fragment: {fragment}")

resources = artifact.get("layout", {}).get("resources", [])
resource_ids = [resource.get("resource_id") for resource in resources]
if resource_ids != ["dock-1", "station-1"]:
    raise SystemExit(f"FAIL: unexpected unified layout resources: {resource_ids!r}")

position_timeline = artifact.get("position_timeline", [])
if len(position_timeline) != 6:
    raise SystemExit(
        f"FAIL: expected 6 unified position timeline entries, got {len(position_timeline)}"
    )

operation_types = {entry.get("operation_type") for entry in position_timeline}
if operation_types != {"inbound", "outbound", "each_pick"}:
    raise SystemExit(f"FAIL: unexpected unified operation types: {operation_types!r}")

stage_types = {entry.get("stage_type") for entry in position_timeline}
if stage_types != {"operation"}:
    raise SystemExit(f"FAIL: unexpected unified stage types: {stage_types!r}")

print("PASS: unified export artifact is valid RunArtifact v1")
PY

if dotnet run --project src/Sim.Cli -- export-artifact "$scenario" -o "$tmp_dir/bad.json" --runner bad-value > /dev/null 2> "$bad_output"; then
    echo "FAIL: bad export-artifact runner mode unexpectedly succeeded"
    exit 1
fi

if ! grep -q "Unknown runner mode 'bad-value'. Allowed values: legacy, unified." "$bad_output"; then
    echo "FAIL: bad export-artifact runner mode did not print the expected error"
    cat "$bad_output"
    exit 1
fi

echo "PASS: default and explicit legacy export artifacts match golden"
echo "PASS: bad export-artifact runner mode rejected"
echo "PASS: unified export artifact smoke"
