# MovementArtifact v1 Proposal

## Status

Proposal only. No schema, contract, generated contract, golden, or implementation change in this PR.

This document is not contract approval. A later CONTRACT- PR is required before MovementArtifact v1 can become a stable handoff.

CONTRACT-R2a documents the proposed shape, semantics, validation needs, golden strategy, and consumer handoff expectations for a future movement-specific artifact. It does not add a schema file, generated contracts, runtime movement, path planning, route interpolation, Unity animation, or report behavior.

CONTRACT-R2b adds the schema/fixture planning checklist in
`docs/architecture/movement-artifact-v1-schema-fixture-plan.md`. It remains
planning-only and does not add schema files or generated contracts.

CONTRACT-R2c-preflight audits the existing contract generation and drift-check
flow before any MovementArtifact schema implementation.

CONTRACT-R2d begins implementing the proposal at the contract/schema layer only.
It does not implement movement generation or consumer behavior.

CONTRACT-R2e adds contract-level fixtures for the MovementArtifact schema.
Runtime generation, loaders, reports, Unity, and golden artifacts remain future
work.

REPORT-R2a adds a minimal MovementArtifact loader boundary in Sim.Report. It
only loads MovementArtifact JSON and checks schema identity; it does not
generate movement artifacts, validate full movement semantics, update reports,
add Unity animation, or create golden artifacts.

REPORT-R2b adds a smoke script for the minimal MovementArtifact loader boundary.
It only runs loader tests against contract fixtures; it does not generate
MovementArtifact runtime output, add report movement sections, create golden
artifacts, or animate movement.

CORE-R2a adds a runtime boundary plan for a future MovementArtifact generator.
It is planning-only and does not implement runtime generation, CLI export,
reports, Unity animation, golden artifacts, or Track C ingestion changes.

CORE-R2b introduces an in-memory deterministic generator boundary for
MovementArtifact v1. The generator is not yet customer-facing and does not
change RunArtifact v1, ComparisonArtifact v1, CLI behavior, reports, Unity, or
ingestion.

CORE-R2c verifies generator output compatibility with the existing
MovementArtifact loader. This remains an internal test boundary and does not
make MovementArtifact customer-facing.

CORE-R2d documents the MovementArtifact input adapter boundary. Future adapter
work must map only explicit scenario/layout/operation inputs into
MovementArtifactGenerationRequest and must not infer movement from RunArtifact
v1 position_timeline.

CORE-R2e adds a deterministic fixture-scale input adapter that produces
MovementArtifactGenerationRequest from explicit scenario/layout/resource inputs
only. It does not infer movement from RunArtifact v1 position_timeline.

CORE-R2f adds an internal end-to-end compatibility test from scenario adapter
to generator to loader. It remains test-only and does not change
MovementArtifact v1 schema or customer-facing artifacts.

CLI-R2a plans an opt-in MovementArtifact export command. It does not change MovementArtifact v1 schema, RunArtifact v1, ComparisonArtifact v1, default CLI behavior, reports, Unity, ingestion, or runtime orchestration.

CLI-R2b adds an opt-in MovementArtifact export command without changing MovementArtifact v1 schema, RunArtifact v1, ComparisonArtifact v1, default CLI behavior, reports, Unity, ingestion, or runtime orchestration.

## Scope

This proposal covers:

- MovementArtifact v1 proposal.
- Future R2 movement handoff.
- Future Unity path animation input.
- Future report movement provenance.
- Future validation / golden strategy.

The product reason is to preserve the current deterministic RunArtifact v1 handoff while creating a separate, reviewable surface for real movement semantics once R2 path and movement work begins.

## Non-goals

This proposal does not:

- change RunArtifact v1.
- replace current `position_timeline`.
- implement route planning.
- implement movement interpolation.
- change contracts.
- change Track C ingestion.
- control physical equipment.
- add a schema file.
- update generated contracts.
- update artifact golden files.
- implement movement/path/route in Sim.Core, Sim.Cli, Sim.Report, Unity, or validation.

## Relationship to RunArtifact v1

RunArtifact v1 remains the KPI, resource, inventory, and baseline handoff artifact.

Current RunArtifact v1 `position_timeline` remains baseline layout positions, NOT simulated movement. It is operation/resource handoff data at deterministic layout coordinates, not a path, route trace, movement map, trajectory, or proof that people, forklifts, or goods traveled between points.

MovementArtifact v1 would be a separate optional artifact. It must link to a RunArtifact by scenario/run identity so consumers can understand which deterministic simulation run the movement data explains.

