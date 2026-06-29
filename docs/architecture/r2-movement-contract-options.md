# R2 Movement Contract Options

## Status

Planning only. No schema change, no contract change, no movement implementation, and no golden update in this PR.

This document prepares the product and contract decision for future R2 real
movement semantics. It does not approve a contract change and does not change
the current RunArtifact v1 meaning.

## Scope

This planning document covers:

- RunArtifact v1 `position_timeline` boundary.
- Future movement semantics.
- Future contract/schema options.
- Future Unity visualization handoff.
- Future report wording.
- Future validation tests.

This planning document does not cover:

- Track C ingestion.
- WMS connectors.
- Physical equipment control.
- Optimization recommendation implementation.
- Calibration confidence implementation.

## Current baseline before R2

After GOLDEN-U3d, default `sample-small-warehouse`, `run-file`,
`export-artifact`, and `compare-files` use unified runner semantics.

RunArtifact v1 `position_timeline` remains operation/resource handoff at
baseline layout positions, NOT simulated movement.

CORE-U4b guard tests enforce this boundary.

## Why R2 needs explicit movement semantics

Current `position_timeline` only records operation start/finish resource
handoff points. It does not encode how a worker, forklift, tote, pallet, or SKU
moved between locations.

Real movement needs explicit path concepts:

- warehouse graph;
- nodes and edges;
- resource start and current location;
- pickup and dropoff locations;
- route selection;
- travel time and distance;
- movement events;
- timestamp semantics.

If AI Warehouse Twin reuses RunArtifact v1 `position_timeline` directly as an
animation path, customers may believe the product has already simulated route
choice, travel time, distance, congestion, or collision avoidance. That would
overstate the current product capability.

For a real warehouse customer, movement semantics affect the credibility of
time KPIs, bottleneck explanations, staffing recommendations, and visualization
handoff. R2 must unify movement with resource occupancy, operation duration,
and KPI interpretation before any customer-facing route or movement claim is
made.

## Concepts R2 must define

- `warehouse graph`: the navigable topology of the warehouse.
- `node`: a graph point such as dock, aisle junction, station, staging area, or
  storage location.
- `edge`: a traversable connection between nodes.
- `resource current location`: where a worker, forklift, AMR, or other resource
  is at a given simulation time.
- `operation pickup location`: where the resource must pick up goods, a tote,
  a pallet, or task context.
- `operation dropoff location`: where the resource must deliver or complete the
  operation.
- `route`: selected ordered path through graph nodes / edges.
- `travel segment`: part of a route with start node, end node, timing, and
  distance semantics.
- `travel time`: simulated time required to traverse a segment or route.
- `travel distance`: path distance used for KPI and calibration interpretation.
- `movement event`: timestamped resource/location change event.
- `resource occupancy`: when a resource is busy with work, waiting, loading,
  unloading, or traveling.
- `load / unload state`: whether a resource is empty, carrying goods, carrying
  a tote, carrying a pallet, or otherwise loaded.
- `position interpolation`: how visualization derives in-between positions
  between movement events or path segments.
- `timestamp semantics`: whether timestamps refer to movement start, arrival,
  segment boundary, pickup, dropoff, or operation completion.
- `deterministic seed behavior`: stable tie-breakers and deterministic route /
  timing outcomes for regression and customer audit.

## Option A: keep RunArtifact v1 and add separate MovementArtifact v1

RunArtifact v1 remains stable. A new `MovementArtifact v1` or
`movement-artifact.v1.json` carries path, route, segment, trajectory, and
movement-provenance handoff data.

RunArtifact continues to cover KPI, resource, inventory, layout, and current
operation handoff semantics. MovementArtifact focuses on path / route /
trajectory handoff. Unity consumes MovementArtifact for movement animation.
Reports can reference MovementArtifact provenance when movement is present.

Advantages:

- Does not break RunArtifact v1.
- Avoids mixing baseline positions with movement paths.
- Makes rollback simpler because movement consumers can be disabled without
  reverting RunArtifact.
- Fits gradual R2 implementation and validation.
- Allows MovementArtifact to evolve under a dedicated contract surface.

Disadvantages:

- Adds another artifact.
- Report and Unity consumers must handle artifact pairing.
- Requires artifact linkage between RunArtifact and MovementArtifact.
- Requires new provenance and lifecycle rules.

## Option B: bump RunArtifact with movement fields

RunArtifact moves to v2 or another schema bump that includes movement fields
directly in the simulation output.

Advantages:

- One artifact contains the complete simulation output.
- Downstream consumers have one handoff payload.
- Movement data can be directly linked to operation, resource, inventory, and
  KPI data.

Disadvantages:

- Requires a `CONTRACT-` PR.
- Requires regenerated contracts.
- Requires tracked golden migration.
- Is more likely to break existing consumers.
- Increases the risk that baseline layout handoff and movement semantics are
  confused.
