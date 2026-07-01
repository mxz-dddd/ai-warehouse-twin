#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="/usr/bin:$DOTNET_ROOT:$PATH"

echo "== Smoke: medium warehouse baseline golden artifacts =="

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

scenario="datasets/medium-warehouse/scenario.json"
golden_run="datasets/medium-warehouse/artifacts/run-artifact.v1.json"
golden_movement="datasets/medium-warehouse/artifacts/movement-artifact.v1.json"

run_a="$tmpdir/run-artifact-a.v1.json"
run_b="$tmpdir/run-artifact-b.v1.json"
movement_a="$tmpdir/movement-artifact-a.v1.json"
movement_b="$tmpdir/movement-artifact-b.v1.json"

dotnet run --project src/Sim.Cli -- \
  export-artifact "$scenario" \
  -o "$run_a" >/dev/null

dotnet run --project src/Sim.Cli -- \
  export-artifact "$scenario" \
  -o "$run_b" >/dev/null

cmp "$run_a" "$run_b"
cmp "$run_a" "$golden_run"

dotnet run --project src/Sim.Cli -- \
  export-movement-artifact "$scenario" \
  -o "$movement_a" \
  --run-id medium-warehouse \
  --source-run-artifact "$golden_run" \
  --graph-source medium-warehouse-layout \
  --generator-version cli-a4b >/dev/null

dotnet run --project src/Sim.Cli -- \
  export-movement-artifact "$scenario" \
  -o "$movement_b" \
  --run-id medium-warehouse \
  --source-run-artifact "$golden_run" \
  --graph-source medium-warehouse-layout \
  --generator-version cli-a4b >/dev/null

cmp "$movement_a" "$movement_b"
cmp "$movement_a" "$golden_movement"

python3 - "$run_a" "$movement_a" <<'PY'
import json
import sys
from pathlib import Path

run = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
movement = json.loads(Path(sys.argv[2]).read_text(encoding="utf-8"))
errors = []

warehouse_graph = run.get("warehouse_graph")
if not warehouse_graph:
    errors.append("RunArtifact missing warehouse_graph")
else:
    nodes = warehouse_graph.get("nodes") or []
    edges = warehouse_graph.get("edges") or []
    if not nodes:
        errors.append("warehouse_graph.nodes is empty")
    if not edges:
        errors.append("warehouse_graph.edges is empty")
    node_ids = {node.get("node_id") for node in nodes}
    for edge in edges:
        if edge.get("from_node_id") not in node_ids:
            errors.append(f"warehouse_graph edge has unknown from_node_id: {edge.get('edge_id')}")
        if edge.get("to_node_id") not in node_ids:
            errors.append(f"warehouse_graph edge has unknown to_node_id: {edge.get('edge_id')}")

kpi = run.get("kpi_summary")
required_kpi_fields = [
    "order_cycle_p50_ms",
    "order_cycle_p90_ms",
    "order_cycle_p95_ms",
    "avg_wait_ms",
    "resource_utilization",
    "bottlenecks",
    "travel_distance_m_by_actor_type",
]
if not kpi:
    errors.append("RunArtifact missing kpi_summary")
else:
    missing = [field for field in required_kpi_fields if field not in kpi]
    if missing:
        errors.append(f"kpi_summary missing fields: {missing}")

    utilization = kpi.get("resource_utilization")
    if not isinstance(utilization, dict):
        errors.append("resource_utilization is not an object")
    else:
        for key, value in utilization.items():
            if not isinstance(value, (int, float)) or value < 0 or value > 100:
                errors.append(f"resource_utilization[{key}] is not percent 0..100: {value}")

    bottlenecks = kpi.get("bottlenecks")
    if not isinstance(bottlenecks, list):
        errors.append("bottlenecks is not an array")
    else:
        for bottleneck in bottlenecks:
            value = bottleneck.get("utilization")
            if value is not None and (not isinstance(value, (int, float)) or value < 0 or value > 100):
                errors.append(
                    f"bottleneck utilization is not percent 0..100: {bottleneck.get('resource_id')}"
                )

    travel = kpi.get("travel_distance_m_by_actor_type")
    if not isinstance(travel, dict):
        errors.append("travel_distance_m_by_actor_type is not an object")
    elif travel:
        errors.append(f"travel_distance_m_by_actor_type should remain unfilled by A4b: {travel}")

route_segments = movement.get("route_segments") or []
if not route_segments:
    errors.append("MovementArtifact route_segments is empty")

for index, segment in enumerate(route_segments):
    if not segment.get("from_node_id") or not segment.get("to_node_id"):
        errors.append(f"route segment {index} missing from/to node")
    if segment.get("end_ms", -1) < segment.get("start_ms", 0):
        errors.append(f"route segment {index} ends before it starts")
    if segment.get("distance_m", -1) <= 0:
        errors.append(f"route segment {index} has non-positive distance")

for previous, current in zip(route_segments, route_segments[1:]):
    if previous.get("actor_id") == current.get("actor_id"):
        if previous.get("end_ms") != current.get("start_ms"):
            errors.append("route segment time discontinuity")
        if previous.get("to_node_id") != current.get("from_node_id"):
            errors.append("route segment node discontinuity")

provenance_text = json.dumps(movement.get("provenance") or {}, sort_keys=True).lower()
if "deterministic" not in provenance_text:
    errors.append("movement provenance does not mention deterministic generation")
for forbidden in [
    "sensor trajectory",
    "observed movement",
    "ground-truth trajectory",
    "ground truth trajectory",
]:
    if forbidden in provenance_text:
        errors.append(f"movement provenance claims real trajectory: {forbidden}")

if errors:
    for error in errors:
        print(error, file=sys.stderr)
    raise SystemExit(1)
PY

echo "PASS: medium warehouse baseline golden smoke"
