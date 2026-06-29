# Unified runner audit

## Product context

AI Warehouse Twin is a startup product for real warehouse customers. R1 exists because product credibility depends on a single timeline with real shared resources, shared inventory, and queueing delays across inbound, outbound, and each-pick work.

This audit is a baseline for CORE-U2 / CORE-U3 and the later dedicated
`GOLDEN-U3d-default-unified-runner` switch. It documents current implementation
state without changing contracts, schemas, generated contracts, ingestion,
Unity, or customer-facing movement claims.

## Current implementation inventory

- `WarehouseScenarioRunner`
  - `Run(WarehouseScenario)` uses the traditional child-flow aggregation path.
  - `RunWithTrace(WarehouseScenario)` also uses the traditional child-flow aggregation path, with a shared trace collector for lease recording.
  - `RunUnified(WarehouseUnifiedScenario)` is an explicit unified path and is not the default `WarehouseScenario` path.
  - `RunWithUnifiedAdapter(WarehouseScenario)` was added in CORE-U3a as an explicit opt-in path that converts `WarehouseScenario` through `WarehouseScenarioToUnifiedScenarioAdapter` and then calls `RunUnified(...)`.
  - `Run(...)` remains available as the traditional legacy path.
  - `RunWithUnifiedAdapter(...)` is now the customer-facing default path for
    CLI run output, export-artifact, and compare-files.
- `WarehouseUnifiedOperationRunner`
  - Exists under `src/Sim.Core/Scenarios/Unified`.
  - Runs `WarehouseUnifiedOperation` records on capacity-one resource timelines grouped by `ResourceId`.
  - Applies a shared `WarehouseInventoryLedger`.
  - Produces operation intervals, operation telemetry, grouped customer KPI summaries, resource KPI summaries, bottleneck summary, position timeline, final inventory snapshot, and a deterministic event log.
- `WarehouseScenarioToUnifiedScenarioAdapter`
  - Added in CORE-U2 as an internal adapter from `WarehouseScenario` to `WarehouseUnifiedScenario`.
  - Maps each inbound receipt, outbound order, and each-pick order to one coarse unified operation.
  - Preserves scenario id, seed, stable operation ids, stable resource ids, deterministic ordering, and available starting inventory by SKU.
  - Does not switch the default CLI / artifact / comparison paths.
- `SharedResource` / shared resource timeline
  - There is no class named exactly `SharedResource`.
  - The current shared resource baseline is `WarehouseSharedResourceTimelineRunner`, `WarehouseSharedResourceWorkItem`, `WarehouseSharedResourceAllocation`, and `WarehouseSharedResourceTimelineResult`.
  - `WarehouseSharedResourceTimelineRunner.RunCapacityOne(...)` serializes work items for one resource id and records queueing behavior.
- `ResourceLease`
  - Exists under `src/Sim.Core/Resources`.
  - `ResourcePool` grants or queues `ResourceRequest` objects and returns `ResourceLease` records.
  - `ResourceLeaseTraceCollector` records lease timeline entries for the traditional runner trace path.
- Inventory types
  - Traditional inbound / outbound / each-pick states each own their own inventory representation.
  - `WarehouseInventoryLedger` exists and is used by `WarehouseUnifiedOperationRunner`.

## Current CLI / artifact path

- `sample-small-warehouse` CLI
  - Uses `new WarehouseScenarioRunner().RunWithUnifiedAdapter(scenario)` by default.
  - Current default status: unified adapter path backed by `WarehouseUnifiedOperationRunner`.
  - `--runner legacy` remains available to call the traditional child-flow aggregation path.
  - `--runner unified` explicitly matches the default.
- `run-file` CLI
  - Loads `WarehouseScenario` from JSON and uses `new WarehouseScenarioRunner().RunWithUnifiedAdapter(scenario)` by default.
  - Current default status: unified adapter path backed by `WarehouseUnifiedOperationRunner`.
  - `--runner legacy` remains available to call the traditional child-flow aggregation path.
  - `--runner unified` explicitly matches the default.
