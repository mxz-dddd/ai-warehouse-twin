#!/usr/bin/env bash
set -euo pipefail

./scripts/gen-contracts.sh

if git diff --exit-code -- packages/contracts/generated >/dev/null; then
  echo "PASS: generated contracts are up to date."
  exit 0
fi

echo "ERROR: generated contracts are out of date. Run ./scripts/gen-contracts.sh and commit the generated files."
git diff --stat -- packages/contracts/generated
exit 1
