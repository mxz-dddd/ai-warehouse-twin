# R3 Phase-1 Demo Handoff and Honesty Notes

This handoff describes the R3 Phase-1 medium warehouse demo as of
`origin/main` commit `29a77513d9a5631d3bf264c9163dcc06b59e6d51`.
It is a docs-only guide for demo operators. It does not add or regenerate
artifacts, change contracts, or change Unity behavior.

## Demo Status and Scope

The R3 demo is an artifact-backed deterministic demo for:

- the `datasets/medium-warehouse` baseline scenario;
- the `datasets/medium-warehouse/optimized` ABC-slotting candidate scenario;
- committed RunArtifact, MovementArtifact, and ComparisonArtifact files;
- Unity handoff seams for B1 warehouse layout, B2 actor movement, and B3 A/B
  Showcase; and
- customer-facing KPI discussion grounded in deterministic simulation output.

This is not a production WMS integration, not a sensor-calibrated positioning
system, and not a globally optimal warehouse optimization system. The demo
should be described as deterministic modeled simulation with explicit evidence
labels and caveats.

Current main includes the relevant R3 Phase-1 pieces:

- B0 Unity R3 DTO / loader seam: `RunArtifactLoader`,
  `MovementArtifactLoader`, and `ComparisonArtifactLoader`.
- B1 warehouse floor / graph / layout renderer.
- B2 actor animation timeline consuming MovementArtifact route data.
- B3 mock A/B Showcase view model and presenter using ComparisonArtifact DTOs.
- A1-S5 MovementArtifact export.
- A2a `RunArtifact.warehouse_graph` export.
- A3b KPI write-through into RunArtifact.
- A4b medium baseline golden artifacts.
- A5b medium A/B comparison artifacts.

## Artifact Inventory

### RunArtifact

RunArtifact contains deterministic simulation run output. For the medium demo,
the committed baseline file is:

```text
datasets/medium-warehouse/artifacts/run-artifact.v1.json
```

The committed optimized file is:

```text
datasets/medium-warehouse/optimized/artifacts/run-artifact.v1.json
```

For R3, RunArtifact includes:

- simulation run facts and event output;
- `warehouse_graph` for static warehouse graph / floor visualization;
- `kpi_summary`, including `order_cycle_p50_ms`, `order_cycle_p90_ms`,
  `order_cycle_p95_ms`, `avg_wait_ms`, `resource_utilization`, `bottlenecks`,
  and `travel_distance_m_by_actor_type`; and
- legacy `position_timeline` fields.

Unity B1 should use `RunArtifact.warehouse_graph` through the loader seam for
layout / graph display. KPI panels may use `RunArtifact.kpi_summary`.
Do not present `RunArtifact.position_timeline` as a real movement trace.
It remains deterministic handoff data and must not be used to infer actor
movement for the customer demo.

Typical baseline regeneration command, confirmed from `src/Sim.Cli` usage and
`scripts/smoke-medium-warehouse-golden.sh`:

```bash
dotnet run --project src/Sim.Cli -- \
  export-artifact datasets/medium-warehouse/scenario.json \
  -o /tmp/medium-run-artifact.v1.json
```

The committed golden should be preferred for demos unless the operator is
explicitly validating regeneration:

```bash
bash scripts/smoke-medium-warehouse-golden.sh
```

### MovementArtifact

MovementArtifact contains deterministic modeled movement data. For the medium
demo, the committed baseline file is:

```text
datasets/medium-warehouse/artifacts/movement-artifact.v1.json
```

The committed optimized file is:

```text
datasets/medium-warehouse/optimized/artifacts/movement-artifact.v1.json
```

MovementArtifact includes:

- `warehouse_graph`;
- `movement_events`;
- `route_segments`; and
- provenance describing deterministic modeled generation.

The movement route is produced from the layout graph shortest-path /
deterministic model path. It is not a sensor-calibrated trajectory, not WMS
telemetry, and not a real forklift, worker, or AGV positioning record.

Typical baseline regeneration command, confirmed from
`scripts/smoke-medium-warehouse-golden.sh`:

```bash
dotnet run --project src/Sim.Cli -- \
  export-movement-artifact datasets/medium-warehouse/scenario.json \
  -o /tmp/medium-movement-artifact.v1.json \
  --run-id medium-warehouse \
  --source-run-artifact datasets/medium-warehouse/artifacts/run-artifact.v1.json \
  --graph-source medium-warehouse-layout \
  --generator-version cli-a4b
```

Unity B2 should consume `MovementArtifact.route_segments` and
`MovementArtifact.movement_events`. It must not infer movement from
`RunArtifact.position_timeline`.

### ComparisonArtifact

ComparisonArtifact contains baseline vs candidate comparison data. For the
medium A/B demo, the committed file is:

```text
datasets/medium-warehouse/optimized/artifacts/comparison-artifact.v1.json
```

ComparisonArtifact uses the DTO field name `candidate`. The UI may label this
side as "Optimized", but the artifact field remains `candidate`; do not invent
an `optimized` DTO field.

For R3, the artifact includes:

- `baseline`;
- `candidate`;
- `deltas`;
- `kpi_deltas`;
- `improvement_pct`;
- `baseline_run_id`;
- `optimized_run_id`;
- `optimization_note`; and
- `evidence_level`.

B3 A/B Showcase should use `ComparisonArtifactLoader` and read `candidate`,
`kpi_deltas`, and `improvement_pct` from the DTO. It should not recompute real
KPI deltas or real improvement percentages in Unity.

The medium optimized directory already contains the A5b committed
ComparisonArtifact. To validate deterministic regeneration, use:

