# MovementArtifact Contract Generation Preflight

## Status

Preflight audit only. No schema file, generated contract, golden artifact, or implementation change in this PR.

This document does not approve MovementArtifact v1 implementation. It only records what a later CONTRACT- implementation PR must account for.

CONTRACT-R2c-preflight audits the current contract/schema/generation/drift flow before any MovementArtifact schema implementation. It does not add `movement-artifact.v1.schema.json`, update `packages/contracts`, change `src/Sim.Contracts`, regenerate code, update golden artifacts, implement movement/path/route, or modify Track C ingestion.

## Scope

This preflight covers:

- existing contract/schema layout.
- existing generated contract flow.
- existing drift checks.
- existing CI enforcement.
- existing artifact consumers.
- future MovementArtifact implementation impact.

The intent is to make the next CONTRACT- implementation PR smaller and safer by naming the current mechanics before any schema or generated contract changes happen.

## Non-goals

This preflight does not:

- add movement-artifact schema.
- modify `packages/contracts`.
- modify `src/Sim.Contracts`.
- regenerate code.
- add MovementArtifact runtime generation.
- add movement golden.
- change RunArtifact v1.
- change Track C ingestion.
- change Unity.
- implement movement/path/route.
- modify CI, scripts, datasets, artifact golden files, license files, or generated contracts.

## Current contract surfaces

Preflight findings:

- `packages/contracts` exists and contains JSON Schema files under:
  - `packages/contracts/domain/*.schema.json`
  - `packages/contracts/events/*.schema.json`
  - `packages/contracts/optimization/*.schema.json`
  - `packages/contracts/calibration/*.schema.json`
- `packages/contracts/generated` exists and contains generated outputs:
  - `packages/contracts/generated/csharp/Contracts.Generated.cs`
  - `packages/contracts/generated/python/contracts_generated.py`
  - `packages/contracts/generated/manifest.json`
- `src/Sim.Contracts` exists.
- `src/Sim.Contracts/Sim.Contracts.csproj` links `packages/contracts/generated/csharp/Contracts.Generated.cs` into the C# project as `Generated/Contracts.Generated.cs`.
- RunArtifact and ComparisonArtifact handoff contracts are currently handwritten C# records under `src/Sim.Contracts/Artifacts`.
- `schema_version` for RunArtifact is expressed as `RunArtifact.CurrentSchemaVersion = "run-artifact.v1"`.
- `schema_version` for ComparisonArtifact is expressed as `ComparisonArtifact.CurrentSchemaVersion = "comparison_artifact.v1"`.
- JSON schema files for RunArtifact v1 and ComparisonArtifact v1 were not found in preflight audit.
- TypeScript generated contracts were not found in preflight audit.
- Unity-specific contract files were not found in preflight audit.

## Current artifact schemas

### RunArtifact v1

Current RunArtifact v1 contract surface:

- Contract type: `src/Sim.Contracts/Artifacts/RunArtifact.cs`.
- Supporting types:
  - `src/Sim.Contracts/Artifacts/RunArtifactKpiSummary.cs`
  - `src/Sim.Contracts/Artifacts/RunArtifactLayout.cs`
  - `src/Sim.Contracts/Artifacts/RunArtifactLayoutResource.cs`
  - `src/Sim.Contracts/Artifacts/RunArtifactPositionTimelineEntry.cs`
- Loader: `src/Sim.Report/RunArtifactLoader.cs`.
- Loader tests: `src/Sim.Report.Tests/RunArtifactLoaderTests.cs`.
- Semantics guards: `src/Sim.Report.Tests/PositionTimelineSemanticsTests.cs`.
- Golden artifact: `datasets/sample-small-warehouse/artifacts/run-artifact.v1.json`.
- Smoke scripts touching it include:
  - `scripts/smoke-export-artifact.sh`
  - `scripts/smoke-unified-export-artifact.sh`
  - `scripts/audit-unified-export-artifact-diff.sh`
  - `scripts/smoke-customer-report.sh`
  - `scripts/check-all.sh`
- JSON schema file: Not found in preflight audit.
- Generated contract file for this artifact: Not found in preflight audit; current artifact model is handwritten C#.

Current RunArtifact v1 `position_timeline` remains baseline layout positions, NOT simulated movement.

### ComparisonArtifact v1

Current ComparisonArtifact v1 contract surface:

- Contract type: `src/Sim.Contracts/Artifacts/ComparisonArtifact.cs`.
- Supporting types:
  - `src/Sim.Contracts/Artifacts/ComparisonArtifactDelta.cs`
  - `src/Sim.Contracts/Artifacts/ComparisonArtifactMetrics.cs`
  - `src/Sim.Contracts/Artifacts/ComparisonArtifactScenarioSummary.cs`