- `export-artifact`
  - Loads `WarehouseScenario` from JSON and generates RunArtifact v1 from the unified adapter path by default.
  - Current default status: unified adapter path backed by `WarehouseUnifiedOperationRunner`.
  - `--runner legacy` remains available to generate the traditional child-flow aggregation artifact with `ResourceLeaseTraceCollector`.
  - `--runner unified` explicitly matches the default.
- `compare-files`
  - Uses `WarehouseScenarioComparisonRunner.CompareWithUnifiedAdapter(...)` by default.
  - Current default status: unified adapter path backed by `WarehouseUnifiedOperationRunner`.
  - `--runner legacy` remains available to compare baseline and candidate with the traditional runner.
  - `--runner unified` explicitly matches the default.
- `render-report`
  - Consumes existing RunArtifact / ComparisonArtifact files through `Sim.Report`.
  - It does not run simulation and does not choose a runner.
  - CORE-U3d-2 added optional operator-provided runner provenance flags for report visibility.

## Shared inventory status

Partially implemented.

`WarehouseUnifiedOperationRunner` uses a single `WarehouseInventoryLedger` and produces `FinalInventorySnapshot`. Existing tests cover conservation and non-negative inventory behavior on that unified path.

The customer-facing default sample CLI, run-file CLI, export-artifact path, and
compare-files path now use the unified adapter path and unified ledger.

The traditional `WarehouseScenarioRunner.Run(...)` and `RunWithTrace(...)`
methods remain as explicit legacy fallback internals. They still run inbound,
outbound, and each-pick child scenarios separately and merge child results.

## Shared resource competition status

Partially implemented.

`WarehouseUnifiedOperationRunner` schedules operations by `ResourceId` through `WarehouseSharedResourceTimelineRunner.RunCapacityOne(...)`. Existing tests cover capacity-one queueing, non-overlapping allocations, deterministic resource timelines, and operation waiting time on the unified path.

The customer-facing default sample CLI, run-file CLI, export-artifact path, and
compare-files path now use unified resource scheduling through the adapter path.

The traditional `WarehouseScenarioRunner.Run(...)` and `RunWithTrace(...)`
methods still construct separate resource pools inside `InboundScenarioRunner`,
`OutboundScenarioRunner`, and `EachPickScenarioRunner`. Those child flows can
record resource leases into one trace collector, but they remain legacy fallback
semantics.

## Single timeline status

Partially implemented.

`WarehouseUnifiedOperationRunner` creates a deterministic unified event log from shared resource events and inventory mutation events, ordered by time and deterministic tie-breakers.

The customer-facing default sample CLI, run-file CLI, export-artifact path, and
compare-files path now use the unified event log produced from shared resource
events and inventory mutation events.

The traditional `WarehouseScenarioRunner.Run(...)` path remains available for
legacy fallback and still runs child flows separately and merges their event log
lines with flow prefixes.

## RunArtifact connection status

Partially implemented for the unified runner.

Default `export-artifact` now converts the scenario through
`WarehouseScenarioToUnifiedScenarioAdapter`, runs the unified path, and writes
RunArtifact v1 without changing the schema.

`export-artifact --runner unified` matches the default byte-for-byte.
`export-artifact --runner legacy` remains available for pre-switch reproduction
and support investigations.

Default `compare-files` now uses the unified adapter path for both baseline and
candidate. `compare-files --runner unified` matches the default, and
`compare-files --runner legacy` remains available for pre-switch reproduction.

`render-report` remains a report consumer only and does not choose a simulation
runner.

The current `position_timeline` remains baseline layout positions, NOT simulated movement.
See `docs/architecture/position-timeline-semantics.md` for the current
RunArtifact position timeline honesty boundary and R2 movement requirements.

## Characterization tests added

CORE-U2 added `WarehouseScenarioToUnifiedScenarioAdapterTests`.

Existing characterization coverage already documents the current baseline:

- `WarehouseScenarioRunnerUnifiedTests.WarehouseScenarioRunner_UnifiedPath_UsesSharedResourceAndInventory`
  - Freezes that `RunUnified(...)` can use shared resource scheduling and shared inventory.
