# Unified Default Runner Migration Note

## Summary

AI Warehouse Twin now uses the unified warehouse runner as the default
customer-facing simulation path for CLI run output, RunArtifact export, and
ComparisonArtifact export.

This is a dedicated `GOLDEN-` switch. It updates tracked golden artifacts to
the unified runner baseline while preserving explicit legacy fallback commands.
It does not change RunArtifact v1, ComparisonArtifact v1, Sim.Contracts,
generated contracts, ingestion, Unity, or report renderer code.

## What changed

The following commands now use unified runner semantics by default:

```bash
dotnet run --project src/Sim.Cli -- sample-small-warehouse
dotnet run --project src/Sim.Cli -- run-file <scenario-json-path>
dotnet run --project src/Sim.Cli -- export-artifact <scenario-json-path> -o <output-json-path>
dotnet run --project src/Sim.Cli -- compare-files <baseline-scenario-json-path> <candidate-scenario-json-path> -o <output-json-path>
```

The unified runner uses the internal `WarehouseScenarioToUnifiedScenarioAdapter`
and unified operation runner path. It applies shared resource scheduling and
shared inventory semantics across inbound, outbound, and each-pick operations.

The following tracked golden files were regenerated from the new default:

- `datasets/sample-small-warehouse/artifacts/run-artifact.v1.json`
- `datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json`
- `datasets/sample-small-warehouse/artifacts/customer-report.v1.md`
- `datasets/sample-small-warehouse/artifacts/run-artifact.v1.report.md`
- `datasets/multi-order-warehouse/artifacts/run-artifact.v1.json`

## Customer-visible impact

Unified default results can differ from legacy default results because the
runner now schedules work through shared resource and inventory baselines.
Customer-visible differences may include:

- KPI timing, including `finished_at_ms` and `total_duration_ms`.
- Throughput metrics derived from the updated simulated run window.
- Event log order and event log line count.
- RunArtifact `layout.resources` and `position_timeline` entries.
- ComparisonArtifact baseline / candidate metrics and deltas.
- Customer Markdown report KPI values generated from the updated artifacts.

For `datasets/sample-small-warehouse/scenario.json`, the baseline change is:

| Metric | Legacy fallback | Unified default |
|---|---:|---:|
| `finished_at_ms` | 220 | 410 |
| `final_world_time_ms` | 220 | 410 |
| `kpi_summary.total_duration_ms` | 210 | 400 |
| `kpi_summary.event_log_line_count` | 10 | 13 |
| `layout.resources` | dock-1, forklift-1, station-1, worker-1 | dock-1, station-1 |
| `position_timeline.count` | 12 | 6 |
| `event_log.count` | 10 | 13 |

The current RunArtifact v1 `layout.resources` and `position_timeline` remain
baseline layout positions, NOT simulated movement. They must not be marketed,
animated, or explained as real people / forklift / goods movement until R2 real
path movement semantics land.

## How to reproduce legacy results

Use explicit legacy runner flags:

```bash
dotnet run --project src/Sim.Cli -- sample-small-warehouse --runner legacy
dotnet run --project src/Sim.Cli -- run-file <scenario-json-path> --runner legacy
dotnet run --project src/Sim.Cli -- export-artifact <scenario-json-path> -o <output-json-path> --runner legacy
dotnet run --project src/Sim.Cli -- compare-files <baseline-scenario-json-path> <candidate-scenario-json-path> -o <output-json-path> --runner legacy
```

Legacy fallback remains available for reproducing pre-switch results, customer
support comparisons, and controlled rollback checks. Legacy fallback artifacts
are valid v1 artifacts, but they no longer match the tracked default golden
files after this switch.

## How to generate unified results explicitly

Use explicit unified runner flags when a script or handoff needs to make runner
provenance obvious at the command line:

```bash
dotnet run --project src/Sim.Cli -- sample-small-warehouse --runner unified
dotnet run --project src/Sim.Cli -- run-file <scenario-json-path> --runner unified
dotnet run --project src/Sim.Cli -- export-artifact <scenario-json-path> -o <output-json-path> --runner unified
dotnet run --project src/Sim.Cli -- compare-files <baseline-scenario-json-path> <candidate-scenario-json-path> -o <output-json-path> --runner unified
```

After this switch, default output and explicit `--runner unified` output are
expected to be byte-identical for generated artifacts.

## Artifact and report provenance

RunArtifact v1 and ComparisonArtifact v1 do not contain a `runner_mode` field.
Runner provenance is therefore not embedded in artifact schemas.

For report review, render customer reports with operator-provided provenance
flags:

```bash
dotnet run --project src/Sim.Cli -- render-report \
  <run-artifact-json-path> \
  <comparison-artifact-json-path> \
  -o <report-md-path> \
  --run-runner unified \
  --comparison-runner unified
```

Default `render-report` remains a consumer-only command. It reads existing
artifacts and does not choose or run a simulation runner.

## Position timeline honesty

The default unified RunArtifact continues to use the existing v1 handoff fields.
Current `position_timeline` entries reflect deterministic baseline layout
coordinates rather than simulated travel paths.

Until R2 real path movement is implemented, describe these coordinates as
baseline layout positions, NOT simulated movement.

## Rollback

Rollback is intentionally simple: revert this `GOLDEN-` PR.

Reverting this PR restores legacy defaults and the previous tracked golden
artifacts. The explicit unified runner flags should remain available unless a
separate investigation shows they are the direct cause of a regression.

Do not change schema during rollback. If a contract or schema issue is found,
handle it through a separate `CONTRACT-` governance task.
