#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

export PATH="/usr/bin:$PATH"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

echo "dotnet: $(dotnet --version)"

./scripts/check-no-unityengine.sh
./scripts/check-contract-drift.sh

dotnet build
dotnet test

bash scripts/smoke-sample-each-pick.sh
bash scripts/smoke-each-pick-export-artifact.sh
bash scripts/smoke-sample-warehouse.sh
bash scripts/smoke-sample-warehouse-run-file.sh
bash scripts/smoke-export-artifact.sh

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

warehouse_a="$tmp_dir/warehouse-a.json"
warehouse_b="$tmp_dir/warehouse-b.json"
each_pick_a="$tmp_dir/each-pick-a.json"
each_pick_b="$tmp_dir/each-pick-b.json"

dotnet run --project src/Sim.Cli -- \
  export-artifact datasets/sample-small-warehouse/scenario.json \
  -o "$warehouse_a" >/dev/null
dotnet run --project src/Sim.Cli -- \
  export-artifact datasets/sample-small-warehouse/scenario.json \
  -o "$warehouse_b" >/dev/null
cmp "$warehouse_a" "$warehouse_b"
cmp "$warehouse_a" \
  datasets/sample-small-warehouse/artifacts/run-artifact.v1.json

dotnet run --project src/Sim.Cli -- \
  export-artifact datasets/sample-each-pick/scenario.json \
  -o "$each_pick_a" >/dev/null
dotnet run --project src/Sim.Cli -- \
  export-artifact datasets/sample-each-pick/scenario.json \
  -o "$each_pick_b" >/dev/null
cmp "$each_pick_a" "$each_pick_b"

echo "PASS: full local validation"
