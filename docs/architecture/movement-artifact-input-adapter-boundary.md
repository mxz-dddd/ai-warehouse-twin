# MovementArtifact Input Adapter Boundary

## Status

CORE-R2d is planning/docs only.

It does not implement MovementArtifact input adapter code.
It does not modify Sim.Core, Sim.Cli, Sim.Report, Unity, ingestion, runtime service, contracts, scripts, datasets, or golden artifacts.

This document defines the boundary for a future adapter that converts explicit warehouse inputs into `MovementArtifactGenerationRequest`. It is a product honesty guardrail: real warehouse customers must not see fabricated movement claims before the system has an approved movement input policy, tests, and handoff strategy.

RunArtifact v1 position_timeline remains baseline layout positions, NOT simulated movement.

The input adapter must not treat RunArtifact v1 position_timeline as a route, path, trajectory, travel trace, speed profile, or animation source.

## Current foundation

The current main branch has these pieces in place:

- MovementArtifact v1 schema and generated contract surfaces.
- Test-only MovementArtifact valid and invalid contract fixtures.
- Sim.Report MovementArtifact loader boundary and smoke coverage.
- Minimal in-memory deterministic `MovementArtifactGenerator` in Sim.Core.
- Generator-to-JSON-to-loader compatibility tests.
- Unified runner, RunArtifact, ComparisonArtifact, customer report, and CI/smoke guardrails.
- RunArtifact v1 `layout.resources` and `position_timeline` for deterministic baseline layout handoff.

The current generator accepts explicit `MovementArtifactGenerationRequest` inputs:

- `scenario_id`, `run_id`, `seed`, and `source_run_artifact`.
- `MovementGraphNodeInput`.
- `MovementGraphEdgeInput`.
- `MovementActorInput`.
- `MovementLegInput`.
- `graph_source` and `movement_generator_version` provenance.

What is not in place yet:

- production adapter from scenario/unified runner data to generator input.
- real path graph construction.
- route solving, travel-time policy, congestion, collision, or resource travel model.
- CLI export command.
- MovementArtifact golden artifact.
- customer report movement section.
- Unity movement animation.
- runtime orchestration integration.
- Track C ingestion integration.

## Non-goals

CORE-R2d does not:

- implement adapter production code.
- modify `MovementArtifactGenerator`.
- infer movement from RunArtifact v1.
- add fields to RunArtifact v1 or ComparisonArtifact v1.
- add `export-movement-artifact` or any other CLI command.
- write MovementArtifact files.
- add MovementArtifact golden artifacts.
- add report movement rendering.
- add Unity animation.
- change Track C ingestion fields or mock WMS behavior.
- change runtime orchestration service behavior.
- change contracts, schema, generated contracts, scripts, datasets, CI, or source code.

## Candidate input sources

### ScenarioDefinition

Allowed future source for:

- `scenario_id`.
- `seed`.
- operation/order/receipt/task identity where the scenario model exposes it.
- static resources and resource ids.
- static layout/resource coordinates if present.
- SKU/order/quantity context needed to attach operation provenance.

Current caveat: the existing scenario surface is domain-specific and not yet a full physical graph. If a future adapter uses scenario data, it must document exactly which fields are mapped and which are intentionally omitted.

### Unified runner output

Allowed future source for:

- deterministic operation order.
- operation timing when explicitly exposed before RunArtifact rendering.
- resource ids used by operation intervals or telemetry.
- operation ids/types for movement segment provenance.

The unified runner output is a better timing source than rendered RunArtifact `position_timeline` because it is internal simulation output, not a visualization handoff. Still, any future adapter must use explicit fields such as operation intervals/telemetry, not parse event-log strings or rendered artifact coordinates.

### RunArtifact v1 layout resources

Allowed only as a static coordinate/resource identity source.

`layout.resources` can help map resource ids to deterministic baseline coordinates for graph node seeding or static actor initialization. It must not be treated as proof of real travel. If used, the adapter provenance must say that graph coordinates came from deterministic layout handoff data.

### RunArtifact v1 position_timeline

Forbidden as a route/path/movement source.

RunArtifact v1 position_timeline remains baseline layout positions, NOT simulated movement.

It may only be used as evidence of existing baseline handoff semantics, not as generated movement input. It must not be parsed to produce graph edges, route segments, movement events, speed profiles, congestion, collision, route choice, or animation paths.

### Track C ingestion outputs

Future possible source of normalized WMS/resource/order data.

Track C may eventually provide cleaner customer data inputs such as resources, zones, tasks, orders, locations, and WMS-derived identifiers. This task does not modify ingestion schemas, services, workflows, smoke checks, or mock WMS adapters.

### Runtime orchestration service

Future caller/coordinator only.

The runtime service can eventually invoke a reviewed movement export surface, manage run manifests, and collect output artifacts. It is not the source of movement semantics and must not manufacture paths from RunArtifact v1 position timeline data.

### Unity player state

Consumer only.

Unity may consume static RunArtifact layout state today and may later consume MovementArtifact output after real movement semantics are implemented and validated. Unity is not a source of movement semantics.

## Allowed input mapping

A future adapter may build:

- `MovementGraphNodeInput` from explicit static layout/resource/location data.
- `MovementGraphEdgeInput` from an explicit deterministic graph policy, reviewed fixture data, or future customer-provided graph data.
- `MovementActorInput` from explicit resource/worker/equipment identity and initial node policy.
- `MovementLegInput` from explicit operation timing plus explicit route policy.

Graph edges and movement legs must come from explicit deterministic adapter policy, not from pretending position_timeline is a physical route.

