#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

python_cmd="${PYTHON:-python3}"

ruff check services/ingestion
mypy services/ingestion
pytest services/ingestion

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

run_ingestion() {
  (
    cd "$repo_root/services/ingestion"
    "$python_cmd" -m ingestion "$@"
  )
}

assert_valid_outputs() {
  local actual_dir="$1"
  local expected_dir="$2"

  test -f "$actual_dir/scenario.json"
  test -f "$actual_dir/data-quality-report.md"
  cmp "$actual_dir/scenario.json" "$expected_dir/scenario.json"
  cmp "$actual_dir/data-quality-report.md" "$expected_dir/data-quality-report.md"
}

assert_byte_stable_outputs() {
  local first_dir="$1"
  local second_dir="$2"

  cmp "$first_dir/scenario.json" "$second_dir/scenario.json"
  cmp "$first_dir/data-quality-report.md" "$second_dir/data-quality-report.md"
}

assert_invalid_case() {
  local command_name="$1"
  local input_dir="$2"
  local output_dir="$3"
  local expected_code="$4"
  local expected_stderr="$5"
  local stdout_file="$6"
  local stderr_file="$7"

  set +e
  run_ingestion "$command_name" \
    --input "$input_dir" \
    --output "$output_dir" \
    --repo-root "$repo_root" \
    >"$stdout_file" \
    2>"$stderr_file"
  local invalid_status=$?
  set -e

  test "$invalid_status" -ne 0
  test -f "$output_dir/data-quality-report.md"
  grep -q "$expected_code" "$output_dir/data-quality-report.md"
  grep -q "$expected_stderr" "$stderr_file"
  if grep -R "Traceback" \
    "$stdout_file" \
    "$stderr_file" \
    "$output_dir/data-quality-report.md" >/dev/null; then
    echo "unexpected traceback in invalid smoke output for $command_name" >&2
    exit 1
  fi
}

run_ingestion --help >/dev/null
run_ingestion print-schema-info --repo-root "$repo_root" >/dev/null

run_ingestion csv-to-scenario \
  --input "$repo_root/datasets/ingestion-cases/csv-basic/input" \
  --output "$tmpdir/csv-basic-1" \
  --repo-root "$repo_root" >/dev/null
run_ingestion csv-to-scenario \
  --input "$repo_root/datasets/ingestion-cases/csv-basic/input" \
  --output "$tmpdir/csv-basic-2" \
  --repo-root "$repo_root" >/dev/null
assert_valid_outputs "$tmpdir/csv-basic-1" \
  "$repo_root/datasets/ingestion-cases/csv-basic/expected"
assert_byte_stable_outputs "$tmpdir/csv-basic-1" "$tmpdir/csv-basic-2"

run_ingestion mock-wms-to-scenario \
  --input "$repo_root/datasets/ingestion-cases/mock-wms-basic/input" \
  --output "$tmpdir/mock-wms-basic-1" \
  --repo-root "$repo_root" >/dev/null
run_ingestion mock-wms-to-scenario \
  --input "$repo_root/datasets/ingestion-cases/mock-wms-basic/input" \
  --output "$tmpdir/mock-wms-basic-2" \
  --repo-root "$repo_root" >/dev/null
assert_valid_outputs "$tmpdir/mock-wms-basic-1" \
  "$repo_root/datasets/ingestion-cases/mock-wms-basic/expected"
assert_byte_stable_outputs "$tmpdir/mock-wms-basic-1" "$tmpdir/mock-wms-basic-2"

assert_invalid_case \
  csv-to-scenario \
  "$repo_root/datasets/ingestion-cases/csv-invalid-missing-column/input" \
  "$tmpdir/csv-invalid-missing-column" \
  "missing_required_column" \
  "CSV ingestion failed" \
  "$tmpdir/csv-invalid-missing-column.stdout" \
  "$tmpdir/csv-invalid-missing-column.stderr"

assert_invalid_case \
  mock-wms-to-scenario \
  "$repo_root/datasets/ingestion-cases/mock-wms-invalid-reference/input" \
  "$tmpdir/mock-wms-invalid-reference" \
  "invalid_location_type" \
  "Mock WMS ingestion failed" \
  "$tmpdir/mock-wms-invalid-reference.stdout" \
  "$tmpdir/mock-wms-invalid-reference.stderr"

bash scripts/check-ingestion-no-src.sh

echo "smoke-ingestion PASS"
