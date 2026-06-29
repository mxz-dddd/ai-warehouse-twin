# Unified Artifact Switch Readiness Audit

## Status

CORE-U3d-readiness only. No default switch.

This audit documents the current difference between the legacy default
`export-artifact` path and the opt-in unified runner path added in CORE-U3c.
It is a readiness check for a future default switch, not the switch itself.

CORE-U3d-1 aligns compare-files runner mode before any default switch.
CORE-U3d-2 adds report-visible runner provenance through render-report flags.
CORE-U3d-3 adds the golden update policy required before any default runner
switch or tracked golden artifact update.
CORE-U3d-4-plan adds the release / migration / rollback plan required before
any dedicated GOLDEN default-switch PR.

## Scope

- Default `export-artifact` remains legacy.
- `export-artifact --runner unified` remains opt-in.
- Tracked golden files are unchanged.
- RunArtifact schema is unchanged.
- `compare-files` remains legacy.
- `render-report` remains a consumer only.
- Runner provenance is operator-provided and is not stored in RunArtifact v1
  or ComparisonArtifact v1.
- Default `render-report` output remains unchanged.
- No customer-facing movement claim changes are made here.

## Commands audited

The audit script is:

```bash
bash scripts/audit-unified-export-artifact-diff.sh
```

It generates three temporary artifacts without writing to `datasets/**`:

```bash
dotnet run --project src/Sim.Cli -- export-artifact \
  datasets/sample-small-warehouse/scenario.json \
  -o "$tmp_dir/legacy-default.json"

dotnet run --project src/Sim.Cli -- export-artifact \
  datasets/sample-small-warehouse/scenario.json \
  -o "$tmp_dir/legacy-explicit.json" \
  --runner legacy

dotnet run --project src/Sim.Cli -- export-artifact \
  datasets/sample-small-warehouse/scenario.json \
  -o "$tmp_dir/unified.json" \
  --runner unified
```

## Legacy artifact baseline

The default legacy artifact and explicit legacy artifact both match the tracked
golden file byte-for-byte:

```bash
cmp "$tmp_dir/legacy-default.json" datasets/sample-small-warehouse/artifacts/run-artifact.v1.json
cmp "$tmp_dir/legacy-explicit.json" datasets/sample-small-warehouse/artifacts/run-artifact.v1.json
```

The legacy baseline remains the customer-facing tracked golden for
`datasets/sample-small-warehouse/artifacts/run-artifact.v1.json`.

## Opt-in unified artifact summary

The opt-in unified artifact is valid JSON and keeps the existing RunArtifact v1
contract:

- `schema_version`: `run-artifact.v1`
- `artifact_kind`: `warehouse-simulation-run`
- `scenario_id`: `sample-small-warehouse`
- `seed`: `20240627`

The unified artifact is intentionally different from the legacy artifact
because it is produced from the unified adapter path and shared resource /
shared inventory baseline.

## Diff summary

Actual audit output:

| Metric | Legacy | Unified |
| --- | --- | --- |
| `schema_version` | `run-artifact.v1` | `run-artifact.v1` |
| `artifact_kind` | `warehouse-simulation-run` | `warehouse-simulation-run` |
| `scenario_id` | `sample-small-warehouse` | `sample-small-warehouse` |
| `seed` | `20240627` | `20240627` |
| `started_at_ms` | `10` | `10` |
| `finished_at_ms` | `220` | `410` |
| `final_world_time_ms` | `220` | `410` |
| `kpi_summary.total_duration_ms` | `210` | `400` |
| `kpi_summary.total_completed_work_items` | `3` | `3` |
| `kpi_summary.event_log_line_count` | `10` | `13` |
| `layout.resources` | `dock-1`, `forklift-1`, `station-1`, `worker-1` | `dock-1`, `station-1` |
| `position_timeline.count` | `12` | `6` |
| `event_log.count` | `10` | `13` |

## Intentional differences

The differences are expected and should not be treated as a regression by
themselves:

- The unified artifact uses the explicit `WarehouseScenarioToUnifiedScenarioAdapter`
  path and one coarse unified operation per inbound receipt, outbound order, or
  each-pick order.
- The unified run uses shared resource scheduling by `ResourceId`; this changes
  customer-visible timing from the legacy child-flow aggregation path.
- The unified event log comes from the unified operation runner rather than
  merged inbound / outbound / each-pick child logs.
- The unified `layout.resources` and `position_timeline` reflect the current
  coarse unified operation resources, not the legacy multi-stage dock /
  forklift / worker / station trace.
- The current `position_timeline` remains baseline layout positions, NOT
  simulated movement.

## Risks before default switch

1. `compare-files` remains legacy. If only `export-artifact` switches default,
   A/B comparison and run artifact generation would use different runner
   semantics.
2. `render-report` consumes artifacts and does not know whether an artifact came
   from the legacy or unified runner. Because RunArtifact v1 is frozen, we
   cannot casually add a `runner_mode` field to the artifact.
3. The position timeline remains baseline layout positions, NOT simulated
   movement. It must not be interpreted as a real movement trace or used to
   claim that people, forklifts, or goods are moving through a simulated path.
4. The unified artifact is currently opt-in and has not gone through tracked
   golden update review.
5. A default switch would change customer-visible KPI values and event-log
   counts, so it needs a dedicated release note / migration note.
6. Before a default switch, the team must decide whether and how to update
   tracked golden artifacts.

## Decision

Do not switch default export-artifact yet.

Not ready for default switch in this PR.

The opt-in unified artifact is valid RunArtifact v1, but the differences are
large enough to require explicit product and contract handoff review before
the default customer-facing artifact path changes.

## Next steps

- CORE-U3d-1: align `compare-files` runner mode.
- CORE-U3d-2: add report-visible runner provenance without changing
  RunArtifact v1 schema, if possible.
- CORE-U3d-3: approve golden update policy.
- CORE-U3d-4-plan: merge default switch release / migration / rollback plan.
- GOLDEN-U3d-default-unified-runner: dedicated default switch and golden update PR.
- CORE-U4: reconcile unified intervals with position timeline semantics.
