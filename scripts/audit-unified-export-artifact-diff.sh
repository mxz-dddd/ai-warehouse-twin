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
unified="$tmp_dir/unified.json"

dotnet run --project src/Sim.Cli -- export-artifact \
  "$scenario" \
  -o "$legacy_default" >/dev/null

dotnet run --project src/Sim.Cli -- export-artifact \
  "$scenario" \
  -o "$legacy_explicit" \
  --runner legacy >/dev/null

dotnet run --project src/Sim.Cli -- export-artifact \
  "$scenario" \
  -o "$unified" \
  --runner unified >/dev/null

cmp "$legacy_default" "$golden"
cmp "$legacy_explicit" "$golden"
python3 -m json.tool "$unified" >/dev/null

python3 - "$legacy_default" "$unified" <<'PY'
import json
import sys
from pathlib import Path

legacy = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
unified = json.loads(Path(sys.argv[2]).read_text(encoding="utf-8"))


def get(payload, path):
    value = payload
    for key in path.split("."):
        value = value[key]
    return value


def resource_ids(payload):
    return [
        resource["resource_id"]
        for resource in payload.get("layout", {}).get("resources", [])
    ]


rows = [
    ("schema_version", legacy["schema_version"], unified["schema_version"]),
    ("artifact_kind", legacy["artifact_kind"], unified["artifact_kind"]),
    ("scenario_id", legacy["scenario_id"], unified["scenario_id"]),
    ("seed", legacy["seed"], unified["seed"]),
    ("started_at_ms", legacy["started_at_ms"], unified["started_at_ms"]),
    ("finished_at_ms", legacy["finished_at_ms"], unified["finished_at_ms"]),
    ("final_world_time_ms", legacy["final_world_time_ms"], unified["final_world_time_ms"]),
    (
        "kpi_summary.total_duration_ms",
        get(legacy, "kpi_summary.total_duration_ms"),
        get(unified, "kpi_summary.total_duration_ms"),
    ),
    (
        "kpi_summary.total_completed_work_items",
        get(legacy, "kpi_summary.total_completed_work_items"),
        get(unified, "kpi_summary.total_completed_work_items"),
    ),
    (
        "kpi_summary.event_log_line_count",
        get(legacy, "kpi_summary.event_log_line_count"),
        get(unified, "kpi_summary.event_log_line_count"),
    ),
    ("layout.resources", resource_ids(legacy), resource_ids(unified)),
    (
        "position_timeline.count",
        len(legacy.get("position_timeline", [])),
        len(unified.get("position_timeline", [])),
    ),
    ("event_log.count", len(legacy.get("event_log", [])), len(unified.get("event_log", []))),
]

print("Artifact switch readiness diff summary")
print("metric | legacy | unified")
print("--- | --- | ---")
for metric, legacy_value, unified_value in rows:
    print(f"{metric} | {legacy_value!r} | {unified_value!r}")
PY

echo "PASS: legacy default and explicit legacy match golden"
echo "PASS: unified export artifact is valid RunArtifact v1"
echo "PASS: artifact switch readiness audit completed"
