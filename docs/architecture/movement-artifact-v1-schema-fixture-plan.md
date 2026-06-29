# MovementArtifact v1 Schema and Fixture Plan

## Status

Planning only. No schema file, generated contract, golden artifact, or implementation change in this PR.

This document prepares a future CONTRACT- PR but does not approve MovementArtifact v1.

CONTRACT-R2b converts the CONTRACT-R2a proposal into a practical checklist for a later schema implementation PR. It does not add `movement-artifact.v1.schema.json`, update `packages/contracts`, regenerate code, implement movement/path/route generation, or update tracked artifact golden files.

## Scope

This plan covers:

- future MovementArtifact v1 schema.
- future validation fixtures.
- future generated contract targets.
- future drift checks.
- future CI integration.
- future consumer review.

The goal is to make the eventual CONTRACT- implementation reviewable before any schema, generated contract, or customer-facing artifact changes occur.

## Non-goals

This plan does not:

- add movement-artifact schema.
- modify `packages/contracts`.
- generate code.
- add MovementArtifact runtime generation.
- add movement golden.
- change RunArtifact v1.
- change Track C ingestion.
- implement movement/path/route.
- modify Unity, Report, Validation, Sim.Core, or Sim.Cli behavior.

## Prerequisites before schema work

Before a schema PR starts, the team should confirm:

- CONTRACT-R2a proposal merged.
- product owner accepts MovementArtifact as separate artifact direction.
- contract owner reviews schema outline.
- Unity/report consumers agree handoff expectations.
- RunArtifact v1 remains stable.
- `position_timeline` safety guards remain green.
- default unified runner behavior remains understood and documented.
- golden update policy is still followed for any new movement artifacts.

## Proposed schema file locations

Candidate paths for a later CONTRACT- PR:

- `packages/contracts/schemas/movement-artifact.v1.schema.json`
- `src/Sim.Contracts/Artifacts/MovementArtifact.cs`

Exact paths must be confirmed in the later CONTRACT- PR.

If the repository's contract layout changes before implementation, the future PR should follow the then-current contract generation pattern rather than forcing these candidate paths.

## Proposed generated contract targets

The later CONTRACT- PR should consider:

- C# contracts.
- TypeScript / JSON schema consumers if present.
- Unity consumer contract if present.
- report/validation loaders.
- any generated documentation or schema index used by consumers.

Do not generate code in CONTRACT-R2b.

## Required top-level schema fields

Future schema review should explicitly decide:

- [ ] `schema_version`
- [ ] `artifact_kind`
- [ ] `scenario_id`
- [ ] `run_id` or explicit no-run-id decision
- [ ] `seed`
- [ ] `source_run_artifact`
- [ ] `warehouse_graph`
- [ ] `actors`
- [ ] `movement_events`
- [ ] `route_segments`
- [ ] `provenance`
- [ ] deterministic generated-at/provenance policy
- [ ] stable ordering policy for all arrays

## Required warehouse graph schema fields

Future warehouse graph schema review should explicitly decide:

- [ ] `nodes`
- [ ] `edges`
- [ ] `node_id`
- [ ] `node_type`
- [ ] `x`
- [ ] `y`
- [ ] `edge_id`
- [ ] `from_node_id`
- [ ] `to_node_id`
- [ ] `distance_m`
- [ ] `travel_time_ms`
- [ ] `bidirectional`
- [ ] coordinate unit field or documented coordinate unit policy
- [ ] graph source/provenance field if graph does not come from scenario input

## Required actor schema fields

Future actor schema review should explicitly decide:

- [ ] `actor_id`
- [ ] `actor_type`
- [ ] `resource_id`
- [ ] `initial_node_id`
- [ ] `capacity`
- [ ] `load_state`
- [ ] whether actor/resource identity can be one-to-one, one-to-many, or optional
- [ ] allowed actor types, such as `worker`, `forklift`, `amr`, `conveyor`, `dock`, and `station`

Actors must not be assumed to be identical to current RunArtifact layout resources unless the CONTRACT- PR states the mapping.