Do not overload RunArtifact v1 `position_timeline` with path semantics. If movement semantics are approved later, they should enter through a dedicated CONTRACT- PR, schema review, generated contract update, validation plan, and golden strategy.

## Proposed artifact identity

Illustrative proposal only; this is not a schema:

```json
{
  "schema_version": "movement-artifact.v1",
  "artifact_kind": "warehouse-movement",
  "scenario_id": "sample-small-warehouse",
  "run_id": "TBD",
  "seed": 20240627,
  "source_run_artifact": "run-artifact.v1.json",
  "warehouse_graph": {},
  "actors": [],
  "movement_events": [],
  "route_segments": [],
  "provenance": {}
}
```

`run_id` is not yet a stable handoff field in the current artifacts; whether it is required before MovementArtifact v1 is an open question.

`source_run_artifact` could be a file name, artifact id, hash, or bundle reference. The final representation must be decided by a later CONTRACT- PR.

## Proposed top-level fields

The future artifact should evaluate these top-level fields:

- `schema_version`
- `artifact_kind`
- `scenario_id`
- `run_id`
- `seed`
- `generated_at_policy`
- `source_run_artifact`
- `warehouse_graph`
- `actors`
- `movement_events`
- `route_segments`
- `provenance`

`generated_at_policy` must not introduce wall-clock timestamp noise. If timestamps are needed for provenance, they must be deterministic, explicitly excluded from byte-stable golden comparison, or governed by a documented provenance policy.

## Proposed warehouse graph fields

The future `warehouse_graph` should evaluate:

- `nodes`
- `edges`
- `node_id`
- `node_type`
- `x`
- `y`
- `edge_id`
- `from_node_id`
- `to_node_id`
- `distance_m`
- `travel_time_ms`
- `bidirectional`

The graph could come from scenario input, layout data, WMS/CSV ingestion, or a future warehouse graph contract. If the graph enters the input contract, it must be handled by a separate CONTRACT- task and not mixed into this proposal.

## Proposed movement actor fields

The future `actors` collection should evaluate:

- `actor_id`
- `actor_type`
- `resource_id`
- `initial_node_id`
- `capacity`
- `load_state`

Potential `actor_type` values include:

- `worker`
- `forklift`
- `amr`
- `conveyor`
- `dock`
- `station`

An actor is not automatically the same thing as a current layout resource. The first implementation may only support a worker/forklift/resource subset, and the final subset should be decided by the R2 implementation and CONTRACT- review.

## Proposed movement event fields

The future `movement_events` collection should evaluate:

- `event_id`
- `actor_id`
- `operation_id`
- `event_type`
- `at_ms`
- `node_id`
- `x`
- `y`
- `load_state`
- `related_resource_id`

Potential `event_type` values include:

- `movement.started`
- `movement.arrived`
- `pickup.started`
- `pickup.completed`
- `dropoff.started`
- `dropoff.completed`
- `wait.started`
- `wait.ended`

These are proposal values only. They are not the current RunArtifact event log and must not be confused with current `event_log` entries.

## Proposed route segment fields

The future `route_segments` collection should evaluate:

- `segment_id`
- `actor_id`
- `operation_id`
- `from_node_id`
- `to_node_id`
- `start_ms`
- `end_ms`
- `distance_m`
- `path_node_ids`
- `edge_ids`
- `travel_time_ms`

Unity could use route segments for path animation after CONTRACT- approval. Reports could aggregate distance and travel time, but must label the result as MovementArtifact-derived and show movement artifact provenance.

## Timestamp semantics

`at_ms` is simulation time, not wall-clock time.

`start_ms` and `end_ms` are route segment intervals. Segment intervals should align with actor movement and operation/resource occupancy semantics.

Events with the same timestamp need deterministic ordering rules. Candidate ordering:

1. `at_ms` ascending.
2. `actor_id` ascending.
3. `operation_id` ascending.
4. `event_type` ascending.
5. `event_id` ascending.

Movement events must align with operation/resource occupancy semantics so reports and Unity do not show an actor moving while the simulation says the resource is unavailable for incompatible work.

## Coordinate semantics

`x` and `y` must come from an approved graph/layout coordinate system.

The unit must be explicit. The first version may use arbitrary layout units or meters, but the chosen unit must be written into the schema and consumer documentation.

Do not treat current RunArtifact v1 baseline positions as path coordinates. Current `position_timeline` remains baseline layout positions, NOT simulated movement.

## Determinism requirements

