# MovementArtifact Runtime Boundary Plan

## Status

CORE-R2a is planning/docs only.

This plan defines the boundary for a future MovementArtifact runtime generator.
It does not implement runtime MovementArtifact generation, movement/path/route
logic, CLI export commands, report sections, Unity animation, golden artifacts,
or Track C ingestion changes.

CORE-R2b adds the first minimal in-memory deterministic MovementArtifact
generator in Sim.Core. It creates a MovementArtifact object from explicit
generator inputs only. It does not add CLI export, write files, update golden
artifacts, render report movement sections, animate Unity movement, modify
Track C ingestion, or infer movement from RunArtifact v1 position_timeline.

CORE-R2c adds generator/loader compatibility tests proving the in-memory
MovementArtifact generator output can serialize to JSON and be read by the
existing Sim.Report MovementArtifact loader. It does not add CLI export, write
tracked files, update golden artifacts, render report movement sections,
animate Unity movement, modify Track C ingestion, or modify runtime
orchestration.

CORE-R2d adds an input adapter boundary plan. It documents candidate input
sources and explicitly forbids using RunArtifact v1 position_timeline as a
route/path/movement source. It does not implement adapter code, CLI export,
file writing, golden artifacts, report movement sections, Unity animation,
Track C ingestion changes, or runtime orchestration changes.

CORE-R2e introduces an in-memory input adapter boundary for MovementArtifact
generation. The adapter remains internal and non-customer-facing; CLI export,
golden artifacts, reports, Unity, ingestion, and runtime orchestration remain
future work.

CORE-R2f verifies the internal adapter-generator-loader boundary end to end.
MovementArtifact remains non-customer-facing until a separate CLI/golden/report/Unity
policy is approved.

CLI-R2a documents the future CLI export boundary for MovementArtifact. The command remains opt-in and non-customer-default; default RunArtifact/ComparisonArtifact/report behavior remains unchanged.

CLI-R2b adds an opt-in CLI export command only. Runtime orchestration remains unchanged and must not manufacture movement semantics.

GOLDEN-R2a adds a MovementArtifact validation golden only. Runtime orchestration remains unchanged and must not manufacture movement semantics.

UNITY-R2a keeps runtime orchestration out of Unity movement semantics. Runtime
may later pass MovementArtifact references only through separately approved
explicit artifact-generation steps.

TRACKA-R2a adds a lightweight readiness gate for Track A R2. Runtime
orchestration remains unchanged and must not manufacture movement semantics.

Current RunArtifact v1 `position_timeline` remains baseline layout positions,
NOT simulated movement. It must not be presented as real movement, travel,
trajectory, route, or animation data.

## Current completed foundation

The current main branch has the following foundation in place:

- Unified runner and explicit/default unified artifact paths for existing
  RunArtifact and ComparisonArtifact flows.
- RunArtifact v1 with `layout.resources` and `position_timeline`.
- Guardrails and report tests that label RunArtifact v1 `position_timeline` as
  baseline layout positions, NOT simulated movement.
- MovementArtifact v1 schema and generated C# / Python contract surfaces.
- Test-only MovementArtifact valid and invalid contract fixtures.
- Sim.Report MovementArtifact loader boundary that loads generated
  `MovementArtifact` JSON and checks `schema_version` / `artifact_kind`.
- Script-level smoke coverage for the MovementArtifact loader boundary.
- Runtime orchestration service scaffold under `services/runtime`.
- Unity run artifact player UI state scaffold under `engine/unity`.
- Track C ingestion service and mock WMS adapter under `services/ingestion`.

These pieces make the next movement work reviewable, but they do not yet create
real movement output.

## Non-goals for this plan

This plan does not:

- implement runtime MovementArtifact generation.
- implement movement, path planning, route solving, A*, interpolation, or travel
  time calculation.
- add a CLI `export-movement-artifact` command.
- add MovementArtifact golden files.
- modify RunArtifact v1 or ComparisonArtifact v1.
- move path or movement semantics into RunArtifact v1.
- add customer report movement sections.
- connect Unity animation to MovementArtifact or RunArtifact.
- modify Track C ingestion or WMS connector behavior.
- modify contracts, generated contracts, schemas, scripts, datasets, CI, or
  production code.

## Existing boundaries

### RunArtifact v1

RunArtifact v1 remains the deterministic simulation handoff for KPI, resource,
inventory, layout, and current position timeline data.

Its `position_timeline` is a baseline layout/resource handoff. It is not a
movement trace, route, path, or evidence that workers, forklifts, totes, or
inventory physically traveled between nodes.

The future movement generator must not overload RunArtifact v1 with path
semantics. If a consumer needs real movement, it must consume a separate
MovementArtifact produced by a future explicitly authorized runtime generator.

### MovementArtifact v1

MovementArtifact v1 is the intended separate contract for real movement data.

The schema and generated contracts exist, but no runtime component currently
produces a MovementArtifact. Current fixtures are test-only contract inputs, not
golden artifacts and not sample runtime outputs.

Future runtime generation should produce a standalone MovementArtifact linked to
its source RunArtifact or simulation run identity. It should not mutate existing
RunArtifact or ComparisonArtifact files.

### Sim.Report loader boundary

