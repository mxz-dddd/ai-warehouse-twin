# MovementArtifact CLI Export Plan

## Status

CLI-R2a is planning/docs only.

It does not implement `export-movement-artifact`.
It does not modify Sim.Cli, Sim.Core, Sim.Report, Unity, ingestion, runtime service, contracts, scripts, datasets, or golden artifacts.

This plan defines a future opt-in CLI surface for exporting MovementArtifact v1 JSON after the current internal adapter-generator-loader boundary has been proven. It preserves the current customer-facing artifact and report behavior while making the next CLI step reviewable and reversible.

CLI-R2b implements the first opt-in `export-movement-artifact` command. The command writes only the explicit output path requested by `-o/--output`. It does not change `export-artifact`, `compare-files`, `render-report`, golden artifacts, reports, Unity, Track C ingestion, or runtime orchestration.

GOLDEN-R2a adds the first controlled MovementArtifact golden at `datasets/sample-small-warehouse/artifacts/movement-artifact.v1.json`. The golden is generated only through the opt-in `export-movement-artifact` command with explicit deterministic options. It is checked by `smoke-movement-artifact-export.sh` and `check-all.sh`. This does not make MovementArtifact customer-facing by default and does not change `export-artifact`, `compare-files`, `render-report`, reports, Unity, Track C ingestion, or runtime orchestration.

REPORT-R2a-consumption-plan keeps report consumption planning separate from CLI
export. The MovementArtifact golden can be loaded and smoked, but current
`render-report` output remains unchanged until a dedicated report implementation
PR approves wording, provenance, customer impact, golden updates, and rollback.

UNITY-R2a keeps opt-in CLI export separate from Unity animation. The exported
MovementArtifact and sample golden remain validation artifacts until a separate
Unity implementation PR explicitly consumes and validates MovementArtifact
input.

TRACKA-R2a adds a readiness gate that confirms the opt-in CLI export,
MovementArtifact golden, and honesty boundaries remain present.

RunArtifact v1 position_timeline remains baseline layout positions, NOT simulated movement.

## Current foundation

The current Track A foundation includes:

- MovementArtifact v1 schema and generated contracts.
- MovementArtifact contract fixtures and fixture tests.
- Sim.Report MovementArtifact loader and loader smoke coverage.
- Minimal in-memory deterministic MovementArtifact generator.
- Generator-to-JSON-to-loader compatibility tests.
- Deterministic MovementArtifact input adapter from explicit scenario/layout/resource inputs.
- Adapter-to-generator-to-JSON-to-loader end-to-end boundary tests.
- RunArtifact v1 and ComparisonArtifact v1 default unified runner handoff.
- Guardrails that keep RunArtifact v1 `position_timeline` as baseline layout positions, NOT simulated movement.

This foundation is enough to plan an export command, but it is not yet customer-facing movement output.

## Non-goals

CLI-R2a does not:

- implement `export-movement-artifact`.
- change existing `export-artifact`, `compare-files`, or `render-report` behavior.
- write MovementArtifact files.
- add MovementArtifact golden artifacts.
- modify MovementArtifact v1 schema or generated contracts.
- modify RunArtifact v1 or ComparisonArtifact v1.
- modify `MovementArtifactInputAdapter` production code.
- modify `MovementArtifactGenerator` production code.
- add customer report movement sections.
- add Unity movement animation.
- modify Track C ingestion, ingestion workflows, ingestion fixtures, or WMS adapters.
- modify runtime orchestration service behavior.
- infer movement, route, path, travel time, congestion, or animation from RunArtifact v1 `position_timeline`.

## Proposed command

Future proposal:

```bash
dotnet run --project src/Sim.Cli -- export-movement-artifact <scenario.json> -o <movement-artifact.v1.json>
```

Possible future options:

```text
--run-id <id>
--source-run-artifact <path>
--graph-source <label>
--generator-version <version>
--runner unified
```

The first implementation should stay intentionally small. It should be opt-in, should not change existing commands, and should not become the default output path for customer reports or artifact generation.

## Proposed input and output

Input:

- a warehouse `scenario.json` accepted by the existing scenario loader.
- explicit command options such as output path, run id, source artifact label, graph source label, and generator version.

Output:

- a standalone MovementArtifact v1 JSON file at the requested `-o` path.

The output should be a separate artifact. It should not mutate RunArtifact v1, ComparisonArtifact v1, customer report Markdown, dataset golden files, or Unity player state.

## Proposed execution pipeline

Future implementation should follow this pipeline:

```text
scenario.json
  -> existing scenario loader
  -> MovementArtifactInputAdapter.FromScenario(...)
  -> MovementArtifactGenerator.Generate(...)
  -> JSON serialization
  -> output file
```

