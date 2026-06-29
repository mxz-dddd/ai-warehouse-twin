#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

run_artifact="datasets/sample-small-warehouse/artifacts/run-artifact.v1.json"
comparison_artifact="datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json"
golden="datasets/sample-small-warehouse/artifacts/customer-report.v1.md"
report_a="$tmpdir/customer-report-a.md"
report_b="$tmpdir/customer-report-b.md"

dotnet run --project src/Sim.Cli -- render-report \
  "$run_artifact" \
  "$comparison_artifact" \
  -o "$report_a" >/dev/null
dotnet run --project src/Sim.Cli -- render-report \
  "$run_artifact" \
  "$comparison_artifact" \
  -o "$report_b" >/dev/null

cmp "$report_a" "$report_b"
cmp "$report_a" "$golden"

grep -n '^# AI Warehouse Twin Report$' "$report_a" >/dev/null
grep -n '^## Run Summary$' "$report_a" >/dev/null
grep -n '^## Artifact Handoff$' "$report_a" >/dev/null
grep -n '^## A/B Comparison Summary$' "$report_a" >/dev/null
grep -n "finished_at_ms" "$report_a" >/dev/null
grep -n "total_work_item_throughput_per_hour" "$report_a" >/dev/null

echo "smoke-customer-report PASS"
