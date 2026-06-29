# Unified Default Runner Switch Plan

## Status

Implemented by `GOLDEN-U3d-default-unified-runner`.

This document remains as the migration record for the default runner switch.
The original planning gates were satisfied by CORE-U3d-1 through CORE-U3d-4,
then this dedicated `GOLDEN-` PR performed the customer-facing default switch
and regenerated tracked golden artifacts.

## Scope

This plan covers the customer-facing CLI entry points that may eventually move
from legacy runner semantics to unified runner semantics:

- `sample-small-warehouse`: CLI run output entry point.
- `run-file`: CLI run output entry point for a scenario JSON file.
- `export-artifact`: RunArtifact generation entry point.
- `compare-files`: ComparisonArtifact generation entry point.
- `render-report`: artifact / report consumer; it does not run simulation.

This switch updates golden files but does not change contracts or create a
release.

## Current state

- Default `sample-small-warehouse`: unified.
- `sample-small-warehouse --runner unified`: opt-in unified.
- `sample-small-warehouse --runner legacy`: explicit legacy fallback.
- Default `run-file`: unified.
- `run-file --runner unified`: opt-in unified.
- `run-file --runner legacy`: explicit legacy fallback.
- Default `export-artifact`: unified.
- `export-artifact --runner unified`: opt-in unified.
- `export-artifact --runner legacy`: explicit legacy fallback.
- Default `compare-files`: unified.
- `compare-files --runner unified`: opt-in unified.
- `compare-files --runner legacy`: explicit legacy fallback.
- `render-report` can show operator-provided provenance, but artifact schemas do
  not store runner mode.

## Target state

Customer-facing default simulation paths use unified runner semantics
consistently.

The target state must preserve these constraints:

- RunArtifact and ComparisonArtifact must not silently use different runner
  semantics.
- Reports must not imply that artifact schemas contain runner provenance.
- Position timelines must not be described as simulated movement.
- Position timelines remain baseline layout positions, NOT simulated movement
  until R2 real movement semantics land.
- Default switch work must not leave tracked golden files representing old
  legacy behavior.

## Customer-visible changes

The default switch is visible to customers and artifact consumers:

- KPI timing may change. The current readiness audit shows `finished_at_ms`
  changing from legacy `220` to unified `410` for the sample warehouse.
- `total_duration_ms` may change.
- Event log count may change.
- Position timeline count may change.
- Layout resources may change because the unified adapter currently models
  coarser operations than the legacy multi-stage lease trace.
- Reports can display runner provenance when operators provide the render flags.
- Customer report KPI values may change, so migration notes are required.

## Commands affected

The following defaults are affected by a future switch:

```bash
dotnet run --project src/Sim.Cli -- sample-small-warehouse
dotnet run --project src/Sim.Cli -- run-file <scenario>
dotnet run --project src/Sim.Cli -- export-artifact <scenario> -o <artifact>
dotnet run --project src/Sim.Cli -- compare-files <baseline> <candidate> -o <comparison>
```

## Commands not affected

`render-report` is not a simulation entry point:

- It does not run simulation.
- It only consumes RunArtifact and ComparisonArtifact files.
- It can display operator-provided provenance.
- Artifact schemas do not gain a `runner_mode` field.

## Completed readiness gates

Before the default switch PR, these gates were satisfied:

1. `export-artifact --runner unified` smoke passes.
2. `compare-files --runner unified` smoke passes.
3. `render-report` provenance smoke passes.
4. Artifact switch readiness audit passes.
5. Golden update policy is merged.
6. Product owner confirms customer-visible KPI changes are acceptable.
7. Visualization / artifact consumer confirms position timeline semantics remain
   clear.
8. Report consumer confirms provenance wording is sufficient.
9. Contract owner confirms no schema version bump is required, or if a bump is
   required, the work first goes through `CONTRACT-` governance.
10. Dedicated `GOLDEN-` PR was prepared and used for the switch.

## Completed GOLDEN PR

The default switch was not merged as a generic CORE PR.
It was performed by a dedicated `GOLDEN-` PR.

The `GOLDEN-` PR includes:

- Default runner switch implementation.
- Regenerated run artifact golden.
- Regenerated comparison artifact golden.
- Customer report golden review.
- Golden diff summary.
- Customer impact statement.
- Regeneration commands.
- Rollback plan.
- Release / migration note reference.

## Release note requirements

### Draft release note

AI Warehouse Twin now uses the unified warehouse runner as the default
customer-facing simulation path. The unified runner applies shared resource and
shared inventory semantics across inbound, outbound, and each-pick operations.
As a result, timing KPIs, event logs, and artifact timelines may differ from
legacy artifacts generated before this change.

Position timelines remain baseline layout positions, NOT simulated movement.

## Migration note requirements

Migration notes must make these points explicit:

- Old golden files represent legacy semantics.
- New golden files represent unified semantics.
- Users and customers should not mix historical legacy reports with new unified
  reports without runner provenance.
- To reproduce old data, use `--runner legacy`.
- To generate new unified resource contention results, use the new default after
  the switch or explicitly use `--runner unified`.
- Reports should be rendered with provenance flags so readers can see the runner
  source for RunArtifact and ComparisonArtifact.

## Rollback plan

If default switch behavior causes unexpected KPI or artifact changes:

- First revert the default switch / `GOLDEN-` PR.
- After revert, defaults return to legacy.
- Keep opt-in unified runner paths unless they are the direct cause of the
  regression.
- Do not change schema during rollback unless a separate schema PR handles it.
- Do not roll back unrelated documentation, policy, ingestion, WMS, or Unity
  changes.

## Pre-switch do-not-switch conditions

The team would not switch default runner while any of these conditions held:

- Readiness audit still says not ready.
- Golden diff lacks product explanation.
- Report provenance is incomplete.
- `compare-files` lacks unified mode.
- Artifact consumer does not accept current position timeline semantics.
- CI or required smoke validation is failing.
- Customer-visible KPI change lacks migration notes.
- Unrelated code changes are mixed into the PR.
- Ingestion / WMS / Unity unrelated changes are mixed into the PR.

## Proposed implementation sequence

1. CORE-U3d-4-plan: merge release / migration plan.
2. Optional: tighten manual smoke docs if needed.
3. GOLDEN-U3d-default-unified-runner: dedicated PR that switches defaults and
   updates golden files.
4. Post-merge: tag or release note only after product owner approval.
5. CORE-U4: reconcile unified intervals with position timeline semantics.

## Current decision

Default runner has switched to unified in the dedicated `GOLDEN-` PR.

Explicit legacy fallback remains available through `--runner legacy` for
reproducing pre-switch outputs.
