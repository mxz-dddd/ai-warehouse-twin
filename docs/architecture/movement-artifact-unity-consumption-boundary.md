# MovementArtifact Unity Consumption Boundary

## Status

UNITY-R2a is planning/docs only.

It does not implement Unity movement animation.
It does not modify engine/unity, Sim.Cli, Sim.Core, Sim.Report, contracts,
scripts, datasets, CI, ingestion, or runtime service.

RunArtifact v1 position_timeline remains baseline layout positions, NOT
simulated movement.

The current sample MovementArtifact is a deterministic fixture-scale validation
baseline, not a claim of optimized or real-world movement.

## Current foundation

Track A currently has:

- MovementArtifact v1 schema.
- MovementArtifact generated contracts.
- MovementArtifact contract fixtures.
- Sim.Report MovementArtifact loader.
- MovementArtifact loader smoke coverage.
- Deterministic MovementArtifact generator.
- MovementArtifact input adapter.
- Adapter-generator-loader tests.
- Opt-in `export-movement-artifact` CLI.
- Sample MovementArtifact golden baseline.
- Report consumption boundary plan.

This foundation makes future Unity consumption reviewable, but it does not make
movement animation customer-ready. The existing Unity player consumes
RunArtifact state for the current artifact handoff. It must not reinterpret that
state as route, path, trajectory, or real movement.

## Non-goals

UNITY-R2a does not:

- implement Unity movement animation.
- modify `engine/unity/**`.
- add MovementArtifact loading code to Unity.
- modify `export-movement-artifact`.
- modify `export-artifact`, `compare-files`, or `render-report`.
- modify MovementArtifact v1 schema or generated contracts.
- modify MovementArtifact golden artifacts.
- add report movement sections.
- add runtime orchestration behavior.
- add Track C ingestion behavior.
- claim real-time forklift trajectories, actual worker paths, optimized routes,
  collision-free routes, live WMS movement, or physically validated travel paths.

## Unity consumption principle

A future Unity movement player may consume MovementArtifact only as an
explicitly supplied, validated MovementArtifact v1 artifact.

Unity must not infer movement from RunArtifact v1 position_timeline.
Unity must not treat baseline layout coordinates as simulated movement.

RunArtifact v1 position_timeline remains baseline layout positions, NOT
simulated movement.

Unity may continue to consume RunArtifact v1 for static layout/resource handoff
and current artifact playback semantics, as long as UI and documentation do not
describe that playback as real movement.

## Approved data sources

Future Unity movement animation may use only approved movement sources:

- explicit MovementArtifact v1 JSON.
- MovementArtifactLoader-equivalent validation result or generated typed
  contract parsing.
- `route_segments` from MovementArtifact.
- `movement_events` from MovementArtifact.
- `warehouse_graph` from MovementArtifact.
- `actors` from MovementArtifact.
- MovementArtifact provenance, including schema version, run id, source run
  artifact, graph source, and generator version.

If Unity needs additional data later, that data must be added through a
separate approved boundary. It must not be scraped from report Markdown,
runtime logs, or current RunArtifact v1 position timeline fields.

## Forbidden data sources

Unity movement animation must not use:

- RunArtifact v1 position_timeline.
- RunArtifact event_log_text.
- RunArtifact layout resources alone.
- current Unity static layout state.
- customer report Markdown.
- Track C raw ingestion records unless separately adapted and validated.
- runtime logs unless separately approved.
- UI player state or playback cursor state.

These sources may be useful for static display, debug context, or provenance,
but they are not movement semantics.

## Animation semantics policy

No real-time animation claim without real-time data source.
No actual forklift trajectory claim without measured or validated trajectory
source.
No optimized route claim without optimizer.
No collision-free claim without collision model.
No congestion claim without congestion model.
No worker walking path claim without worker path model or evidence.

Allowed wording for future early-stage Unity playback:

- "modeled movement events"
- "generated route segments"
- "deterministic fixture-scale movement baseline"
- "MovementArtifact playback"
- "MovementArtifact-derived animation preview"

