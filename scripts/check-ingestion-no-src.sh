#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

base_ref="${1:-origin/main}"

if ! git rev-parse --verify "$base_ref" >/dev/null 2>&1; then
  echo "FAIL: base ref '$base_ref' is not available"
  exit 1
fi

violations=()

while IFS= read -r path; do
  case "$path" in
    src/*|engine/*|packages/contracts/*|.github/workflows/ci.yml|scripts/check-all.sh|WarehouseTwin.sln|datasets/templates/scenario.schema.json)
      violations+=("$path")
      ;;
  esac
done < <(git diff --name-only "$base_ref"...HEAD)

if [ "${#violations[@]}" -ne 0 ]; then
  echo "FAIL: ingestion branch touched forbidden paths:"
  printf '  %s\n' "${violations[@]}"
  exit 1
fi

echo "PASS: ingestion branch did not touch forbidden core paths"