Allowed first-slice mappings should be intentionally small:

- scenario/run identity from scenario input.
- seed from scenario input.
- resource ids from scenario/unified runner output.
- initial node ids from explicit static layout policy.
- operation id/type from unified operation telemetry or intervals if exposed.
- segment ordering from explicit operation timing and stable id ordering.

## Explicitly forbidden mapping

Do not map position_timeline entries directly to movement_events.

Do not map position_timeline order directly to route_segments.

Do not infer travel distance, speed, congestion, collision, or route choice from position_timeline.

Do not add movement fields to RunArtifact v1.

Do not make customer-facing movement claims from adapter output until golden/report/Unity policy is separately approved.

Additional forbidden shortcuts:

- Do not parse RunArtifact event logs to create route geometry.
- Do not treat repeated resource coordinates as evidence that an actor stayed, traveled, or waited.
- Do not generate “worker moving”, “forklift moving”, or “goods moving” claims from current baseline layout handoff data.
- Do not backfill MovementArtifact from Unity playback state.
- Do not couple runtime orchestration to unstable adapter internals.
- Do not mix Track C ingestion changes into the first adapter production PR.

## Proposed adapter location

Future adapter should live in Sim.Core Movement namespace, near MovementArtifactGenerator.

Possible future API shape:

```csharp
MovementArtifactGenerationRequest MovementArtifactInputAdapter.FromScenario(...)
```

or, if timing must come from the unified runner:

```csharp
MovementArtifactGenerationRequest MovementArtifactInputAdapter.FromUnifiedRun(...)
```

This PR does not implement this adapter.

Keeping the adapter near `MovementArtifactGenerator` makes the boundary explicit: the adapter creates generator input; the generator converts explicit input to the contract object; CLI/report/runtime/Unity remain separate consumers or callers.

CORE-R2e adds the first deterministic fixture-scale MovementArtifact input adapter. The adapter creates MovementArtifactGenerationRequest objects from explicit scenario/layout/resource inputs and a documented fixture-scale graph policy. It does not read RunArtifact v1 position_timeline, implement real route optimization, add CLI export, write files, update golden artifacts, render report movement sections, animate Unity movement, modify Track C ingestion, or modify runtime orchestration.

## Proposed first adapter slice

Recommended next task:

```text
CORE-R2e add deterministic fixture-scale MovementArtifact input adapter
```

Suggested scope:

- in-memory only.
- accepts `ScenarioDefinition` or a narrowly scoped existing scenario model.
- maps static layout/resources to graph nodes/actors.
- creates deterministic single-edge fixture-scale movement legs only when explicit policy is documented.
- uses stable ordering by ids and timestamps.
- emits `MovementArtifactGenerationRequest`, not files.
- no CLI.
- no file writing.
- no golden.
- no report movement section.
- no Unity animation.
- no ingestion changes.
- no runtime service changes.

The first adapter slice should prove that an explicit policy can produce valid generator input without implying that current RunArtifact v1 position timeline is movement.

## Proposed tests

Future adapter tests should cover:

- converts a minimal scenario/layout fixture into `MovementArtifactGenerationRequest`.
- deterministic output across repeated adapter calls.
- stable node, edge, actor, and movement leg ordering.
- resource ids map to known actor/resource ids.
- operation ids attach to movement legs only from explicit operation/timing input.
- generated request can pass `MovementArtifactGenerator.Generate(...)`.
- generated artifact can serialize to JSON and load through Sim.Report loader.
- adapter does not read or map RunArtifact v1 `position_timeline`.
- unsupported or ambiguous fields are rejected or documented, not silently inferred.

Do not start with tracked MovementArtifact golden files. In-memory fixtures are enough for the first adapter production PR.

## CLI / golden / report / Unity policy

No CLI export command should be added until the adapter has in-memory tests, generator compatibility tests, and an approved regeneration/golden policy.

No MovementArtifact golden artifact should be added in the adapter PR unless a separate GOLDEN task explicitly approves the exact customer impact, regeneration command, diff summary, and rollback plan.

Customer reports must not show movement sections until real MovementArtifact output exists and report wording is separately approved.

Unity must not animate from RunArtifact v1 `position_timeline`. Future Unity animation must depend on a real MovementArtifact or later explicitly approved movement contract.

## Rollback plan

Because CORE-R2d is planning/docs only, rollback is limited to reverting this document and the related status notes.

For a future adapter production PR, rollback should preserve:

- existing RunArtifact v1 behavior.
- existing ComparisonArtifact behavior.
- existing report output.
- existing Unity static layout behavior.
- existing ingestion and runtime service behavior.

The adapter should remain additive and in-memory until a separate export/golden policy is approved.

## Go / no-go checklist

Before implementing adapter production code, confirm:

- [ ] adapter input source is explicit and reviewed.
- [ ] `MovementArtifactGenerationRequest` mapping is documented.
- [ ] graph edges come from explicit deterministic policy, not `position_timeline`.
- [ ] movement legs come from explicit operation/timing plus route policy, not `position_timeline`.
- [ ] RunArtifact v1 position_timeline remains baseline layout positions, NOT simulated movement.
- [ ] no RunArtifact v1 schema changes are required.
- [ ] no ComparisonArtifact schema changes are required.
- [ ] no CLI export command is included in the first adapter PR.
- [ ] no file writing is included in the first adapter PR.
- [ ] no MovementArtifact golden artifact is included in the first adapter PR.
- [ ] no report movement section is included in the first adapter PR.
- [ ] no Unity animation is included in the first adapter PR.
- [ ] no Track C ingestion changes are included.
- [ ] no runtime orchestration service changes are included.