MovementArtifact generation must be deterministic:

- stable ordering.
- explicit seed.
- no wall-clock timestamps in byte-stable content.
- stable JSON serialization.
- repeat generation byte-identical when inputs are unchanged.
- deterministic route choice when multiple valid paths exist.
- deterministic ids for actors, events, and route segments.

## Linkage to RunArtifact

MovementArtifact must reference the RunArtifact / simulation run it explains.

RunArtifact v1 does not need to reference MovementArtifact in v1.

A future report bundle may include both artifacts.

ComparisonArtifact may remain layout-free unless a later product need requires movement comparison. If movement comparison becomes necessary, it may need either explicit MovementArtifact linkage or a separate MovementComparisonArtifact proposal.

## Unity handoff

Unity must not animate movement from RunArtifact v1 `position_timeline`.

Unity may animate movement only from MovementArtifact or other movement-approved fields after CONTRACT- approval.

Until that approval exists, Unity can use RunArtifact v1 for static baseline layout/resource rendering and artifact handoff checks, while labeling those positions as baseline layout positions, NOT simulated movement.

## Report handoff

Reports must distinguish baseline layout positions from movement paths.

Reports using MovementArtifact must show provenance, including the source run/artifact and whether movement was enabled.

Reports must not call RunArtifact v1 `position_timeline` a path, route, movement map, or trajectory.

Movement-derived report sections must remain opt-in until the product approves R2 movement semantics for customer-facing use.

## Validation requirements

Future validation should include:

- schema validation.
- graph connectivity validation.
- actor node existence.
- segment node existence.
- event actor existence.
- timestamps monotonic per actor.
- segment start/end alignment.
- deterministic serialization.
- known route fixture.
- no movement output if movement is disabled.
- route segment `from_node_id` / `to_node_id` existence.
- actor `initial_node_id` existence.
- event `operation_id` linkage where applicable.
- bad-case fixtures for invalid graph, missing node, and non-monotonic times.

## Golden artifact strategy

MovementArtifact golden would be new, not replacing RunArtifact golden.

Golden updates require a dedicated `GOLDEN-` and/or `CONTRACT-` PR with diff evidence, product impact, regeneration commands, and rollback notes.

At minimum, the first MovementArtifact golden strategy should include:

- one small deterministic movement fixture.
- bad-case fixtures for invalid graph.
- bad-case fixtures for missing node.
- bad-case fixtures for non-monotonic actor timestamps.
- byte-identical regeneration checks.

RunArtifact v1 golden files should remain stable unless a separate approved task explicitly changes them.

## Backward compatibility

RunArtifact v1 consumers remain valid.

Current reports can ignore MovementArtifact.

Unity can continue static baseline rendering from RunArtifact v1.

Movement consumers are opt-in until R2 product approval.

If MovementArtifact is absent, consumers must not infer movement from RunArtifact v1 `position_timeline`.

## Migration and rollback

If MovementArtifact fails, disable the movement consumer or revert the MovementArtifact PR.

RunArtifact v1 baseline remains intact.

Do not roll back Track C ingestion or the default unified runner for MovementArtifact failures.

Rollback should remove or disable only movement-specific generation, validation, report sections, Unity movement consumption, and associated golden checks.

## Contract governance checklist

- [ ] CONTRACT- PR opened
- [ ] product reason documented
- [ ] schema file proposed
- [ ] generated contracts updated
- [ ] drift check updated
- [ ] golden strategy documented
- [ ] Unity consumer reviewed
- [ ] report wording reviewed
- [ ] rollback plan documented
- [ ] no Track C / ingestion scope mixed in

## Open questions

- Is `run_id` required before MovementArtifact?
- Should graph live in scenario input, movement artifact, or separate graph artifact?
- Are units meters or layout units?
- Should distance affect KPI immediately or only visualization first?
- Should congestion be R2 or later?
- How to connect MovementArtifact to calibration R5?
- Is MovementArtifact compared in ComparisonArtifact or separate MovementComparisonArtifact?
- Should movement events be operation-derived only, or can they include idle/repositioning segments?
- What is the minimum customer-pilot evidence required before movement claims?
- Should `source_run_artifact` be a path, artifact id, content hash, or bundle reference?

## Current decision

Do not implement MovementArtifact in CONTRACT-R2a.

Do not change contracts in CONTRACT-R2a.

Use this proposal to prepare a later CONTRACT- implementation PR.

RunArtifact v1 `position_timeline` remains baseline layout positions, NOT simulated movement.
