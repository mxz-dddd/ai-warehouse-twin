#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

echo "== Smoke: MovementArtifact loader boundary =="

dotnet test src/Sim.Report.Tests/Sim.Report.Tests.csproj \
  --filter "FullyQualifiedName~MovementArtifactLoaderTests"

echo "PASS: MovementArtifact loader smoke"