- Loader: `src/Sim.Report/ComparisonArtifactLoader.cs`.
- Loader tests: `src/Sim.Report.Tests/ComparisonArtifactLoaderTests.cs`.
- Core characterization tests: `src/Sim.Core.Tests/Scenarios/ComparisonArtifactTests.cs`.
- Golden artifact: `datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json`.
- Smoke scripts touching it include:
  - `scripts/smoke-comparison-artifact.sh`
  - `scripts/smoke-unified-comparison-artifact.sh`
  - `scripts/smoke-customer-report.sh`
  - `scripts/check-all.sh`
- JSON schema file: Not found in preflight audit.
- Generated contract file for this artifact: Not found in preflight audit; current artifact model is handwritten C#.

## Current generated contract targets

Current generator:

- Script: `scripts/generate_contracts.py`.
- Wrapper: `scripts/gen-contracts.sh`.
- Source globs:
  - `packages/contracts/domain/*.schema.json`
  - `packages/contracts/events/*.schema.json`
  - `packages/contracts/optimization/*.schema.json`
  - `packages/contracts/calibration/*.schema.json`
- Generated targets:
  - C#: `packages/contracts/generated/csharp/Contracts.Generated.cs`
  - Python: `packages/contracts/generated/python/contracts_generated.py`
  - Manifest: `packages/contracts/generated/manifest.json`

Current generated C# types are included by `src/Sim.Contracts/Sim.Contracts.csproj`.

Generated contract compile coverage exists in `src/Sim.Core.Tests/Contracts/GeneratedContractsCompileTests.cs`.

Contract codegen tests exist in `tests/contract/test_contract_codegen.py`.

TypeScript generated contracts: Not found in preflight audit.

Unity contract generated targets: Not found in preflight audit.

Report/validation loaders exist for RunArtifact and ComparisonArtifact, but those artifact contracts are currently handwritten, not generated from JSON schema.

## Current drift checks

Current drift check:

- Script: `scripts/check-contract-drift.sh`.
- It runs `./scripts/gen-contracts.sh`.
- It checks `git diff --exit-code -- packages/contracts/generated`.
- It prints `PASS: generated contracts are up to date.` when generated outputs match tracked files.

Current generator writes only `packages/contracts/generated`; therefore the current drift check covers generated outputs for the schema globs in `scripts/generate_contracts.py`.

MovementArtifact is not currently included in the generator globs, generated outputs, or drift check.

Future MovementArtifact schema work must decide whether MovementArtifact joins this generator flow or remains a handwritten artifact contract like current RunArtifact / ComparisonArtifact.

## Current CI enforcement

Current CI workflow:

- Workflow: `.github/workflows/ci.yml`.
- Job: `Validate simulation core (${{ matrix.os }})`.
- Matrix: `ubuntu-latest`, `windows-latest`.
- Contract drift step: `Check contract drift`, running `./scripts/check-contract-drift.sh`.
- Linux-only full validation: `bash scripts/check-all.sh`, which also runs `./scripts/check-contract-drift.sh`.

Contract/artifact-related CI steps include:

- `Check contract drift`
- `Build`
- `Test`
- `Smoke export artifact`
- `Smoke comparison artifact`
- `Smoke customer report`
- Linux `Full local validation`

CI currently enforces generated contract drift for `packages/contracts/generated`. It does not automatically enforce a future MovementArtifact schema unless that schema is connected to the generator and drift check in a later CONTRACT- PR.

## Current artifact consumers

Preflight consumer findings:

| Consumer | Current artifact touchpoints | Notes |
|---|---|---|
| Sim.Cli | `src/Sim.Cli/Program.cs` maps run/comparison results to `RunArtifact` and `ComparisonArtifact`. | Produces current artifact JSON. |
| Sim.Report | `RunArtifactLoader`, `ComparisonArtifactLoader`, Markdown/customer report renderers. | Loads artifact contracts and validates current schema versions. |
| Sim.Validation | Scenario input validation and template checks. | No MovementArtifact consumer found. |
| Tests | `RunArtifactLoaderTests`, `ComparisonArtifactLoaderTests`, `ComparisonArtifactTests`, report renderer tests, semantics tests. | Characterize current artifacts and golden files. |
| scripts/smoke | export/comparison/customer report smokes and `check-all.sh`. | Exercise current artifact generation and golden comparison. |
| Unity | `engine/unity` exists in repository history, but no MovementArtifact or generated Unity contract consumer was found in this preflight audit. | Unity must not animate current RunArtifact v1 `position_timeline`. |
| Track C ingestion | `services/ingestion` exists, but is outside this task. | No change unless future graph/input contract explicitly changes. |

## MovementArtifact implementation implications

A future MovementArtifact implementation PR may need:

- schema file.
- contract type.
- generated type / copied type.
- loader.
- validator.
- fixture.
- golden strategy.
- drift check update.
- CI update.
- consumer wording guard.

Not implemented in this PR.

The implementation PR must decide whether MovementArtifact follows the existing `packages/contracts` schema-generation flow or the current handwritten artifact model used by RunArtifact / ComparisonArtifact. That decision affects drift checks, generated outputs, loader tests, and consumer review.

