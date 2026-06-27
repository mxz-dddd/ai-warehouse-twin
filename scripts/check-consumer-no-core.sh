#!/usr/bin/env bash
set -euo pipefail

roots=(
  "src/Sim.Report"
  "src/Sim.Validation"
  "engine/unity"
)

patterns=(
  "Sim.Core.csproj"
  "using Sim.Core"
  "Sim.Core"
)

matches_file="$(mktemp)"
trap 'rm -f "$matches_file"' EXIT

violations=0

for root in "${roots[@]}"; do
  if [ ! -d "$root" ]; then
    continue
  fi

  for pattern in "${patterns[@]}"; do
    if grep -RIn \
      --exclude-dir=bin \
      --exclude-dir=obj \
      --exclude-dir=.git \
      -- "$pattern" "$root" >"$matches_file" 2>/dev/null; then
      echo "FAIL: consumer code must not reference Sim.Core"
      echo "Root: $root"
      echo "Pattern: $pattern"
      cat "$matches_file"
      violations=1
    fi
  done
done

if [ "$violations" -ne 0 ]; then
  exit 1
fi

echo "PASS: consumer projects do not reference Sim.Core"
