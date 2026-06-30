#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

python_cmd="${PYTHON:-python3}"

ruff check services/runtime
mypy services/runtime
pytest services/runtime

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

scenario_path="$repo_root/datasets/validation-cases/valid/outbound-only.json"

(
  cd services/runtime
  "$python_cmd" -m runtime_service --help >/dev/null
  "$python_cmd" -m runtime_service run-dry \
    --scenario "$scenario_path" \
    --output "$tmpdir/run-1" >/dev/null
  "$python_cmd" -m runtime_service run-dry \
    --scenario "$scenario_path" \
    --output "$tmpdir/run-2" >/dev/null
  "$python_cmd" -m runtime_service plan-simcli \
    --scenario "$scenario_path" \
    --output "$tmpdir/plan-1" >/dev/null
  "$python_cmd" -m runtime_service plan-simcli \
    --scenario "$scenario_path" \
    --output "$tmpdir/plan-2" >/dev/null
)

test -f "$tmpdir/run-1/runtime-result.json"
test -f "$tmpdir/run-1/artifact-index.json"
test -f "$tmpdir/run-2/runtime-result.json"
test -f "$tmpdir/run-2/artifact-index.json"
test -f "$tmpdir/plan-1/simcli-plan.json"
test -f "$tmpdir/plan-1/artifact-index.json"
test -f "$tmpdir/plan-2/simcli-plan.json"
test -f "$tmpdir/plan-2/artifact-index.json"

cmp "$tmpdir/run-1/runtime-result.json" "$tmpdir/run-2/runtime-result.json"
cmp "$tmpdir/run-1/artifact-index.json" "$tmpdir/run-2/artifact-index.json"
cmp "$tmpdir/plan-1/simcli-plan.json" "$tmpdir/plan-2/simcli-plan.json"
cmp "$tmpdir/plan-1/artifact-index.json" "$tmpdir/plan-2/artifact-index.json"

if grep -R -E "$repo_root|$tmpdir|[0-9]{4}-[0-9]{2}-[0-9]{2}|T[0-9]{2}:[0-9]{2}|[0-9a-f]{32,}" \
  "$tmpdir/run-1" "$tmpdir/run-2" "$tmpdir/plan-1" "$tmpdir/plan-2" >/dev/null; then
  echo "runtime smoke output contains local path or timestamp noise" >&2
  exit 1
fi

echo "smoke-runtime PASS"
