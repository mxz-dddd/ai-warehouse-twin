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

(
  cd services/ingestion
  "$python_cmd" -m ingestion --help >/dev/null
  "$python_cmd" -m ingestion print-schema-info --repo-root "$repo_root" >/dev/null
  "$python_cmd" -m ingestion csv-to-scenario \
    --input "$repo_root/datasets/ingestion-cases/csv-basic/input" \
    --output "$tmpdir/csv-basic" \
    --repo-root "$repo_root" >/dev/null
  "$python_cmd" -m ingestion mock-wms-to-scenario \
    --input "$repo_root/datasets/ingestion-cases/mock-wms-basic/input" \
    --output "$tmpdir/mock-wms-basic" \
    --repo-root "$repo_root" >/dev/null
)

test -f "$tmpdir/csv-basic/scenario.json"
test -f "$tmpdir/csv-basic/data-quality-report.md"
cmp "$tmpdir/csv-basic/scenario.json" \
  "$repo_root/datasets/ingestion-cases/csv-basic/expected/scenario.json"
cmp "$tmpdir/csv-basic/data-quality-report.md" \
  "$repo_root/datasets/ingestion-cases/csv-basic/expected/data-quality-report.md"

test -f "$tmpdir/mock-wms-basic/scenario.json"
test -f "$tmpdir/mock-wms-basic/data-quality-report.md"
cmp "$tmpdir/mock-wms-basic/scenario.json" \
  "$repo_root/datasets/ingestion-cases/mock-wms-basic/expected/scenario.json"
cmp "$tmpdir/mock-wms-basic/data-quality-report.md" \
  "$repo_root/datasets/ingestion-cases/mock-wms-basic/expected/data-quality-report.md"

set +e
(
  cd services/ingestion
  "$python_cmd" -m ingestion csv-to-scenario \
    --input "$repo_root/datasets/ingestion-cases/csv-invalid-missing-column/input" \
    --output "$tmpdir/csv-invalid-missing-column" \
    --repo-root "$repo_root" \
    >"$tmpdir/csv-invalid-missing-column.stdout" \
    2>"$tmpdir/csv-invalid-missing-column.stderr"
)
invalid_status=$?
set -e

test "$invalid_status" -ne 0
test -f "$tmpdir/csv-invalid-missing-column/data-quality-report.md"
grep -q "missing_required_column" \
  "$tmpdir/csv-invalid-missing-column/data-quality-report.md"
grep -q "CSV ingestion failed" "$tmpdir/csv-invalid-missing-column.stderr"
if grep -R "Traceback" \
  "$tmpdir/csv-invalid-missing-column.stderr" \
  "$tmpdir/csv-invalid-missing-column/data-quality-report.md" >/dev/null; then
  echo "unexpected traceback in invalid CSV smoke output" >&2
  exit 1
fi

bash scripts/check-ingestion-no-src.sh

echo "smoke-ingestion PASS"
