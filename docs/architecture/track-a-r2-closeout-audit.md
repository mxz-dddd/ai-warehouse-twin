# Track A R2 Closeout Audit

## Status

TRACKA-R2b is the final Track A R2 closeout audit.

It does not implement new product behavior.
It does not modify production code, schemas, generated contracts, datasets,
golden artifacts, CLI behavior, reports, Unity, Track C ingestion, or runtime
orchestration.

Track A R2 closes the MovementArtifact foundation as a deterministic,
reviewable, opt-in artifact path. It does not make movement output
customer-facing by default.

## Scope completed in Track A R2

Track A R2 completed:

- MovementArtifact v1 schema and generated contracts.
- contract fixtures and schema fixture tests.
- Sim.Report MovementArtifact loader.
- MovementArtifact loader smoke.
- deterministic MovementArtifact generator.
- MovementArtifact input adapter.
- adapter-generator-loader e2e tests.
- opt-in `export-movement-artifact` CLI.
- sample MovementArtifact golden baseline.
- report consumption boundary.
- Unity consumption boundary.
- Track A R2 readiness gate.

This completed scope gives the product a stable MovementArtifact handoff
foundation without changing RunArtifact v1, ComparisonArtifact v1, reports,
Unity, Track C ingestion, or runtime orchestration behavior.

## Acceptance evidence

Final Track A R2 acceptance depends on:

```bash
python3 -m pytest tests/contract/test_contract_codegen.py tests/contract/test_movement_artifact_schema_fixtures.py

./scripts/smoke-movement-artifact-loader.sh
./scripts/smoke-movement-artifact-export.sh
./scripts/check-track-a-r2-readiness.sh
./scripts/check-all.sh
./scripts/check-contract-drift.sh

dotnet build --disable-build-servers
dotnet test --disable-build-servers
```

The readiness gate is intentionally lightweight. It checks presence and
guardrails; it does not replace contract tests, smoke tests, build, or full
test execution.

## Release gate

`scripts/check-track-a-r2-readiness.sh` is the executable Track A R2 release
gate.

It verifies that MovementArtifact schema, generated contracts, loader,
generator, adapter, CLI export, golden baseline, report boundary, Unity
boundary, and honesty language remain present.

`scripts/check-all.sh` invokes this gate so the local full validation path
continues to protect Track A R2 boundaries.

## MovementArtifact artifact chain

The approved Track A R2 MovementArtifact chain is:

```text
scenario.json
  -> MovementArtifactInputAdapter
  -> MovementArtifactGenerator
  -> export-movement-artifact
  -> MovementArtifact v1 JSON
  -> MovementArtifactLoader
  -> smoke/check-all/readiness gate
```

The chain starts from explicit scenario/layout/resource inputs and approved
adapter policy. It must not infer movement from RunArtifact v1
`position_timeline`.

## CLI and golden baseline

`export-movement-artifact` is opt-in.

The sample MovementArtifact golden baseline is a deterministic validation
baseline. It is not a default customer output and does not modify
`export-artifact`, `compare-files`, or `render-report`.

MovementArtifact golden changes require a dedicated golden review with diff
summary, regeneration commands, customer impact, and rollback plan.

## Report boundary

Report movement section implementation is out of scope for Track A R2.

Report movement sections require separate implementation PR.

Reports may load MovementArtifact through approved loader boundaries, but
customer report Markdown must not silently consume MovementArtifact or infer
movement from RunArtifact v1 `position_timeline`.

## Unity boundary

Unity movement animation implementation is out of scope for Track A R2.

Unity movement animation requires separate implementation PR.

Unity must not animate from RunArtifact v1 `position_timeline`, current static
layout state, report Markdown, Track C raw ingestion records, or runtime logs.
Future Unity playback must explicitly consume validated MovementArtifact input
and label evidence level.

## Runtime boundary

Runtime orchestration invoking MovementArtifact export is out of scope for
Track A R2.

Runtime orchestration must not manufacture movement semantics. Runtime may
later pass MovementArtifact references only through separately approved
explicit artifact-generation steps.

## Track C boundary

Track C WMS/ingestion integration is out of scope for Track A R2.

Track C ingestion remains separate and raw ingestion records are not
MovementArtifact unless adapted and validated by approved boundaries.

## Customer-facing honesty rules

RunArtifact v1 position_timeline remains baseline layout positions, NOT
simulated movement.

No real-time forklift trajectory claim.
No actual worker path claim.
No optimized route claim.
No collision-free route claim.
No live WMS movement claim.
No physically validated travel path claim.

MovementArtifact output should be described according to evidence level. The
current baseline is deterministic and fixture-scale, not calibrated warehouse
truth.

## Known limitations

Current MovementArtifact output is fixture-scale.
It is not calibrated real-world movement.
It is not a route optimizer.
It is not a collision model.
It is not a congestion model.
It is not customer-facing by default.

## Out of scope for Track A R2

- Report movement section implementation.
- Unity movement animation implementation.
- Track C WMS/ingestion integration.
- Runtime orchestration invoking MovementArtifact export.
- Real route optimization.
- Real-time telemetry playback.
- Customer-facing movement ROI claims.

## Final validation commands

Run the final acceptance set before treating Track A R2 as closed:

```bash
python3 -m pytest tests/contract/test_contract_codegen.py tests/contract/test_movement_artifact_schema_fixtures.py

./scripts/smoke-movement-artifact-loader.sh
./scripts/smoke-movement-artifact-export.sh
./scripts/check-track-a-r2-readiness.sh
./scripts/check-all.sh
./scripts/check-contract-drift.sh

dotnet build --disable-build-servers
dotnet test --disable-build-servers
```

Also confirm no changes to Track B handoff, contracts, generated contracts,
scripts, production code, datasets/golden artifacts, CI, Unity, ingestion, or
runtime service unless a future task explicitly authorizes them.

## Rollback plan

If a future MovementArtifact consumer causes customer-facing confusion, rollback
should:

- disable that consumer without changing RunArtifact v1.
- keep ComparisonArtifact v1 unchanged.
- keep `export-movement-artifact` opt-in.
- keep the sample MovementArtifact golden available as a validation baseline.
- remove report movement sections or Unity playback entry points introduced by
  the offending PR.
- remove customer-facing movement claims from docs, UI, reports, or runtime
  logs.
- keep Track C ingestion and runtime orchestration separate.

## Go / no-go checklist

- [ ] MovementArtifact schema/generated contracts are present.
- [ ] contract fixtures and fixture tests pass.
- [ ] MovementArtifact loader and loader smoke pass.
- [ ] deterministic generator and input adapter tests pass.
- [ ] adapter-generator-loader e2e tests pass.
- [ ] `export-movement-artifact` remains opt-in.
- [ ] sample MovementArtifact golden baseline is deterministic.
- [ ] report consumption boundary remains planning/approval only.
- [ ] Unity consumption boundary remains planning/approval only.
- [ ] `scripts/check-track-a-r2-readiness.sh` passes.
- [ ] `scripts/check-all.sh` invokes the readiness gate.
- [ ] RunArtifact v1 position_timeline remains baseline layout positions, NOT
      simulated movement.
- [ ] no customer-facing movement ROI, real-time, actual path, optimized route,
      collision-free, congestion, or physically validated movement claims are
      introduced.
