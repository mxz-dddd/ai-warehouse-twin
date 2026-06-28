#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

baseline="datasets/sample-small-warehouse/scenario.json"
candidate="datasets/sample-small-warehouse/scenario-candidate.json"
golden="datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json"
artifact_a="$tmpdir/comparison-a.json"
artifact_b="$tmpdir/comparison-b.json"

dotnet run --project src/Sim.Cli -- compare-files \
  "$baseline" \
  "$candidate" \
  -o "$artifact_a" >/dev/null
dotnet run --project src/Sim.Cli -- compare-files \
  "$baseline" \
  "$candidate" \
  -o "$artifact_b" >/dev/null

cmp "$artifact_a" "$artifact_b"
cmp "$artifact_a" "$golden"

grep -n '"schema_version": "comparison_artifact.v1"' "$artifact_a" >/dev/null
grep -n '"baseline"' "$artifact_a" >/dev/null
grep -n '"candidate"' "$artifact_a" >/dev/null
grep -n '"deltas"' "$artifact_a" >/dev/null
grep -n '"finished_at_ms"' "$artifact_a" >/dev/null
grep -n '"total_work_item_throughput_per_hour"' "$artifact_a" >/dev/null

echo "smoke-comparison-artifact PASS"
