# Track A R2 Readiness Audit

## Status

TRACKA-R2a adds the Track A R2 readiness audit and release gate.

It does not implement new product behavior.
It does not modify MovementArtifact schema, generated contracts, production
generator, production adapter, CLI semantics, reports, Unity, Track C
ingestion, or runtime orchestration.

RunArtifact v1 position_timeline remains baseline layout positions, NOT
simulated movement.

## Completed foundation

Track A R2 currently has the following foundation in place:

- MovementArtifact schema / generated contracts.
- MovementArtifact contract fixtures.
- Sim.Report MovementArtifact loader.
- MovementArtifact loader smoke.
- deterministic MovementArtifact generator.
- MovementArtifact input adapter.
- adapter-generator-loader e2e tests.
- opt-in `export-movement-artifact` CLI.
- MovementArtifact golden baseline.
- report consumption boundary.
- Unity consumption boundary.

These pieces make MovementArtifact reviewable as a separate artifact surface.
They do not make movement output customer-facing by default.

## Release gate

`scripts/check-track-a-r2-readiness.sh` is the lightweight Track A R2 readiness
gate.

`scripts/check-all.sh` invokes it.

The gate checks that required files exist and that honesty boundaries remain
present. It intentionally does not write files, generate artifacts, or run heavy
test suites.

## Required checks

The readiness gate checks for:

- MovementArtifact v1 schema.
- generated C# / Python contracts and manifest.
- sample MovementArtifact golden baseline.
- MovementArtifact loader smoke.
- MovementArtifact export smoke.
- MovementArtifact generator.
- MovementArtifact input adapter.
- MovementArtifact loader.
- CLI export wording.
- report consumption boundary.
- Unity consumption boundary.
- `baseline layout positions, NOT simulated movement` honesty language.
- fixture-scale wording.
- absence of MovementArtifact implementation under `engine/unity`.
- absence of MovementArtifact report section implementation under
  `src/Sim.Report`.

## MovementArtifact boundaries

MovementArtifact is separate from RunArtifact v1 and ComparisonArtifact v1.

MovementArtifact is generated only through approved adapter/generator/CLI
boundaries. It must not be inferred from RunArtifact v1 position timeline,
event-log text, report Markdown, Unity player state, Track C raw ingestion
records, or runtime logs.

The current sample MovementArtifact is a deterministic fixture-scale validation
baseline.

## Customer-facing honesty boundaries

RunArtifact v1 position_timeline remains baseline layout positions, NOT
simulated movement.

No real-time forklift trajectory claim.
No actual worker path claim.
No optimized route claim.
No collision-free route claim.
No live WMS movement claim.
No physically validated travel path claim.

Allowed early wording should stay in the preview/validation lane:

- deterministic fixture-scale movement baseline.
- MovementArtifact-derived validation output.
- generated route segments.
- modeled movement events.

## Report boundary

Report movement sections require separate implementation PR.

The current report boundary allows planning and loader validation only. Customer
report Markdown must not silently consume MovementArtifact, and report
rendering must not infer movement from RunArtifact v1 position_timeline.

A future report implementation must own wording, provenance, customer impact,
golden updates, and rollback.

## Unity boundary

Unity movement animation requires separate implementation PR.

Unity must not animate from RunArtifact v1 position_timeline, current static
layout state, report Markdown, Track C raw ingestion records, or runtime logs.
Future Unity playback must explicitly consume validated MovementArtifact input
and label evidence level.

## Runtime boundary

Runtime orchestration must not manufacture movement semantics.

The runtime service may later pass MovementArtifact references only through
separately approved explicit artifact-generation steps. Runtime logs are not a
MovementArtifact and must not be treated as route, path, or animation data.

## Track C boundary

Track C ingestion remains separate and raw ingestion records are not
MovementArtifact unless adapted and validated by approved boundaries.

Ingestion data may support future calibration or real-world evidence, but it is
not a MovementArtifact, report section, or Unity animation input by default.

## Known limitations

Current MovementArtifact output is fixture-scale.
It is not calibrated real-world movement.
It is not a route optimizer.
It is not a collision model.
It is not a congestion model.
It is not customer-facing by default.

## Rollback plan

If a future MovementArtifact consumer crosses a boundary, rollback should:

- disable that consumer without changing RunArtifact v1.
- keep ComparisonArtifact v1 unchanged.
- keep the sample MovementArtifact golden available as a validation baseline.
- keep `export-movement-artifact` opt-in.
- remove customer-facing movement claims from reports, UI, or runtime logs.
- keep Track C ingestion and runtime orchestration separate.

## Go / no-go checklist

- [ ] `scripts/check-track-a-r2-readiness.sh` passes.
- [ ] `scripts/check-all.sh` invokes the readiness gate.
- [ ] RunArtifact v1 position_timeline remains baseline layout positions, NOT
      simulated movement.
- [ ] MovementArtifact schema/generated contracts remain present.
- [ ] MovementArtifact loader/generator/adapter remain present.
- [ ] MovementArtifact export remains opt-in.
- [ ] MovementArtifact golden baseline remains deterministic validation data.
- [ ] Report movement sections require separate implementation PR.
- [ ] Unity movement animation requires separate implementation PR.
- [ ] Runtime orchestration does not manufacture movement semantics.
- [ ] Track C raw ingestion records are not treated as MovementArtifact.