Forbidden wording until separately proven and approved:

- "real-time forklift trajectory"
- "actual worker path"
- "optimized route"
- "collision-free route"
- "live WMS movement"
- "physically validated travel path"
- "真实叉车轨迹"
- "实时路径优化"
- "工人真实行走路线"

## Visual wording and UI policy

Any future Unity UI must label MovementArtifact animation by evidence level.

For current fixture-scale output, UI wording must be
internal/preview/validation-oriented. It should not be positioned as measured
warehouse truth or customer-ready optimization evidence.

Customer-facing Unity playback requires separate approval and wording review.
That review must include product, engineering, and validation sign-off on:

- what the animation proves.
- what it does not prove.
- whether movement data is fixture-scale, deterministic, calibrated,
  pilot-validated, or real-time.
- how customers can reproduce the artifact behind the playback.

## Validation policy

Before any Unity movement animation implementation, validation must prove:

- MovementArtifact loads successfully.
- `schema_version == "movement-artifact.v1"`.
- `artifact_kind == "warehouse-movement"`.
- non-empty `warehouse_graph`.
- non-empty `actors`.
- non-empty `movement_events`.
- non-empty `route_segments`.
- negative test proving Unity does not animate from RunArtifact
  position_timeline.
- test proving missing/invalid MovementArtifact disables movement playback.
- sample playback fixture or screenshot/golden policy if applicable.
- rollback path that disables MovementArtifact playback without changing static
  layout playback.

Unity-specific validation should stay separate from report and CLI validation.
Passing CLI export or report loader tests does not automatically approve Unity
animation.

## CLI and golden boundary

The sample MovementArtifact golden is a validation baseline.
It does not automatically become Unity default input.
Unity integration must be a separate approved PR.

The opt-in `export-movement-artifact` CLI can produce MovementArtifact files
for validation and future handoff experiments, but Unity must not silently load
those files as a customer-facing default.

Any future Unity fixture or screenshot golden must be separately approved and
must document:

- exact input artifact.
- visual scope.
- evidence level.
- regeneration command.
- rollback path.

## Report boundary

Report consumption is separate from Unity consumption.

Reports may later consume MovementArtifact through explicit report boundaries,
but report approval does not approve Unity movement animation. Unity approval
requires Unity-specific input validation, visual wording, and playback tests.

Report Markdown must not be used as a Unity movement input.

## Runtime orchestration boundary

Runtime orchestration remains unchanged.

Runtime may later pass MovementArtifact references only through separately
approved explicit artifact-generation steps. Runtime logs are not
MovementArtifact and must not be treated as route, path, or animation data.

## Track C ingestion boundary

Track C ingestion remains separate.

Raw WMS, telemetry, or adapter records should not be treated as MovementArtifact
unless adapted and validated by approved boundaries. Ingestion data may support
future calibration or real-world evidence, but it is not Unity animation input
by default.

## Rollback plan

Any future Unity MovementArtifact playback must be easy to disable without
breaking the current static RunArtifact layout/resource playback.

Rollback should:

- disable MovementArtifact playback behind a clear feature flag or explicit
  input requirement.
- keep static layout/resource rendering available.
- keep RunArtifact v1 and ComparisonArtifact v1 unchanged.
- keep report rendering unchanged unless the same PR explicitly owns report
  changes.
- remove customer-facing movement claims from UI copy.

## Go / no-go checklist

- [ ] Unity consumes explicit MovementArtifact v1 JSON only.
- [ ] MovementArtifact validation rejects missing or invalid input.
- [ ] Unity does not animate from RunArtifact v1 position_timeline.
- [ ] RunArtifact v1 position_timeline remains baseline layout positions, NOT
      simulated movement.
- [ ] UI labels evidence level clearly.
- [ ] No real-time, actual, optimized, collision-free, congestion, or physically
      validated movement claims are made without corresponding evidence.
- [ ] Static RunArtifact playback can continue if MovementArtifact playback is
      disabled.
- [ ] Report consumption, runtime orchestration, and Track C ingestion remain
      separate approval surfaces.
