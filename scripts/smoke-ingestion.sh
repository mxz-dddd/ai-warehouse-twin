#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

python_cmd="${PYTHON:-python3}"

ruff check services/ingestion
mypy services/ingestion
pytest services/ingestion

(
  cd services/ingestion
  "$python_cmd" -m ingestion --help >/dev/null
  "$python_cmd" -m ingestion print-schema-info --repo-root "$repo_root" >/dev/null
)

bash scripts/check-ingestion-no-src.sh

echo "smoke-ingestion PASS"