```bash
bash scripts/smoke-medium-warehouse-ab-comparison.sh
```

The command shape used by that smoke is:

```bash
dotnet run --project src/Sim.Cli -- \
  export-medium-ab-comparison \
  datasets/medium-warehouse/scenario.json \
  datasets/medium-warehouse/optimized/abc-slotting-config.json \
  --baseline-run-artifact datasets/medium-warehouse/artifacts/run-artifact.v1.json \
  --output-dir /tmp/medium-warehouse-optimized
```

That command writes a derived optimized `scenario.json`,
`artifacts/run-artifact.v1.json`, `artifacts/movement-artifact.v1.json`, and
`artifacts/comparison-artifact.v1.json` under the chosen output directory.

## Unity Loading Handoff

The Unity side has loader/view-model seams, but the exact runtime loading path
depends on the active demo scene / runner wiring. Do not claim that every scene
is already wired unless the current demo scene has been checked in that task.

Use this artifact mapping:

- B1 warehouse floor / graph / layout: load RunArtifact through
  `RunArtifactLoader`; use `warehouse_graph` for graph and floor layout.
- KPI display: load RunArtifact through `RunArtifactLoader`; use
  `kpi_summary` for P50 / P90 / P95 cycle times, waiting time, utilization,
  bottlenecks, and travel distance fields.
- B2 actor movement: load MovementArtifact through `MovementArtifactLoader`;
  use `route_segments` and `movement_events`.
- B3 A/B Showcase: load ComparisonArtifact through `ComparisonArtifactLoader`;
  use `candidate`, `kpi_deltas`, and `improvement_pct`.

Existing Unity source and tests show a default `StreamingAssets` convention for
`run-artifact.v1.json`. If the demo needs medium artifacts in Unity, place or
wire the selected artifact files through the current demo scene / runner
convention in a separate integration task. This handoff does not authorize
Unity Scene, prefab, ProjectSettings, or feature-code changes.

## Evidence and Honesty Notes

Use these statements in customer-facing narration:

- Movement is deterministic modeled movement.
- Movement is not a sensor-calibrated trajectory.
- No real WMS telemetry is used in this demo.
- ABC slotting is a deterministic demo heuristic.
- ABC slotting does not guarantee a globally optimal slotting solution.
- A/B improvements are from deterministic simulation artifacts.
- A/B improvements are not measured WMS production results.
- The medium dataset is demo data unless a specific artifact is identified as
  a committed golden artifact.
- KPI values are deterministic simulation outputs, not audited operational
  KPIs.
- ComparisonArtifact `candidate` is the optimized scenario artifact, but
  "optimized" does not mean globally optimal.

The current A5b medium ComparisonArtifact honestly records zero
`improvement_pct` values for the tracked KPI set. Do not invent a gain for the
demo narrative.

## Customer Demo Flow

1. Start with the medium warehouse scenario.
   - Say that it is an R3 Phase-1 deterministic demo dataset.
   - Point to the baseline RunArtifact and MovementArtifact as committed
     demo artifacts.
2. Show B1 warehouse floor / graph / layout.
   - Use the RunArtifact `warehouse_graph`.
   - Explain that this is a deterministic layout graph for visualization.
3. Show B2 actor movement timeline.
   - Use MovementArtifact `route_segments` and `movement_events`.
   - Say "deterministic modeled movement"; do not say "sensor trajectory".
4. Show KPIs.
   - Include P50 / P90 / P95 order cycle time.
   - Include waiting time.
   - Include resource utilization.
   - Include bottlenecks.
   - State that these are deterministic simulation KPIs.
5. Show B3 A/B Showcase.
   - Show baseline side.
   - Show candidate / Optimized side.
   - Show `kpi_deltas`.
   - Show `improvement_pct`.
   - Note that the medium A5b artifact currently records 0% improvement for
     tracked KPIs rather than fabricating a benefit.
6. Close with honesty boundaries.
   - Deterministic simulation.
   - Not WMS measurement.
   - Not sensor calibrated.
   - Heuristic optimization, not global optimization.

## Do / Don't

Do:

- Use the artifact-backed demo flow.
- Preserve source and evidence labels.
- Say "deterministic modeled movement".
- Say "demo heuristic".
- Say "simulation-based improvement".
- Use the committed medium artifacts for customer demo consistency.

Don't:

- Don't say "sensor trajectory".
- Don't say "WMS measured improvement".
- Don't say "globally optimal".
- Don't infer movement from `RunArtifact.position_timeline`.
- Don't present mock / fixture data as real comparison.
- Don't describe deterministic KPI values as audited operational KPIs.

## Quick Operator Checklist

- Baseline RunArtifact:
  `datasets/medium-warehouse/artifacts/run-artifact.v1.json`
- Baseline MovementArtifact:
  `datasets/medium-warehouse/artifacts/movement-artifact.v1.json`
- Optimized RunArtifact:
  `datasets/medium-warehouse/optimized/artifacts/run-artifact.v1.json`
- Optimized MovementArtifact:
  `datasets/medium-warehouse/optimized/artifacts/movement-artifact.v1.json`
- Medium ComparisonArtifact:
  `datasets/medium-warehouse/optimized/artifacts/comparison-artifact.v1.json`
- Baseline artifact smoke:
  `bash scripts/smoke-medium-warehouse-golden.sh`
- A/B comparison smoke:
  `bash scripts/smoke-medium-warehouse-ab-comparison.sh`

Keep this handoff as a narration and operating guide. Any Unity wiring,
artifact regeneration, or Final Unity integration smoke should be handled in a
separate task with a clean worktree.
