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
legacy_artifact="$tmpdir/comparison-legacy.json"
unified_artifact="$tmpdir/comparison-unified.json"
bad_output="$tmpdir/bad-runner.stderr"

dotnet run --project src/Sim.Cli -- compare-files \
  "$baseline" \
  "$candidate" \
  -o "$default_artifact" >/dev/null

dotnet run --project src/Sim.Cli -- compare-files \
  "$baseline" \
  "$candidate" \
  -o "$legacy_artifact" \
  --runner legacy >/dev/null

dotnet run --project src/Sim.Cli -- compare-files \
  "$baseline" \
  "$candidate" \
  -o "$unified_artifact" \
  --runner unified >/dev/null

cmp "$default_artifact" "$golden"
cmp "$legacy_artifact" "$default_artifact"

python3 -m json.tool "$unified_artifact" >/dev/null

python3 - "$unified_artifact" <<'PY'
import json
import sys
from pathlib import Path

artifact = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))

if artifact.get("schema_version") != "comparison_artifact.v1":
    raise SystemExit(
        "FAIL: expected schema_version comparison_artifact.v1, "
        f"got {artifact.get('schema_version')!r}"
    )

baseline = artifact.get("baseline", {})
candidate = artifact.get("candidate", {})
if baseline.get("scenario_id") != "sample-small-warehouse":
    raise SystemExit(
        f"FAIL: unexpected baseline scenario id {baseline.get('scenario_id')!r}"
    )

if candidate.get("scenario_id") != "sample-small-warehouse-candidate":
    raise SystemExit(
        f"FAIL: unexpected candidate scenario id {candidate.get('scenario_id')!r}"
    )

deltas = artifact.get("deltas", [])
if len(deltas) != 11:
    raise SystemExit(f"FAIL: expected 11 comparison deltas, got {len(deltas)}")

metric_names = [delta.get("metric_name") for delta in deltas]
required = {
    "finished_at_ms",
    "completed_receipts",
    "completed_outbound_orders",
    "completed_each_pick_orders",
    "total_work_item_throughput_per_hour",
}
missing = sorted(required.difference(metric_names))
if missing:
    raise SystemExit(f"FAIL: missing unified comparison deltas: {missing!r}")

baseline_metrics = baseline.get("metrics", {})
candidate_metrics = candidate.get("metrics", {})
for name, metrics in [("baseline", baseline_metrics), ("candidate", candidate_metrics)]:
    completed = (
        metrics.get("completed_receipts", 0)
        + metrics.get("completed_outbound_orders", 0)
        + metrics.get("completed_each_pick_orders", 0)
    )
    if completed != 3:
        raise SystemExit(
            f"FAIL: expected {name} completed work items 3, got {completed!r}"
        )

print("PASS: unified comparison artifact is valid ComparisonArtifact v1")
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

echo "PASS: default and explicit legacy comparison artifacts match"
echo "PASS: bad compare-files runner mode rejected"
echo "PASS: unified comparison artifact smoke"