- `WarehouseScenarioRunnerUnifiedTests.WarehouseScenarioRunner_LegacySample_RemainsStable`
  - Freezes that the default sample `Run(...)` remains the traditional behavior and has an empty `FinalInventorySnapshot`.
- `WarehouseSharedResourceTests`
  - Freezes capacity-one resource queueing and deterministic timeline behavior.
- `WarehouseUnifiedOperationRunnerTests`
  - Freezes unified operation intervals, telemetry, shared ledger behavior, KPI summaries, resource KPI, bottleneck summary, and position timeline determinism.
- `WarehouseScenarioTraceTests.Run_DoesNotChangeTraditionalBehavior`
  - Freezes that `RunWithTrace(...)` does not change traditional `Run(...)` behavior.
- `WarehouseScenarioToUnifiedScenarioAdapterTests`
  - Freezes sample conversion, deterministic adapter output, stable resource ids, unified runner compatibility, and the fact that the default runner remained unchanged at CORE-U2 time.
- `WarehouseScenarioRunnerUnifiedTests` CORE-U3a coverage
  - Freezes that `RunWithUnifiedAdapter(...)` can run sample-small-warehouse, matches legacy core counts and quantities, produces a final inventory snapshot, left default `Run(...)` unchanged at CORE-U3a time, leaves `RunWithTrace(...)` on the legacy trace path, and documents the current coarse operation mapping gap.

## R1 gap list

- Internal legacy `WarehouseScenarioRunner.Run(...)` does not use `WarehouseUnifiedOperationRunner`.
- Internal legacy `RunWithTrace(...)` does not use `WarehouseUnifiedOperationRunner`.
- Traditional inbound / outbound / each-pick states still own separate inventory models.
- Traditional inbound / outbound / each-pick runners still construct separate process-local resource pools.
- RunArtifact position timeline is unified baseline layout handoff, not real movement.
- There is still no R2 real path movement model.

## Historical task split

The following bullets preserve the incremental R1 migration history. Some
entries describe the state at the time of that task and are superseded by
`GOLDEN-U3d-default-unified-runner`.

- CORE-U2: Added an internal `WarehouseScenario` → `WarehouseUnifiedScenario` adapter with characterization tests.
  - CLI and RunArtifact output remain unchanged.
  - The adapter currently uses one coarse unified operation per inbound receipt, outbound order, or each-pick order.
  - Multi-stage legacy details such as separate dock / forklift / worker / station leases are not yet 1:1 mapped into unified operations.
- CORE-U3: Wire `WarehouseScenarioRunner` behind an explicitly tested internal unified path.
- CORE-U3a: Added `RunWithUnifiedAdapter(...)` as an explicit opt-in unified path behind `WarehouseScenarioRunner`.
  - At CORE-U3a time, default `Run(...)` remained legacy.
  - At CORE-U3a time, `RunWithTrace(...)` / `export-artifact` remained legacy.
  - At CORE-U3a time, CLI / RunArtifact / compare-files remained unchanged.
  - CORE-U3b followed before any default runner switch.
- CORE-U3b: Added CLI opt-in runner mode for sample-small-warehouse and run-file.
  - At CORE-U3b time, default CLI runner remained legacy.
  - `sample-small-warehouse --runner unified` and `run-file <scenario> --runner unified` call the explicit unified adapter path.
  - At CORE-U3b time, `export-artifact` remained legacy.
  - At CORE-U3b time, `compare-files` remained legacy.
  - At CORE-U3b time, RunArtifact / ComparisonArtifact golden files remained unchanged.
  - CORE-U3c followed with export-artifact opt-in unified runner support; default artifact switching remained out of scope.
- CORE-U3c: Added export-artifact opt-in runner mode.
  - At CORE-U3c time, default export-artifact remained legacy.
  - `export-artifact --runner unified` can generate RunArtifact v1 from the explicit unified adapter path.
  - At CORE-U3c time, RunArtifact schema and tracked golden files remained unchanged.
  - At CORE-U3c time, `compare-files` remained legacy.
  - `render-report` remained a report consumer only.
  - CORE-U3d followed before default artifact generation switched to unified.