- Makes rollback heavier because core artifact consumers all see the schema
  change.

## Option C: keep movement internal and expose only report/visualization outputs

Movement is implemented inside simulation, report, or Unity pipelines, but no
stable movement artifact contract is exposed.

Advantages:

- Lower initial contract pressure.
- Faster early implementation.
- Useful for private prototypes or internal review.

Disadvantages:

- Poor customer auditability.
- Weak reproducibility and regression testing.
- Harder for Unity or third-party consumers to integrate.
- Movement provenance is unclear.
- Does not match the startup product requirement for trustworthy,
  artifact-backed handoff.

## Option comparison

| Option | Contract risk | Customer auditability | Unity handoff | Backward compatibility | Golden migration cost | Recommended? |
|---|---|---|---|---|---|---|
| Option A: separate MovementArtifact v1 | Medium, isolated to new contract | High | High; Unity consumes movement-specific artifact | High; RunArtifact v1 remains stable | Medium; new artifact golden needed | Yes, as planning direction |
| Option B: RunArtifact schema bump | High | High | Medium to high; one payload | Lower; existing consumers migrate | High; existing golden migration required | Not first choice |
| Option C: internal-only movement | Low initially | Low | Low to medium; consumer-specific | High for existing artifacts | Low initially | No for customer handoff |

## Recommended path

Recommended planning path: Option A: keep RunArtifact v1 stable and introduce a
separate MovementArtifact v1 behind a `CONTRACT-` governed task.

This is a planning recommendation, not an approved contract change.

Option A best preserves the current RunArtifact v1 handoff while giving R2 a
clean place to define route, segment, travel, and movement provenance semantics.
It also keeps the existing honesty boundary intact: current RunArtifact v1
`position_timeline` remains baseline layout positions, NOT simulated movement.

## Contract governance requirements

If R2 introduces MovementArtifact or bumps RunArtifact, it must use a dedicated
`CONTRACT-` task / PR.

That PR must include:

- product reason;
- schema proposal;
- contract owner review;
- generated contracts update;
- drift check;
- golden diff review;
- consumer migration note;
- rollback plan.

No movement field should become customer-facing until the contract and consumer
handoff are reviewed.

## Required simulation model changes

Future R2 work needs:

- graph model;
- route planner;
- travel-time model;
- resource initial location;
- operation pickup/dropoff mapping;
- movement event generation;
- deterministic ordering;
- tests for route and timing.

These are not implemented in CORE-U4c.

## Required validation and tests

R2 validation should include:

- known graph fixture;
- single resource movement;
- multi-resource contention;
- pickup/dropoff validation;
- deterministic movement artifact generation;
- schema validation;
- Unity consumer fixture;
- report wording guard;
- legacy v1 `position_timeline` guard remains.

Validation must prove that movement timestamps, path segments, and KPI
interpretation are deterministic and explainable.

## Required visualization handoff rules

Before MovementArtifact or approved movement fields exist, Unity must not
animate movement from RunArtifact v1 `position_timeline`.

After R2, Unity must consume only movement-approved fields for path animation.

RunArtifact v1 `position_timeline` can still support static baseline resource
markers or operation handoff indicators, but not route animation.

## Required report wording changes

Movement report wording must distinguish baseline positions from movement path.

Reports must label movement artifact provenance when movement output is used.

Reports must not call RunArtifact v1 `position_timeline` a path, route trace,
movement map, travel trace, or trajectory.

If MovementArtifact is introduced, reports should state whether a report uses
only RunArtifact baseline handoff data or also includes movement-approved
artifact data.

## Migration and rollback

RunArtifact v1 remains supported.

If MovementArtifact is introduced and fails in pilot use, MovementArtifact
consumers can be rolled back without reverting RunArtifact.

If RunArtifact is bumped, the team must define schema rollback and migration
steps before merge.

All golden updates must use dedicated `GOLDEN-` and/or `CONTRACT-` PRs with
diff evidence and customer impact notes.

## Open questions

- Should MovementArtifact have an independent version lifecycle?
- Is a warehouse graph schema required before MovementArtifact?
- Should travel time be fixed, distance-based, or calibration-driven?
- Is agent/resource-level identity required for R2?
- Should congestion be included in R2, or deferred?
- Does the first Unity movement view need continuous interpolation or discrete
  path segments?
- Should R2 share historical travel-time data with R5 calibration?
- How should MovementArtifact link to RunArtifact and ComparisonArtifact?
- What is the minimum customer-pilot evidence required before movement claims?

## Current decision

Do not change contracts in CORE-U4c.
Do not implement movement in CORE-U4c.
Keep RunArtifact v1 `position_timeline` as baseline layout positions, NOT simulated movement.
Use this document to prepare a future CONTRACT-R2 movement artifact proposal.
