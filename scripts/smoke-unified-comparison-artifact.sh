#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

baseline="datasets/sample-small-warehouse/scenario.json"
candidate="datasets/sample-small-warehouse/scenario-candidate.json"
golden="datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json"
default_artifact="$tmpdir/comparison-default.json"
unified_artifact="$tmpdir/comparison-unified.json"
legacy_artifact="$tmpdir/comparison-legacy.json"
bad_output="$tmpdir/bad-runner.stderr"

dotnet run --project src/Sim.Cli -- compare-files \
  "$baseline" \
  "$candidate" \
  -o "$default_artifact" >/dev/null

dotnet run --project src/Sim.Cli -- compare-files \
  "$baseline" \
  "$candidate" \
  -o "$unified_artifact" \
  --runner unified >/dev/null

dotnet run --project src/Sim.Cli -- compare-files \
  "$baseline" \
  "$candidate" \
  -o "$legacy_artifact" \
  --runner legacy >/dev/null

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

if default.get("schema_version") != "comparison_artifact.v1":
    raise SystemExit(
        "FAIL: expected default schema_version comparison_artifact.v1, "
        f"got {default.get('schema_version')!r}"
    )

if legacy.get("schema_version") != "comparison_artifact.v1":
    raise SystemExit(
        "FAIL: expected legacy schema_version comparison_artifact.v1, "
        f"got {legacy.get('schema_version')!r}"
    )

for name, artifact in [("default", default), ("legacy", legacy)]:
    baseline = artifact.get("baseline", {})
    candidate = artifact.get("candidate", {})
    if baseline.get("scenario_id") != "sample-small-warehouse":
        raise SystemExit(
            f"FAIL: unexpected {name} baseline scenario id {baseline.get('scenario_id')!r}"
        )
    if candidate.get("scenario_id") != "sample-small-warehouse-candidate":
        raise SystemExit(
            f"FAIL: unexpected {name} candidate scenario id {candidate.get('scenario_id')!r}"
        )
    if len(artifact.get("deltas", [])) != 11:
        raise SystemExit(f"FAIL: expected 11 {name} deltas")

summary = {
    "baseline.finished_at_ms": (
        legacy["baseline"]["metrics"]["finished_at_ms"],
        default["baseline"]["metrics"]["finished_at_ms"],
    ),
    "candidate.finished_at_ms": (
        legacy["candidate"]["metrics"]["finished_at_ms"],
        default["candidate"]["metrics"]["finished_at_ms"],
    ),
    "baseline.total_work_item_throughput_per_hour": (
        legacy["baseline"]["metrics"]["total_work_item_throughput_per_hour"],
        default["baseline"]["metrics"]["total_work_item_throughput_per_hour"],
    ),
    "candidate.total_work_item_throughput_per_hour": (
        legacy["candidate"]["metrics"]["total_work_item_throughput_per_hour"],
        default["candidate"]["metrics"]["total_work_item_throughput_per_hour"],
    ),
}

if not any(legacy_value != default_value for legacy_value, default_value in summary.values()):
    raise SystemExit("FAIL: expected legacy fallback comparison to differ from unified default")

print("Unified default vs legacy fallback comparison diff summary")
for metric, (legacy_value, default_value) in summary.items():
    print(f"- {metric}: legacy={legacy_value!r}, unified={default_value!r}")

print("PASS: explicit legacy comparison artifact remains valid ComparisonArtifact v1")
PY

if dotnet run --project src/Sim.Cli -- compare-files \
  "$baseline" \
  "$candidate" \
  -o "$tmpdir/bad.json" \
  --runner bad-value > /dev/null 2> "$bad_output"; then
    echo "FAIL: bad compare-files runner mode unexpectedly succeeded"
    exit 1
fi

if ! grep -q "Unknown runner mode 'bad-value'. Allowed values: legacy, unified." "$bad_output"; then
    echo "FAIL: bad compare-files runner mode did not print the expected error"
    cat "$bad_output"
    exit 1
fi

echo "PASS: default compare-files matches explicit unified and golden"
echo "PASS: bad compare-files runner mode rejected"
echo "PASS: unified comparison artifact smoke"
