# MovementArtifact Report Consumption Boundary

## Status

REPORT-R2a-consumption-plan is documentation only.

It plans how reports may eventually consume MovementArtifact v1 without making
movement output customer-facing by default. It does not modify Sim.Report,
Sim.Cli, Sim.Core, contracts, datasets, golden artifacts, scripts, CI, Unity,
Track C ingestion, or runtime orchestration.

The earlier REPORT-R2a loader milestone added the minimal Sim.Report
MovementArtifact loader boundary. This document builds on that foundation by
defining the reporting boundary before any renderer or customer report section
is implemented.

UNITY-R2a defines a separate Unity consumption boundary for MovementArtifact.
Report consumption and Unity animation remain separate approval surfaces;
neither should infer movement from RunArtifact v1 position_timeline.

TRACKA-R2a adds a readiness gate that protects the report boundary: report
movement sections remain separate from MovementArtifact generation and require
a future approved implementation PR.

RunArtifact v1 `position_timeline` remains baseline layout positions, NOT
simulated movement. Reports must not infer path, route, travel, congestion,
walking distance, forklift distance, or animation semantics from RunArtifact v1
baseline layout positions.

## Current foundation

The current Track A foundation includes:

- MovementArtifact v1 schema and generated contracts.
- MovementArtifact contract fixtures and contract tests.
- Sim.Report MovementArtifact loader tests and loader smoke coverage.
- Deterministic MovementArtifact generator and input adapter in Sim.Core.
- Opt-in `export-movement-artifact` CLI command.
- Controlled sample-small-warehouse MovementArtifact golden baseline.

This is enough to plan report consumption, but it is not enough to claim
calibrated, customer-ready movement intelligence.

## Report consumption principle

Reports may consume MovementArtifact only as an explicit, separate input. A
future report command must make the MovementArtifact path visible in the command
surface and in report provenance. MovementArtifact must not be silently inferred
from RunArtifact v1, ComparisonArtifact v1, or current position timeline fields.

Allowed future report inputs:

- existing RunArtifact v1 for KPI, resource, inventory, and baseline handoff.
- existing ComparisonArtifact v1 for A/B deltas.
- optional MovementArtifact v1 for separately approved movement sections.

Forbidden report inputs for movement claims:

- RunArtifact v1 `position_timeline`.
- layout resources alone.
- customer report Markdown generated before MovementArtifact approval.
- Unity playback state.
- Track C ingestion data unless a future explicit adapter and validation plan
  is approved.

## Future report surface

A future renderer may add an opt-in surface similar to:

```bash
dotnet run --project src/Sim.Cli -- render-report \
  datasets/sample-small-warehouse/artifacts/run-artifact.v1.json \
  datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json \
  -o /tmp/customer-report.v1.md \
  --movement-artifact datasets/sample-small-warehouse/artifacts/movement-artifact.v1.json
```

This is a proposal only. Current `render-report` behavior must remain unchanged
until a separately reviewed implementation PR approves:

- the CLI flag shape.
- report wording.
- provenance fields.
- golden report update strategy.
- rollback behavior.
- customer impact statement.

## Report wording levels

MovementArtifact report output should progress through explicit levels rather
than jumping directly to customer recommendations.

### Level 0: no report rendering

Current state. MovementArtifact can be generated, loaded, smoked, and compared
to its golden baseline, but customer reports do not show movement sections.

### Level 1: provenance-only internal rendering

Future internal-only report output may state that a MovementArtifact was
provided and loaded successfully. It should not compute movement KPIs or
recommendations.

Required wording:

- MovementArtifact schema version.
- scenario id and run id.
- source run artifact label/path, if present.
- graph source and generator version.
- validation level.
- explicit note that RunArtifact v1 position timeline remains baseline layout
  positions, NOT simulated movement.

### Level 2: movement summary rendering

Future movement summary rendering may aggregate MovementArtifact fields such as
actor count, route segment count, movement event count, total planned distance,
and route duration only after separate approval.

It must label the result according to its evidence level. For the current
fixture-scale generator, wording should be closer to "deterministic planning
baseline" than "measured warehouse movement".

### Level 3: customer-facing movement recommendations

Customer-facing movement recommendations require later calibration and pilot
evidence. They must not be based solely on the current fixture-scale generator
or the current RunArtifact v1 position timeline.

Before this level is allowed, the product needs:

- real path movement semantics.
- calibration and confidence policy.
- customer wording approval.
- report golden update.
- rollback plan.
- validation that movement metrics are not presented as measured WMS truth.

## Minimum provenance requirements

Any future report section that consumes MovementArtifact should include:

- `schema_version`
- `artifact_kind`
- `scenario_id`
- `run_id`
- `source_run_artifact`
- `graph_source`
- `generator_version`
- whether the section is internal, preview, or customer-facing.
- whether the movement data is calibrated, uncalibrated, fixture-scale, or
  pilot-validated.

The report must keep deterministic simulation output separate from calibrated
warehouse evidence. This matters because the product promise is credible,
visible, confidence-labeled evaluation for real warehouse customers, not a
movement-looking demo.

## Golden policy

No customer report golden should change in this planning PR.

A future report implementation PR that changes report golden files must follow
`docs/architecture/golden-update-policy.md` and include:

- before/after golden diff summary.
- customer impact statement.
- regeneration commands.
- rollback instructions.
- explicit movement honesty wording.

MovementArtifact golden baselines do not automatically authorize customer
report movement sections.

## Non-goals

This plan does not:

- implement report rendering.
- add `--movement-artifact` to any CLI command.
- update customer report golden files.
- update RunArtifact v1 or ComparisonArtifact v1.
- change MovementArtifact v1 schema.
- change generated contracts.
- change Sim.Core generation behavior.
- change Sim.Report production code.
- add Unity animation.
- add Track C ingestion behavior.
- add runtime orchestration behavior.
- claim real-world calibrated movement, travel time, congestion, or
  optimization recommendations.

## Acceptance checklist for a future implementation

- [ ] MovementArtifact input is explicit, never inferred from RunArtifact v1.
- [ ] Report provenance identifies schema, run, graph, and generator.
- [ ] Customer-facing wording states calibration/confidence level.
- [ ] RunArtifact v1 `position_timeline` remains baseline layout positions,
      NOT simulated movement.
- [ ] Report golden updates are isolated in a dedicated PR.
- [ ] Rollback keeps MovementArtifact report sections disabled without changing
      RunArtifact v1 or ComparisonArtifact v1.
- [ ] No Unity animation claims are introduced by report wording.