Sim.Report currently has a minimal MovementArtifact loader boundary. It can load
MovementArtifact JSON and validate the artifact identity:

- `schema_version == "movement-artifact.v1"`
- `artifact_kind == "warehouse-movement"`

It does not perform full graph, actor, event, segment, or route validation. It
does not render movement sections in customer reports.

### Runtime orchestration service boundary

The runtime orchestration service scaffold is separate from Sim.Core movement
generation. It should orchestrate commands/artifacts only after the generator
contract is clear.

The first runtime implementation slice should avoid coupling orchestration to
unstable movement internals. It can later call an explicit movement export
surface, but it should not infer movement from RunArtifact v1
`position_timeline`.

### Unity run artifact player boundary

The Unity run artifact player can consume RunArtifact for static layout/resource
handoff and UI state.

Unity must not animate movement from RunArtifact v1 position_timeline. Future
movement animation must depend on a real MovementArtifact generated after R2
runtime movement is implemented and validated.

### Track C ingestion boundary

Track C ingestion provides source data normalization and mock WMS adapter work.
It does not create MovementArtifact runtime output.

The future movement generator may eventually consume normalized warehouse graph,
resource, order, and task inputs, but CORE-R2a does not modify ingestion fields,
schemas, fixtures, workflows, or smoke tests.

## Recommended next implementation slice

The next implementation task should still be small and explicit:

1. Add an internal movement generator skeleton behind a clearly named API.
2. Accept a deterministic input that is already available from Sim.Core unified
   results and/or RunArtifact-adjacent data.
3. Emit an in-memory generated MovementArtifact contract object only.
4. Keep CLI export, golden files, report rendering, Unity animation, and runtime
   orchestration out of that first slice unless separately authorized.
5. Add tests for identity, deterministic ordering, basic actor/event/segment
   shape, and non-overlap with RunArtifact v1 semantics.

The implementation should remain additive and reversible.

## Proposed minimal generator shape

Future production code could introduce a small internal generator such as:

```text
MovementArtifactGenerator.Generate(input) -> WarehouseTwin.Contracts.MovementArtifact
```

The minimal input should be explicit and reviewed before coding. It may include:

- scenario id, run id, seed, and source artifact identity.
- warehouse graph nodes and edges.
- actors/resources eligible for movement.
- operation intervals or movement candidates.
- deterministic ordering policy.
- provenance fields describing generator version and assumptions.

The output should be a generated `MovementArtifact` contract type. It should not
write files directly in the core generator.

## Proposed CLI surface

A future CLI task may add an explicit command such as:

```bash
dotnet run --project src/Sim.Cli -- export-movement-artifact <scenario.json> -o <movement.json>
```

This command should be introduced only after the generator has tests and a
reviewed golden policy. It should be opt-in at first and should not change
existing `export-artifact`, `compare-files`, or `render-report` behavior.

## Proposed tests

Future tests should cover:

- deterministic MovementArtifact generation for a minimal scenario.
- `schema_version` and `artifact_kind`.
- stable actor, movement event, and route segment ordering.
- nonnegative distances and times.
- monotonic segment timing.
- route segment references to known graph nodes and edges.
- MovementArtifact generation does not mutate RunArtifact v1 output.
- Unity/report consumers do not treat RunArtifact v1 `position_timeline` as real
  movement.

Tests should start in memory before introducing tracked golden files.

## Golden policy

No MovementArtifact golden exists today.

A future golden PR must follow the golden update policy and include:

- exact regeneration command.
- diff summary.
- customer impact statement.
- rollback plan.
- proof that RunArtifact v1 and ComparisonArtifact v1 golden files changed only
  if explicitly authorized.

MovementArtifact fixtures under `tests/contract/fixtures` remain test-only and
must not be described as runtime golden artifacts.

## Report and Unity policy

Reports may mention MovementArtifact availability only after a real runtime
artifact exists. Before then, customer reports must not claim real travel paths,
movement traces, or route animation.

Unity may render static RunArtifact layout/resource state, but Unity must not
animate movement from RunArtifact v1 position_timeline. Any future animated
movement must be driven by MovementArtifact or a later explicitly approved
movement contract.

## Rollback plan

Because CORE-R2a is planning/docs only, rollback is limited to reverting this
document and the associated status notes.

For future runtime changes, rollback should be designed around:

- disabling the movement export command.
- keeping existing RunArtifact / ComparisonArtifact behavior unchanged.
- preserving existing report and Unity behavior.
- removing generated MovementArtifact outputs without changing source scenario
  or existing golden artifacts.

## Go / no-go checklist

Before implementing runtime MovementArtifact generation, confirm:

- [ ] MovementArtifact generator input shape is reviewed.
- [ ] Generator output remains a separate MovementArtifact.
- [ ] RunArtifact v1 `position_timeline` remains baseline layout positions, NOT
      simulated movement.
- [ ] No path/movement data is added to RunArtifact v1.
- [ ] CLI surface is opt-in and separately authorized.
- [ ] Golden update policy is accepted for any tracked MovementArtifact output.
- [ ] Report and Unity movement claims remain blocked until real movement output
      exists.
- [ ] Track C ingestion changes are not mixed into the runtime generator PR.
- [ ] Rollback preserves existing customer-facing artifact behavior.
