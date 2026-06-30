#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

echo "== Check: Track A R2 MovementArtifact readiness =="

required_files=(
  "packages/contracts/artifacts/movement-artifact.v1.schema.json"
  "packages/contracts/generated/csharp/Contracts.Generated.cs"
  "packages/contracts/generated/python/contracts_generated.py"
  "packages/contracts/generated/manifest.json"
  "datasets/sample-small-warehouse/artifacts/movement-artifact.v1.json"
  "scripts/smoke-movement-artifact-loader.sh"
  "scripts/smoke-movement-artifact-export.sh"
  "src/Sim.Core/Movement/MovementArtifactGenerator.cs"
  "src/Sim.Core/Movement/MovementArtifactInputAdapter.cs"
  "src/Sim.Core/Movement/MovementArtifactInputAdapterOptions.cs"
  "src/Sim.Report/MovementArtifactLoader.cs"
  "docs/architecture/movement-artifact-cli-export-plan.md"
  "docs/architecture/movement-artifact-report-consumption-boundary.md"
  "docs/architecture/movement-artifact-unity-consumption-boundary.md"
  "docs/architecture/movement-artifact-v1-proposal.md"
  "docs/architecture/track-a-r2-readiness-audit.md"
)

for path in "${required_files[@]}"; do
  test -f "$path"
done

grep -RIn "baseline layout positions, NOT simulated movement" docs/architecture >/dev/null
grep -RIn "export-movement-artifact" src/Sim.Cli docs/architecture scripts >/dev/null
grep -RIn "MovementArtifactInputAdapter" src/Sim.Core src/Sim.Core.Tests docs/architecture >/dev/null
grep -RIn "MovementArtifactGenerator" src/Sim.Core src/Sim.Core.Tests docs/architecture >/dev/null
grep -RIn "MovementArtifactLoader" src/Sim.Report src/Sim.Report.Tests src/Sim.Core.Tests docs/architecture scripts >/dev/null
grep -RIn "movement-artifact.v1" packages/contracts datasets/sample-small-warehouse/artifacts docs/architecture >/dev/null
grep -RIn "fixture-scale" docs/architecture >/dev/null
grep -RIn "Report movement sections require separate implementation PR" docs/architecture >/dev/null
grep -RIn "Unity movement animation requires separate implementation PR" docs/architecture >/dev/null

if grep -RIn "MovementArtifact\|movement-artifact" engine/unity >/dev/null 2>&1; then
  echo "Unexpected MovementArtifact Unity implementation detected under engine/unity."
  exit 1
fi

if grep -RIn "movement section\|MovementArtifact.*section\|--movement-artifact" src/Sim.Report >/dev/null 2>&1; then
  echo "Unexpected MovementArtifact report section implementation detected under src/Sim.Report."
  exit 1
fi

echo "PASS: Track A R2 MovementArtifact readiness"
