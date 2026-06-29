#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

run_artifact="datasets/sample-small-warehouse/artifacts/run-artifact.v1.json"
comparison_artifact="datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json"
report_unified="$tmpdir/report-unified.md"
report_mixed="$tmpdir/report-mixed.md"
bad_output="$tmpdir/bad-runner.stderr"
partial_output="$tmpdir/partial-runner.stderr"

dotnet run --project src/Sim.Cli -- render-report \
  "$run_artifact" \
  "$comparison_artifact" \
  -o "$report_unified" \
  --run-runner unified \
  --comparison-runner unified >/dev/null

grep -n "Runner Provenance" "$report_unified" >/dev/null
grep -n "Run artifact runner: unified" "$report_unified" >/dev/null
grep -n "Comparison artifact runner: unified" "$report_unified" >/dev/null
grep -n "Provenance source: operator-provided render-report flags" "$report_unified" >/dev/null

echo "PASS: customer report runner provenance rendered"

dotnet run --project src/Sim.Cli -- render-report \
  "$run_artifact" \
  "$comparison_artifact" \
  -o "$report_mixed" \
  --run-runner unified \
  --comparison-runner legacy >/dev/null

grep -n "Warning: Run artifact and comparison artifact were generated with different runner modes." "$report_mixed" >/dev/null

echo "PASS: mixed runner provenance warning rendered"

if dotnet run --project src/Sim.Cli -- render-report \
  "$run_artifact" \
  "$comparison_artifact" \
  -o "$tmpdir/report-bad.md" \
  --run-runner bad-value \
  --comparison-runner unified > /dev/null 2> "$bad_output"; then
    echo "FAIL: bad runner provenance unexpectedly succeeded"
    exit 1
fi

if ! grep -q "Unknown runner mode 'bad-value'. Allowed values: legacy, unified, unknown." "$bad_output"; then
    echo "FAIL: bad runner provenance did not print the expected error"
    cat "$bad_output"
    exit 1
fi

echo "PASS: bad runner provenance rejected"

if dotnet run --project src/Sim.Cli -- render-report \
  "$run_artifact" \
  "$comparison_artifact" \
  -o "$tmpdir/report-partial.md" \
  --run-runner unified > /dev/null 2> "$partial_output"; then
    echo "FAIL: partial runner provenance unexpectedly succeeded"
    exit 1
fi

if ! grep -q "Both --run-runner and --comparison-runner are required when providing runner provenance." "$partial_output"; then
    echo "FAIL: partial runner provenance did not print the expected error"
    cat "$partial_output"
    exit 1
fi

echo "PASS: partial runner provenance rejected"
echo "PASS: customer report runner provenance smoke"
