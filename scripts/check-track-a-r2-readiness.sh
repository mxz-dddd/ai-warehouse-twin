#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

echo "== Check: Track A R2 MovementArtifact readiness =="

is_unity_artifact_seam_path() {
  local path="$1"
  case "$path" in
    engine/unity/AIWarehouseTwin/Assets/Scripts/Artifact/*.cs)
      return 0
      ;;
    engine/unity/AIWarehouseTwin/Assets/Tests/EditMode/*Artifact*Tests.cs|\
engine/unity/AIWarehouseTwin/Assets/Tests/EditMode/*Loader*Tests.cs)
      return 0
      ;;
    *)
      return 1
      ;;
  esac
}

is_unity_high_risk_runtime_path() {
  local path="$1"
  case "$path" in
    engine/unity/*/Rendering/*|\
engine/unity/*/Rendering/Layout/*|\
engine/unity/*/Rendering/Actors/*|\
engine/unity/*/Actors/*|\
engine/unity/*/Animation/*|\
engine/unity/*.unity|\
engine/unity/*.prefab|\
engine/unity/*MainScene*)
      return 0
      ;;
    *)
      return 1
      ;;
  esac
}

line_references_sim_core_from_unity() {
  local line="$1"
  [[ "$line" =~ using[[:space:]]+Sim\.Core ]] ||
    [[ "$line" =~ Sim\.Core\. ]] ||
    [[ "$line" =~ src/Sim\.Core ]] ||
    [[ "$line" =~ PathGraph ]] ||
    [[ "$line" =~ PathRoute ]]
}

line_derives_movement_from_position_timeline() {
  local line="$1"
  [[ "$line" =~ RunArtifact\.position_timeline ]] ||
    [[ "$line" =~ position_timeline.*(movement|Movement|route|Route|trajectory|Trajectory|actor|Actor) ]]
}

check_unity_movement_artifact_boundary() {
  local failed=0
  local match path line

  while IFS= read -r match; do
    path="${match%%:*}"
    line="${match#*:}"
    line="${line#*:}"

    if is_unity_high_risk_runtime_path "$path"; then
      echo "Unexpected MovementArtifact Unity runtime/scene implementation: $path"
      failed=1
      continue
    fi

    if is_unity_artifact_seam_path "$path"; then
      continue
    fi

    echo "Unexpected MovementArtifact Unity implementation outside artifact DTO/loader seam: $path"
    failed=1
  done < <(grep -RInE "MovementArtifact|movement-artifact" engine/unity 2>/dev/null || true)

  if [[ "$failed" -ne 0 ]]; then
    exit 1
  fi
}

check_unity_core_reference_boundary() {
  local failed=0
  local match path line

  while IFS= read -r match; do
    path="${match%%:*}"
    line="${match#*:}"
    line="${line#*:}"

    if line_references_sim_core_from_unity "$line"; then
      echo "Unexpected Sim.Core or PathGraph/PathRoute reference from Unity: $path"
      failed=1
    fi
  done < <(grep -RInE "using[[:space:]]+Sim\\.Core|Sim\\.Core\\.|src/Sim\\.Core|PathGraph|PathRoute" \
    engine/unity/AIWarehouseTwin/Assets/Scripts \
    engine/unity/AIWarehouseTwin/Assets/Tests 2>/dev/null || true)

  if [[ "$failed" -ne 0 ]]; then
    exit 1
  fi
}

check_unity_position_timeline_boundary() {
  local failed=0
  local match path line

  while IFS= read -r match; do
    path="${match%%:*}"
    line="${match#*:}"
    line="${line#*:}"

    if line_derives_movement_from_position_timeline "$line"; then
      echo "Unexpected Unity movement derivation from RunArtifact.position_timeline: $path"
      failed=1
    fi
  done < <(grep -RInE "RunArtifact\\.position_timeline|position_timeline.*(movement|Movement|route|Route|trajectory|Trajectory|actor|Actor)" \
    engine/unity/AIWarehouseTwin/Assets/Scripts \
    engine/unity/AIWarehouseTwin/Assets/Tests 2>/dev/null || true)

  if [[ "$failed" -ne 0 ]]; then
    exit 1
  fi
}

run_self_test() {
  local allowed_paths=(
    "engine/unity/AIWarehouseTwin/Assets/Scripts/Artifact/MovementArtifactDto.cs"
    "engine/unity/AIWarehouseTwin/Assets/Scripts/Artifact/MovementArtifactLoader.cs"
    "engine/unity/AIWarehouseTwin/Assets/Tests/EditMode/RunArtifactLoaderTests.cs"
  )

  local blocked_runtime_paths=(
    "engine/unity/AIWarehouseTwin/Assets/Scripts/Rendering/Actors/MovementArtifactActorAnimator.cs"
    "engine/unity/AIWarehouseTwin/Assets/Scripts/Animation/MovementArtifactAnimationPlayer.cs"
    "engine/unity/AIWarehouseTwin/Assets/Scenes/MainScene.unity"
    "engine/unity/AIWarehouseTwin/Assets/Prefabs/MovementActor.prefab"
  )

  local path
  for path in "${allowed_paths[@]}"; do
    if ! is_unity_artifact_seam_path "$path"; then
      echo "Self-test failed: expected allowed artifact seam path: $path"
      exit 1
    fi
  done

  for path in "${blocked_runtime_paths[@]}"; do
    if ! is_unity_high_risk_runtime_path "$path"; then
      echo "Self-test failed: expected high-risk Unity runtime path: $path"
      exit 1
    fi
  done

  if ! line_references_sim_core_from_unity "using Sim.Core;"; then
    echo "Self-test failed: expected using Sim.Core to be blocked"
    exit 1
  fi

  if ! line_references_sim_core_from_unity "var graph = Sim.Core.Spatial.PathGraph.Create();"; then
    echo "Self-test failed: expected Sim.Core.Spatial.PathGraph to be blocked"
    exit 1
  fi

  if ! line_derives_movement_from_position_timeline "var route = RunArtifact.position_timeline;"; then
    echo "Self-test failed: expected RunArtifact.position_timeline movement derivation to be blocked"
    exit 1
  fi

  if ! line_derives_movement_from_position_timeline "position_timeline to route"; then
    echo "Self-test failed: expected position_timeline to route derivation to be blocked"
    exit 1
  fi

  echo "PASS: Track A R2 readiness gate policy self-test"
}

if [[ "${1:-}" == "--self-test" ]]; then
  run_self_test
  exit 0
fi

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

check_unity_movement_artifact_boundary
check_unity_core_reference_boundary
check_unity_position_timeline_boundary

if grep -RIn "movement section\|MovementArtifact.*section\|--movement-artifact" src/Sim.Report >/dev/null 2>&1; then
  echo "Unexpected MovementArtifact report section implementation detected under src/Sim.Report."
  exit 1
fi

echo "PASS: Track A R2 MovementArtifact readiness"