## Required movement event schema fields

Future movement event schema review should explicitly decide:

- [ ] `event_id`
- [ ] `actor_id`
- [ ] `operation_id`
- [ ] `event_type`
- [ ] `at_ms`
- [ ] `node_id`
- [ ] `x`
- [ ] `y`
- [ ] `load_state`
- [ ] `related_resource_id`
- [ ] allowed event types
- [ ] deterministic tie-break ordering for same-timestamp events
- [ ] whether idle/repositioning events are allowed in v1

Movement events must not be confused with current RunArtifact `event_log` entries.

## Required route segment schema fields

Future route segment schema review should explicitly decide:

- [ ] `segment_id`
- [ ] `actor_id`
- [ ] `operation_id`
- [ ] `from_node_id`
- [ ] `to_node_id`
- [ ] `start_ms`
- [ ] `end_ms`
- [ ] `distance_m`
- [ ] `path_node_ids`
- [ ] `edge_ids`
- [ ] `travel_time_ms`
- [ ] whether `path_node_ids` includes endpoints
- [ ] whether zero-distance waits are segments or events only
- [ ] whether segment distance affects KPIs in v1

Route segments are the likely Unity path animation input after CONTRACT- approval.

## Required provenance fields

Future provenance schema review should explicitly decide:

- [ ] source RunArtifact reference format.
- [ ] movement generator version.
- [ ] graph source.
- [ ] movement enabled/disabled flag.
- [ ] deterministic generation policy.
- [ ] whether wall-clock generation time is forbidden, omitted, or represented outside byte-stable golden content.
- [ ] runner mode / simulation provenance where required by reports.
- [ ] explicit statement that RunArtifact v1 `position_timeline` remains baseline layout positions, NOT simulated movement.

## Validation fixture plan

The later CONTRACT- PR should plan fixtures before adding schema or generated contracts.

### `valid-small-single-actor-route`

- Purpose: prove a minimal graph, one actor, one operation, one route, and deterministic event/segment ordering are valid.
- Expected result: schema validation passes; generated loader reads all fields; golden regeneration is byte-identical.
- Why it matters: establishes the smallest customer-safe movement handoff baseline.

### `valid-two-actor-resource-contention`

- Purpose: prove multiple actors can share a graph while preserving operation/resource timing semantics.
- Expected result: validation passes; actor timelines are independently monotonic; resource-related references are valid.
- Why it matters: prevents a path artifact from drifting away from unified runner resource contention semantics.

### `invalid-missing-node`

- Purpose: catch actors, events, or segments referencing a node not present in `warehouse_graph.nodes`.
- Expected result: validation fails with a clear missing-node error.
- Why it matters: Unity and reports cannot safely render or aggregate unknown positions.

### `invalid-edge-references-missing-node`

- Purpose: catch graph edges whose `from_node_id` or `to_node_id` does not exist.
- Expected result: validation fails with a clear edge endpoint error.
- Why it matters: path planning and route rendering require graph connectivity integrity.

### `invalid-event-references-missing-actor`

- Purpose: catch movement events for an unknown `actor_id`.
- Expected result: validation fails with a clear actor reference error.
- Why it matters: prevents orphan movement claims in reports or animation.

### `invalid-segment-non-monotonic-time`

- Purpose: catch segments with `end_ms < start_ms` or per-actor segments that move backward in time.
- Expected result: validation fails with a monotonic-time error.
- Why it matters: movement must align with simulation time and deterministic replay.

### `invalid-duplicate-event-id`

- Purpose: catch duplicate event identifiers.
- Expected result: validation fails with a duplicate-id error.
- Why it matters: stable ids are required for report references, debugging, and deterministic diffs.

### `invalid-negative-distance`

- Purpose: catch negative `distance_m`.
- Expected result: validation fails with a numeric range error.
- Why it matters: distance may later feed travel metrics, reports, or calibration.

### `invalid-wall-clock-generated-at-if-forbidden`

