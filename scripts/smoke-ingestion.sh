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
)

test -f "$tmpdir/csv-basic/scenario.json"
test -f "$tmpdir/csv-basic/data-quality-report.md"
cmp "$tmpdir/csv-basic/scenario.json" \
  "$repo_root/datasets/ingestion-cases/csv-basic/expected/scenario.json"
cmp "$tmpdir/csv-basic/data-quality-report.md" \
  "$repo_root/datasets/ingestion-cases/csv-basic/expected/data-quality-report.md"

bash scripts/check-ingestion-no-src.sh

echo "smoke-ingestion PASS"