- CORE-U3d-readiness: Added artifact switch readiness audit.
  - Compared legacy default export-artifact with opt-in unified export-artifact.
  - Did not switch default export-artifact.
  - Did not update RunArtifact schema or tracked golden files.
  - Recommended whether the later default switch was safe.
- CORE-U3d-1: Added compare-files opt-in runner mode.
  - At CORE-U3d-1 time, default compare-files remained legacy.
  - `compare-files --runner unified` compares baseline and candidate with the explicit unified adapter path.
  - ComparisonArtifact schema and tracked golden files remained unchanged.
  - This prevents export-artifact and compare-files from diverging when both are explicitly run in unified mode.
- CORE-U3d-2: Added report-visible runner provenance.
  - `render-report` can display operator-provided run/comparison runner modes.
  - RunArtifact v1 and ComparisonArtifact v1 schemas remain unchanged.
  - Default render-report output remains unchanged.
  - Mixed runner modes are shown with a warning.
- CORE-U3d-3: Added golden update policy.
  - No tracked golden files were updated in CORE-U3d-3.
  - At CORE-U3d-3 time, default legacy artifacts remained the customer-facing baseline.
  - Future unified default switch required a dedicated GOLDEN PR.
  - RunArtifact / ComparisonArtifact / customer report golden changes must include diff evidence and customer impact notes.
- CORE-U3d-4-plan: Added default runner switch release / migration plan.
  - No default runner switch was performed in CORE-U3d-4-plan.
  - No tracked golden files were updated in CORE-U3d-4-plan.
  - The future default switch required a dedicated GOLDEN PR.
  - Release note, migration note, and rollback requirements are documented.
- GOLDEN-U3d-default-unified-runner: Switches customer-facing defaults to unified.
  - `sample-small-warehouse`, `run-file`, `export-artifact`, and `compare-files` default to unified semantics.
  - `--runner unified` matches the default.
  - `--runner legacy` remains available for pre-switch reproduction and rollback investigations.
  - RunArtifact v1 and ComparisonArtifact v1 schemas remain unchanged.
  - Tracked golden artifacts are regenerated in the dedicated GOLDEN PR.
  - The release note documents customer impact and rollback.
- CORE-U3d: Make the unified path the single authority only after parity and gap tests are green.
  - Preserve RunArtifact schema and golden files unless a dedicated artifact task authorizes updates.
  - Ensure `run-file`, `export-artifact`, and `compare-files` cannot silently diverge.
- CORE-U4: Reconcile resource lease trace / position timeline generation with unified operation intervals.
  - Keep the honesty label: baseline layout positions, NOT simulated movement.
  - Do not claim real movement until R2 movement-driven RunArtifact exists.
  - CORE-U4a adds the position timeline semantics audit in
    `docs/architecture/position-timeline-semantics.md`.
- CORE-U4b: Added position timeline semantics guard tests.
  - Guards RunArtifact v1 `position_timeline` as operation/resource handoff at baseline layout positions.
  - Guards customer report wording against movement/path/route claims.
  - Does not change RunArtifact schema, golden artifacts, runner behavior, report renderer, Unity, or ingestion.
- CORE-U4c: Added R2 movement contract/options planning.
  - Compares separate MovementArtifact, RunArtifact schema bump, and internal-only options.
  - Recommends separate MovementArtifact planning path, subject to future CONTRACT- approval.
  - Does not change contracts, schema, golden artifacts, runner behavior, Unity, or ingestion.
- CONTRACT-R2a: Added proposal-level MovementArtifact v1 field outline.
  - Keeps RunArtifact v1 unchanged.
  - Does not implement movement/path/route.
  - Requires future CONTRACT- approval before schema or generated contract changes.

## Boundaries

This audit does not change contracts, schema versions, generated contracts, CI,
reports, validation, license files, ingestion, Unity, or customer-facing
movement claims.
