# Unified Artifact Switch Readiness Audit

## Status

Default switch completed by `GOLDEN-U3d-default-unified-runner`.

This audit now documents the current difference between the unified default
`export-artifact` path and the explicit legacy fallback path.

CORE-U3d-1 aligns compare-files runner mode before any default switch.
CORE-U3d-2 adds report-visible runner provenance through render-report flags.
CORE-U3d-3 adds the golden update policy required before any default runner
switch or tracked golden artifact update.
CORE-U3d-4-plan added the release / migration / rollback plan required before
the dedicated GOLDEN default-switch PR.
`GOLDEN-U3d-default-unified-runner` switches the default and updates tracked
golden artifacts.

## Scope

- Default `export-artifact` is unified.
- `export-artifact --runner unified` matches the default byte-for-byte.
- `export-artifact --runner legacy` remains available as a fallback /
  reproduction path.
- Tracked golden files are updated to the unified default.
- RunArtifact schema is unchanged.
- Default `compare-files` is unified.
- `compare-files --runner unified` matches the default byte-for-byte.
- `compare-files --runner legacy` remains available as a fallback /
  reproduction path.
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
  -o "$tmp_dir/default-unified.json"

dotnet run --project src/Sim.Cli -- export-artifact \
  datasets/sample-small-warehouse/scenario.json \
  -o "$tmp_dir/explicit-unified.json" \
  --runner unified

dotnet run --project src/Sim.Cli -- export-artifact \
  datasets/sample-small-warehouse/scenario.json \
  -o "$tmp_dir/explicit-legacy.json" \
  --runner legacy
```

## Unified artifact baseline

The default unified artifact and explicit unified artifact both match the
tracked golden file byte-for-byte:

```bash
cmp "$tmp_dir/default-unified.json" "$tmp_dir/explicit-unified.json"
cmp "$tmp_dir/default-unified.json" datasets/sample-small-warehouse/artifacts/run-artifact.v1.json
```

The unified baseline is now the customer-facing tracked golden for
`datasets/sample-small-warehouse/artifacts/run-artifact.v1.json`.

## Legacy fallback artifact summary

The explicit legacy fallback artifact is valid JSON and keeps the existing
RunArtifact v1 contract:

- `schema_version`: `run-artifact.v1`
- `artifact_kind`: `warehouse-simulation-run`
- `scenario_id`: `sample-small-warehouse`
- `seed`: `20240627`

The legacy fallback artifact is intentionally different from the unified
default artifact. It remains available for reproducing pre-switch outputs and
support investigations.

## Diff summary

Actual audit output:

| Metric | Legacy fallback | Unified default |
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

- The unified default uses the explicit `WarehouseScenarioToUnifiedScenarioAdapter`
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

## Post-switch risks

1. `render-report` consumes artifacts and does not know whether an artifact came
   from the legacy or unified runner. Because RunArtifact v1 is frozen, we
   cannot casually add a `runner_mode` field to the artifact.
2. The position timeline remains baseline layout positions, NOT simulated
   movement. It must not be interpreted as a real movement trace or used to
   claim that people, forklifts, or goods are moving through a simulated path.
3. Customer-visible KPI values and event-log counts changed in the golden PR,
   so customer reports should be interpreted with the migration note.
4. Legacy fallback artifacts remain useful for support but no longer match
   tracked golden files.

## Decision

Default export-artifact has switched to unified in the dedicated `GOLDEN-` PR.
Explicit legacy fallback remains available through `--runner legacy`.

## Next steps

- Monitor customer-visible KPI and report changes after the default switch.
- Use `--runner legacy` only for reproduction / rollback investigations.
- Keep report-visible runner provenance as operator-provided flags because
  RunArtifact v1 and ComparisonArtifact v1 do not store runner mode.
- CORE-U4: reconcile unified intervals with position timeline semantics.