## Minimum future CONTRACT implementation scope

| Area | Likely files | Reason | Required in first schema PR? |
|---|---|---|---|
| schema definition | `packages/contracts/.../movement-artifact.v1.schema.json` or another approved schema path | Defines stable JSON shape. | Yes, if MovementArtifact becomes schema-backed. |
| generated contracts | `packages/contracts/generated/**`, `scripts/generate_contracts.py`, `scripts/gen-contracts.sh` | Keeps generated consumers aligned with schema. | Yes, if added to generator flow. |
| C# artifact model | `src/Sim.Contracts/Artifacts/**` or generated C# linked through `src/Sim.Contracts` | Enables .NET loaders/report consumers. | Yes, but exact source-of-truth must be chosen. |
| validator/loader | likely `src/Sim.Report/**`, `src/Sim.Validation/**`, or future dedicated loader | Reads and validates MovementArtifact JSON. | Minimal loader/validator likely yes. |
| tests | `src/Sim.*.Tests/**`, `tests/contract/**` | Characterizes schema, generation, loader, and bad cases. | Yes. |
| fixtures | future fixture path under an approved non-golden validation area | Exercises valid and invalid schema cases. | Yes. |
| golden | future MovementArtifact golden path | Byte-stable customer handoff fixture. | Maybe; only after generator/runtime exists. |
| CI drift check | `scripts/check-contract-drift.sh`, `.github/workflows/ci.yml`, `scripts/check-all.sh` | Prevents stale generated contracts. | Yes if schema/generated outputs are added. |
| Unity consumer | `engine/unity/**` | Future path animation handoff. | No for first schema PR unless explicitly authorized. |
| Report consumer | `src/Sim.Report/**` | Future provenance/report sections. | Minimal loader/wording only if scope includes reports. |
| Track C ingestion | `services/ingestion/**`, ingestion workflows/scripts | Graph/input source integration. | No, unless future graph/input contract changes. |

## Files that should remain unchanged in preflight

The following paths should remain unchanged in CONTRACT-R2c-preflight:

- `src/**`
- `packages/contracts/**`
- `datasets/**`
- `scripts/**`
- `.github/**`
- `engine/unity/**`
- `services/ingestion/**`
- `LICENSE`
- `NOTICE`

## Risks found

### No single obvious schema source of truth for artifacts

Observed. Domain/event/optimization/calibration contracts use `packages/contracts/*.schema.json` with generated C# and Python outputs. RunArtifact and ComparisonArtifact are handwritten C# artifact contracts under `src/Sim.Contracts/Artifacts` with no JSON schema found in preflight audit.

Mitigation: a future CONTRACT- PR must choose whether MovementArtifact joins schema-first generation or follows the current handwritten artifact model. The choice must be explicit.

### Generated contracts may be manual or partially checked

Partially observed. Generated domain/event/optimization/calibration contracts are checked by `scripts/check-contract-drift.sh`, but RunArtifact and ComparisonArtifact handwritten artifact contracts are not generated from JSON schema.

Mitigation: if MovementArtifact is generated, add it to generator globs and drift checks. If handwritten, document why and add equivalent loader/contract tests.

### CI drift step may not cover future MovementArtifact automatically

Observed. The generator globs in `scripts/generate_contracts.py` do not include a movement artifact path today, so CI will not cover MovementArtifact unless the future PR updates generator/drift logic.

Mitigation: future CONTRACT- PR must update drift checks or document a separate contract guard.

### Future schema PR could accidentally touch RunArtifact v1

Risk observed from scope, not from current changes. MovementArtifact should be additive and separate; RunArtifact v1 remains stable.

Mitigation: future PR should include a forbidden-diff check for RunArtifact v1 schema/contract/golden unless explicitly authorized.

### Track C graph/input scope could creep in

Risk observed from MovementArtifact graph needs, not from current changes.

Mitigation: Track C should not be modified unless future graph/input contract changes are explicitly in scope.

## Recommended next CONTRACT step

Recommended next step:

`CONTRACT-R2d: add MovementArtifact schema and minimal generated contract, only after this preflight is merged and exact paths/commands are confirmed.`

Do not start CONTRACT-R2d until the preflight is reviewed.

Before CONTRACT-R2d, decide:

- whether MovementArtifact belongs in `packages/contracts` generation or handwritten `src/Sim.Contracts/Artifacts`.
- exact schema path.
- exact generated outputs.
- whether first PR includes loader/validation tests or only schema/generated contract tests.
- whether a golden artifact is deferred until runtime generation exists.

## Current decision

Do not add MovementArtifact schema in CONTRACT-R2c-preflight.

Do not modify contracts or generated code.

Use this audit to prepare a later CONTRACT implementation PR.

RunArtifact v1 `position_timeline` remains baseline layout positions, NOT simulated movement.