The CLI must not read RunArtifact v1 position_timeline as movement input.
The CLI must not infer route/path/movement from rendered RunArtifact JSON.

If the command accepts `--source-run-artifact`, that path should be provenance only unless a later approved task explicitly defines stronger linkage semantics.

## Determinism policy

Same scenario + same options must produce byte-stable JSON output.

Required constraints:

- No wall-clock timestamps.
- No random IDs.
- Stable ordering by ids/timestamps.
- Stable JSON serialization options.
- Stable option defaults.
- Stable provenance strings for the same invocation.

If future implementation needs a run id, it should require an explicit value or derive one deterministically from approved inputs.

## Validation policy

Future CLI implementation should add tests and smoke checks proving:

- CLI exports valid MovementArtifact JSON.
- Exported JSON loads through MovementArtifactLoader.
- Same input twice produces identical bytes.
- Invalid scenario fails clearly.
- Command does not change `export-artifact` / `compare-files` / `render-report`.
- Command does not update golden unless explicit GOLDEN PR.
- Command does not read or infer movement from RunArtifact v1 `position_timeline`.

The first implementation should prefer temporary output directories in smoke tests. It should not write into `datasets/**/artifacts` unless a separate GOLDEN task explicitly authorizes tracked golden changes.

## Golden policy

No MovementArtifact golden should be added in the CLI implementation PR unless a separate GOLDEN task explicitly authorizes it.

First CLI implementation should use temp output smoke tests, not tracked golden.

MovementArtifact golden requires separate regeneration command, diff summary, customer impact statement, and rollback plan.

Until that separate approval exists, MovementArtifact CLI output is a generated validation artifact, not a committed customer handoff baseline.

## Customer-facing policy

`export-movement-artifact` is opt-in.

Existing customer report and default artifact paths remain unchanged.

No customer-facing movement claims are introduced by CLI export alone.

MovementArtifact output may become customer-facing only after separate approval for wording, provenance, confidence/validation level, report integration, and rollback behavior.

## Report and Unity policy

Report and Unity must not consume MovementArtifact automatically from this CLI.

Report movement sections and Unity movement animation require separate approved PRs.

Unity must not animate from RunArtifact v1 position_timeline.

Future Report or Unity consumers must clearly distinguish real MovementArtifact data from RunArtifact v1 baseline layout handoff data.

## Runtime orchestration boundary

Runtime orchestration may later call `export-movement-artifact`, but CLI-R2a does not modify `services/runtime`.

Runtime service must not manufacture movement semantics.

If runtime orchestration later invokes this CLI, it should treat the command as an explicit artifact-generation step with logged inputs, deterministic options, and clear provenance.

## Track C ingestion boundary

Track C ingestion remains separate.

CLI-R2a does not modify `services/ingestion`, ingestion workflows, ingestion fixtures, or WMS adapters.

Future ingestion output may eventually provide better warehouse graph, location, order, resource, or WMS identity inputs. This CLI plan does not add those fields and does not depend on Track C changes.

## Rollback plan

Because CLI-R2a is planning/docs only, rollback is limited to reverting this document and related status notes.

For a future implementation PR, rollback should preserve:

- existing `export-artifact` behavior.
- existing `compare-files` behavior.
- existing `render-report` behavior.
- existing RunArtifact v1 and ComparisonArtifact v1 schemas and golden files.
- existing report output.
- existing Unity static layout behavior.
- existing ingestion and runtime service behavior.

The implementation should be additive and opt-in so rollback can remove only the new command surface and its smoke tests.

## Go / no-go checklist

Before implementing `export-movement-artifact`, confirm:

- [ ] command is opt-in and does not alter existing CLI defaults.
- [ ] scenario loading path is explicit and deterministic.
- [ ] adapter input source is reviewed.
- [ ] `MovementArtifactInputAdapter.FromScenario(...)` mapping is documented.
- [ ] `MovementArtifactGenerator.Generate(...)` output is byte-stable for same inputs.
- [ ] JSON serialization options are stable across platforms.
- [ ] output path handling is safe and clear.
- [ ] invalid scenarios fail with actionable errors.
- [ ] exported JSON loads through MovementArtifactLoader.
- [ ] no MovementArtifact golden is added without a separate GOLDEN task.
- [ ] reports do not automatically consume MovementArtifact output.
- [ ] Unity does not animate MovementArtifact output without separate approval.
- [ ] runtime orchestration remains unchanged unless separately approved.
- [ ] Track C ingestion remains unchanged.
- [ ] RunArtifact v1 position_timeline remains baseline layout positions, NOT simulated movement.
