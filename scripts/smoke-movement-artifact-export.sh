#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

echo "== Smoke: MovementArtifact CLI export golden =="

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

generated="$tmpdir/movement-artifact.v1.json"
golden="datasets/sample-small-warehouse/artifacts/movement-artifact.v1.json"

dotnet run --project src/Sim.Cli -- \
  export-movement-artifact \
  datasets/sample-small-warehouse/scenario.json \
  -o "$generated" \
  --run-id sample-small-warehouse \
  --source-run-artifact datasets/sample-small-warehouse/artifacts/run-artifact.v1.json \
  --graph-source sample-small-warehouse-layout \
  --generator-version cli-r2b

test -s "$generated"
cmp "$golden" "$generated"

dotnet test src/Sim.Report.Tests/Sim.Report.Tests.csproj \
  --filter "FullyQualifiedName~MovementArtifactLoaderTests"

echo "PASS: MovementArtifact CLI export golden"