- Purpose: catch wall-clock timestamp fields if the schema forbids them in byte-stable content.
- Expected result: validation fails or golden check fails according to the chosen generated-at policy.
- Why it matters: artifacts must remain deterministic and reproducible.

## Golden fixture plan

The first golden should be tiny and deterministic.

Golden must be generated by CLI only after movement generator exists.

Golden update requires dedicated GOLDEN/CONTRACT PR.

RunArtifact v1 golden must not be changed just to add MovementArtifact.

The future golden plan should include:

- one valid minimal MovementArtifact golden.
- one deterministic regeneration command.
- byte comparison between regenerated output and tracked golden.
- documented customer impact.
- rollback instructions that disable MovementArtifact consumers while leaving RunArtifact v1 intact.

## Drift-check plan

CONTRACT PR must update drift checks so generated contracts match schema.

CI must fail if generated contracts are stale.

MovementArtifact schema drift must not be hidden inside unrelated PRs.

The future drift check should verify:

- schema file changes are accompanied by generated contract changes.
- generated C# contract changes are deterministic.
- loader/report/validation consumers compile against generated types.
- no RunArtifact v1 schema changes are smuggled into MovementArtifact work.

## CI integration plan

The future CONTRACT- PR should add:

- schema validation.
- generated contract drift check.
- MovementArtifact loader tests.
- fixture validation tests.
- byte-stable golden regeneration smoke.
- no movement output when movement disabled.
- bad-case fixture validation.
- consumer wording checks if reports expose movement sections.

The CI path should not require Track C ingestion changes unless graph/input contracts are explicitly changed by the future task.

## Consumer review checklist

The future CONTRACT- PR should request review from:

- Unity.
- Report.
- Validation.
- Sim.Core.
- Sim.Cli.
- Product/customer wording.
- Track C ingestion only if graph input source is affected.

Track C should not be modified unless graph/input contract is explicitly changed in a future CONTRACT task.

Reviewers should confirm that no consumer describes current RunArtifact v1 `position_timeline` as path, route, trajectory, or simulated movement.

## CONTRACT PR checklist

- [ ] CONTRACT- title
- [ ] product reason
- [ ] schema file added
- [ ] generated contracts updated
- [ ] validation fixtures added
- [ ] drift check updated
- [ ] golden strategy documented
- [ ] consumer migration note added
- [ ] rollback plan added
- [ ] no unrelated Track C / ingestion changes
- [ ] no unrelated RunArtifact v1 schema changes
- [ ] no Unity movement claims before approved movement data exists

## Risks and mitigations

### Schema too broad

Risk: MovementArtifact v1 tries to cover every future movement need and becomes hard to validate.

Mitigation: start with the smallest customer-safe movement handoff, add explicit open questions, and defer advanced route/congestion semantics.

### Over-promising movement

Risk: docs or reports imply full real-world movement fidelity before calibration and pilot evidence exist.

Mitigation: require provenance wording and keep current RunArtifact v1 as baseline layout positions, NOT simulated movement.

### Unity starts animation before data is approved

Risk: Unity animates current baseline positions or unapproved movement fields.

Mitigation: require MovementArtifact or other movement-approved fields before customer-facing animation.

### Movement fields diverge from KPI semantics

Risk: route events show actors moving while operation/resource occupancy says they are busy elsewhere.

Mitigation: validate alignment between movement events, route segments, operations, and resource occupancy.

### Calibration expectations appear before R5

Risk: customers assume distances/travel times are calibrated to their real warehouse before R5 evidence exists.

Mitigation: report movement provenance and confidence level separately from uncalibrated deterministic simulation.

### Track C graph/input scope creep

Risk: schema work pulls ingestion into the same PR.

Mitigation: only involve Track C if the graph/input contract is explicitly changed in a future CONTRACT task.

## Current decision

Do not add MovementArtifact schema in CONTRACT-R2b.

Do not modify contracts or generated code.

Use this plan to prepare a later CONTRACT implementation PR.

RunArtifact v1 `position_timeline` remains baseline layout positions, NOT simulated movement.
