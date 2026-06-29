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
default_artifact="$tmp_dir/default.json"
unified_artifact="$tmp_dir/unified.json"
legacy_artifact="$tmp_dir/legacy.json"
bad_output="$tmp_dir/bad-runner.stderr"

dotnet run --project src/Sim.Cli -- export-artifact "$scenario" -o "$default_artifact" >/dev/null
dotnet run --project src/Sim.Cli -- export-artifact "$scenario" -o "$unified_artifact" --runner unified >/dev/null
dotnet run --project src/Sim.Cli -- export-artifact "$scenario" -o "$legacy_artifact" --runner legacy >/dev/null

cmp "$default_artifact" "$unified_artifact"
cmp "$default_artifact" "$golden"

python3 -m json.tool "$default_artifact" >/dev/null
python3 -m json.tool "$unified_artifact" >/dev/null
python3 -m json.tool "$legacy_artifact" >/dev/null

python3 - "$default_artifact" "$legacy_artifact" <<'PY'
import json
import sys
from pathlib import Path

default = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
legacy = json.loads(Path(sys.argv[2]).read_text(encoding="utf-8"))

if default.get("schema_version") != "run-artifact.v1":
    raise SystemExit(
        f"FAIL: expected default schema_version run-artifact.v1, got {default.get('schema_version')!r}"
    )

if legacy.get("schema_version") != "run-artifact.v1":
    raise SystemExit(
        f"FAIL: expected legacy schema_version run-artifact.v1, got {legacy.get('schema_version')!r}"
    )

expected_default = {
    "artifact_kind": "warehouse-simulation-run",
    "scenario_id": "sample-small-warehouse",
    "seed": 20240627,
    "started_at_ms": 10,
    "finished_at_ms": 410,
    "final_world_time_ms": 410,
}

for key, value in expected_default.items():
    actual = default.get(key)
    if actual != value:
        raise SystemExit(f"FAIL: default {key}: expected {value!r}, got {actual!r}")

summary = {
    "finished_at_ms": (legacy["finished_at_ms"], default["finished_at_ms"]),
    "final_world_time_ms": (legacy["final_world_time_ms"], default["final_world_time_ms"]),
    "total_duration_ms": (
        legacy["kpi_summary"]["total_duration_ms"],
        default["kpi_summary"]["total_duration_ms"],
    ),
    "event_log_line_count": (
        legacy["kpi_summary"]["event_log_line_count"],
        default["kpi_summary"]["event_log_line_count"],
    ),
    "layout_resources": (
        [resource["resource_id"] for resource in legacy["layout"]["resources"]],
        [resource["resource_id"] for resource in default["layout"]["resources"]],
    ),
    "position_timeline_count": (
        len(legacy.get("position_timeline", [])),
        len(default.get("position_timeline", [])),
    ),
    "event_log_count": (
        len(legacy.get("event_log", [])),
        len(default.get("event_log", [])),
    ),
}

if not any(legacy_value != default_value for legacy_value, default_value in summary.values()):
    raise SystemExit("FAIL: expected legacy fallback to differ from unified default")

print("Unified default vs legacy fallback diff summary")
for metric, (legacy_value, default_value) in summary.items():
    print(f"- {metric}: legacy={legacy_value!r}, unified={default_value!r}")

print("PASS: explicit legacy export artifact remains valid RunArtifact v1")
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

echo "PASS: default export-artifact matches explicit unified and golden"
echo "PASS: bad export-artifact runner mode rejected"
echo "PASS: unified export artifact smoke"
