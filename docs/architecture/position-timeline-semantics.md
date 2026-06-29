# Position Timeline Semantics

## Status

Audit only. No RunArtifact schema change, no golden update, and no movement implementation in this PR.

This document freezes the current meaning of RunArtifact v1 `position_timeline`
after `GOLDEN-U3d-default-unified-runner` switched customer-facing defaults to
the unified runner. It is an honesty boundary for product, report, validation,
and visualization work before R2 real path movement exists.

## Scope

This audit covers:

- RunArtifact v1 `position_timeline`.
- RunArtifact v1 `layout.resources`.
- Unified runner operation intervals.
- Customer report wording.
- Future Unity visualization handoff.

It does not cover ingestion, WMS connectors, physical equipment control,
optimization recommendations, calibration confidence, contract changes, or
artifact schema changes.

## Current default runner state

After GOLDEN-U3d, default `sample-small-warehouse`, `run-file`,
`export-artifact`, and `compare-files` use unified runner semantics.

`render-report` remains an artifact consumer and does not run simulation.
It reads RunArtifact / ComparisonArtifact files and can display
operator-provided runner provenance flags, but it does not choose a runner.

## What position_timeline currently means

`position_timeline` entries represent deterministic operation start/finish
handoff points mapped to baseline layout resource positions.

The current RunArtifact v1 fields have these meanings:

- `operation_id`: deterministic operation identifier from the unified scenario
  adapter / unified operation runner.
- `operation_type`: coarse operation type such as `inbound`, `outbound`, or
  `each_pick`.
- `stage_type`: current unified default is `operation`.
- `resource_id`: resource used by the operation interval.
- `at_ms`: operation/resource interval start or finish time.
- `event_type`: `start` or `finish`; this is not a movement event.
- `node_id`: baseline layout node for the resource.
- `x`: baseline layout X coordinate for the resource.
- `y`: baseline layout Y coordinate for the resource.

`layout.resources` lists the baseline resources used by the unified timeline.
It is not a complete warehouse map and should not be treated as one.

## What position_timeline does not mean

Current `position_timeline` is baseline layout positions, NOT simulated movement.

It does not represent simulated travel paths.
It does not represent forklift/person/goods trajectories.
It does not encode speed, distance, route choice, congestion, or collision avoidance.
It must not be used to animate real movement before R2.

The timeline shows when an operation starts or finishes at a baseline resource
position. It does not say how a worker, forklift, tote, pallet, or SKU traveled
between positions.

## Current unified mapping

The current unified RunArtifact maps unified operation intervals into the
existing RunArtifact v1 `position_timeline` contract without adding new fields.

For the `sample-small-warehouse` unified golden:

- `finished_at_ms = 410`
- `position_timeline.count = 6`
- `stage_type = operation`
- `layout.resources = dock-1, station-1`

For the each-pick unified default smoke:

- event fragments are `resource.requested`, `resource.acquired`,
  `inventory.removed`, and `resource.released`;
- these events describe resource/inventory operation semantics;
- the position timeline still cannot be interpreted as real picker movement.

The current adapter maps each inbound receipt, outbound order, and each-pick
order to one coarse unified operation. That is enough for shared resource /
inventory baselines, but it is not a route model.

## Customer-facing wording rules

Allowed wording:

- Operation handoff timeline.
- Resource occupancy timeline.
- Baseline layout positions.
- Resource start/finish points.
- Deterministic artifact handoff coordinates.

Forbidden wording before R2:

- forklift movement animation.
- worker route.
- goods trajectory.
- real-time travel path.
- path optimization result.
- collision-aware movement.
- distance-based travel simulation.

Customer-facing material must not imply that AI Warehouse Twin has already
simulated real people, forklifts, totes, pallets, or goods moving through
warehouse paths.

## Visualization rules before R2

Before R2, Unity or any visual consumer may render resources as static baseline
positions or operation state markers only.

Before R2, consumers must not animate movement along paths using
`position_timeline`.

Allowed before R2:

- Static resource markers from `layout.resources`.
- Start/finish state changes at a resource marker.
- Timeline scrubber states that highlight resource occupancy.
- Labels that say `baseline layout positions, NOT simulated movement`.

Forbidden before R2:

- Interpolating between `position_timeline` points as if they were travel
  paths.
- Drawing forklift, worker, tote, pallet, or goods routes from current
  `position_timeline`.
- Claiming travel time, travel distance, path choice, congestion, or collision
  avoidance from current artifact fields.

## Report wording rules

Customer reports may say layout coordinates are deterministic baseline
positions.

Customer reports must not call current `position_timeline` a warehouse map,
movement map, route trace, travel trace, or real movement trace.

Reports may describe the timeline as operation/resource handoff data, but they
must not claim that real movement has been simulated.

If a report discusses visualization or movement, it must say that real path
movement is planned for R2 and is not implemented in RunArtifact v1 today.

## Risks if misinterpreted

- Customers may believe the system already simulates forklift/person/goods
  movement paths.
- KPI timing may be misunderstood as including calibrated walking or driving
  distance.
- Unity animation could over-promise movement semantics that the artifact does
  not contain.
- Real warehouse pilots may ask for path calibration evidence that does not yet
  exist.
- Mislabeling current positions as movement conflicts with R2 real movement,
  R5 calibration confidence, and R7 optimization recommendation work.

## R2 requirements for real movement

R2 real movement requires a separate movement model and explicit product /
contract decisions. At minimum, it must define:

- warehouse graph / nodes / edges;
- resource start locations;
- operation pickup/dropoff locations;
- travel-time model;
- route selection;
- movement event model;
- position interpolation or path segments;
- tests for movement semantics;
- artifact/schema decision if new fields are needed.

If new schema fields are needed, the change must go through a dedicated
`CONTRACT-` PR before customer-facing artifacts depend on those fields.

R2 should also define how movement affects resource occupancy, operation
duration, KPI interpretation, customer reports, and visualization handoff.

## Acceptance checklist before enabling movement visualization

Before any customer-facing movement visualization is enabled:

- movement model implemented.
- movement tests passed.
- artifact contract reviewed.
- report wording reviewed.
- Unity consumer reviewed.
- customer-facing examples updated.
- old baseline-position wording not reused for real movement.
- Reports distinguish movement-driven data from baseline layout positions.
- Visualization consumers render path data only from movement-approved fields.
- Golden artifacts are regenerated only through a dedicated authorized task if
  artifact outputs change.
- Product wording no longer relies on current RunArtifact v1 baseline positions
  as proof of movement.
- Validation covers at least one route with known nodes, edges, travel times,
  and expected position progression.

## Guard tests

CORE-U4b adds tests that load the tracked RunArtifact golden and customer
report golden to ensure the current v1 `position_timeline` remains documented
as operation/resource handoff at baseline layout positions, NOT simulated
movement. These tests are not movement tests. They are safety guards until R2
defines real movement semantics.

## R2 movement contract planning

CORE-U4c records contract options for future R2 movement semantics in
`docs/architecture/r2-movement-contract-options.md`. Until a later `CONTRACT-`
task approves movement fields, current RunArtifact v1 `position_timeline`
remains baseline layout positions, NOT simulated movement.

## Current decision

Do not implement or claim real movement in CORE-U4a.

Keep current RunArtifact v1 `position_timeline` as operation/resource handoff
data only.

Describe it as baseline layout positions, NOT simulated movement.

Do not build customer-facing movement animation or movement claims on it before
R2 real path movement lands.
