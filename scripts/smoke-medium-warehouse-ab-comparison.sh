#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

first="$tmp_dir/first"
second="$tmp_dir/second"

dotnet run --no-build --project src/Sim.Cli -- \
  export-medium-ab-comparison \
  datasets/medium-warehouse/scenario.json \
  datasets/medium-warehouse/optimized/abc-slotting-config.json \
  --baseline-run-artifact datasets/medium-warehouse/artifacts/run-artifact.v1.json \
  --output-dir "$first" >/dev/null

dotnet run --no-build --project src/Sim.Cli -- \
  export-medium-ab-comparison \
  datasets/medium-warehouse/scenario.json \
  datasets/medium-warehouse/optimized/abc-slotting-config.json \
  --baseline-run-artifact datasets/medium-warehouse/artifacts/run-artifact.v1.json \
  --output-dir "$second" >/dev/null

for relative_path in \
  scenario.json \
  artifacts/run-artifact.v1.json \
  artifacts/movement-artifact.v1.json \
  artifacts/comparison-artifact.v1.json
do
  cmp "$first/$relative_path" "$second/$relative_path"
  cmp "$first/$relative_path" "datasets/medium-warehouse/optimized/$relative_path"
done

python3 - <<'PY'
import json
from pathlib import Path

artifact = json.loads(Path("datasets/medium-warehouse/optimized/artifacts/comparison-artifact.v1.json").read_text())
required = [
    "schema_version",
    "baseline",
    "candidate",
    "deltas",
    "kpi_deltas",
    "improvement_pct",
    "baseline_run_id",
    "optimized_run_id",
    "optimization_note",
    "evidence_level",
]
missing = [name for name in required if name not in artifact]
if missing:
    raise SystemExit(f"Missing medium A/B comparison fields: {missing}")

if artifact["baseline_run_id"] != "medium-warehouse":
    raise SystemExit("Unexpected baseline_run_id")
if artifact["optimized_run_id"] != "medium-warehouse-optimized-abc-slotting":
    raise SystemExit("Unexpected optimized_run_id")
if artifact["evidence_level"] != "deterministic_modeled":
    raise SystemExit("Unexpected evidence_level")
if "globally optimal" not in artifact["optimization_note"]:
    raise SystemExit("Missing global-optimality disclaimer")
if len(artifact["kpi_deltas"]) < 3:
    raise SystemExit("Expected at least three KPI deltas")
if len(artifact["improvement_pct"]) < 3:
    raise SystemExit("Expected at least three improvement_pct values")

print("PASS: medium warehouse A/B comparison artifact is deterministic and complete")
PY
