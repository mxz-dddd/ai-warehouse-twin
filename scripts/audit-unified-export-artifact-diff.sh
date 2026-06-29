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
default_artifact="$tmp_dir/default-unified.json"
unified_explicit="$tmp_dir/explicit-unified.json"
legacy_fallback="$tmp_dir/explicit-legacy.json"

dotnet run --project src/Sim.Cli -- export-artifact \
  "$scenario" \
  -o "$default_artifact" >/dev/null

dotnet run --project src/Sim.Cli -- export-artifact \
  "$scenario" \
  -o "$unified_explicit" \
  --runner unified >/dev/null

dotnet run --project src/Sim.Cli -- export-artifact \
  "$scenario" \
  -o "$legacy_fallback" \
  --runner legacy >/dev/null

cmp "$default_artifact" "$unified_explicit"
cmp "$default_artifact" "$golden"
python3 -m json.tool "$legacy_fallback" >/dev/null

python3 - "$default_artifact" "$legacy_fallback" <<'PY'
import json
import sys
from pathlib import Path

default = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
legacy = json.loads(Path(sys.argv[2]).read_text(encoding="utf-8"))


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


if default.get("schema_version") != "run-artifact.v1":
    raise SystemExit(
        f"FAIL: expected default schema_version run-artifact.v1, got {default.get('schema_version')!r}"
    )

if legacy.get("schema_version") != "run-artifact.v1":
    raise SystemExit(
        f"FAIL: expected legacy schema_version run-artifact.v1, got {legacy.get('schema_version')!r}"
    )

rows = [
    ("schema_version", legacy["schema_version"], default["schema_version"]),
    ("artifact_kind", legacy["artifact_kind"], default["artifact_kind"]),
    ("scenario_id", legacy["scenario_id"], default["scenario_id"]),
    ("seed", legacy["seed"], default["seed"]),
    ("started_at_ms", legacy["started_at_ms"], default["started_at_ms"]),
    ("finished_at_ms", legacy["finished_at_ms"], default["finished_at_ms"]),
    ("final_world_time_ms", legacy["final_world_time_ms"], default["final_world_time_ms"]),
    (
        "kpi_summary.total_duration_ms",
        get(legacy, "kpi_summary.total_duration_ms"),
        get(default, "kpi_summary.total_duration_ms"),
    ),
    (
        "kpi_summary.total_completed_work_items",
        get(legacy, "kpi_summary.total_completed_work_items"),
        get(default, "kpi_summary.total_completed_work_items"),
    ),
    (
        "kpi_summary.event_log_line_count",
        get(legacy, "kpi_summary.event_log_line_count"),
        get(default, "kpi_summary.event_log_line_count"),
    ),
    ("layout.resources", resource_ids(legacy), resource_ids(default)),
    (
        "position_timeline.count",
        len(legacy.get("position_timeline", [])),
        len(default.get("position_timeline", [])),
    ),
    ("event_log.count", len(legacy.get("event_log", [])), len(default.get("event_log", []))),
]

print("Default unified switch audit diff summary")
print("metric | legacy fallback | unified default")
print("--- | --- | ---")
for metric, legacy_value, default_value in rows:
    print(f"{metric} | {legacy_value!r} | {default_value!r}")

if not any(legacy_value != default_value for _, legacy_value, default_value in rows):
    raise SystemExit("FAIL: expected legacy fallback to differ from unified default")
PY

echo "PASS: default export-artifact matches explicit unified"
echo "PASS: default export-artifact matches updated golden"
echo "PASS: explicit legacy fallback remains valid RunArtifact v1"
echo "PASS: default unified switch audit completed"
