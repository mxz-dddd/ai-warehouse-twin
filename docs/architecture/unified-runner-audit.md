# Unified runner audit

## Product context

AI Warehouse Twin is a startup product for real warehouse customers. R1 exists because product credibility depends on a single timeline with real shared resources, shared inventory, and queueing delays across inbound, outbound, and each-pick work.

This audit is a baseline for CORE-U2 / CORE-U3. It documents the current implementation without changing contracts, artifacts, CLI behavior, golden files, or customer-facing claims.

## Current implementation inventory

- `WarehouseScenarioRunner`
  - `Run(WarehouseScenario)` uses the traditional child-flow aggregation path.
  - `RunWithTrace(WarehouseScenario)` also uses the traditional child-flow aggregation path, with a shared trace collector for lease recording.
  - `RunUnified(WarehouseUnifiedScenario)` is an explicit unified path and is not the default `WarehouseScenario` path.
  - `RunWithUnifiedAdapter(WarehouseScenario)` was added in CORE-U3a as an explicit opt-in path that converts `WarehouseScenario` through `WarehouseScenarioToUnifiedScenarioAdapter` and then calls `RunUnified(...)`.
  - Default `Run(...)` remains legacy.
  - `RunWithTrace(...)` / `export-artifact` remain legacy.
  - CLI / RunArtifact / compare-files remain unchanged.
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
  - Uses `new WarehouseScenarioRunner().Run(scenario)`.
  - Current status: traditional child-flow aggregation path, not `WarehouseUnifiedOperationRunner`.
- `run-file` CLI
  - Loads `WarehouseScenario` from JSON and uses `new WarehouseScenarioRunner().Run(scenario)`.
  - Current status: traditional child-flow aggregation path, not `WarehouseUnifiedOperationRunner`.
- `export-artifact`
  - Loads `WarehouseScenario` from JSON and uses `new WarehouseScenarioRunner().RunWithTrace(scenario)`.
  - Current status: traditional child-flow aggregation path with `ResourceLeaseTraceCollector`, not `WarehouseUnifiedOperationRunner`.
- `compare-files`
  - Uses `WarehouseScenarioComparisonRunner.Compare(...)`.
  - `WarehouseScenarioComparisonRunner` calls `WarehouseScenarioRunner.Run(...)` for baseline and candidate.
  - Current status: traditional child-flow aggregation path, not `WarehouseUnifiedOperationRunner`.
- `render-report`
  - Consumes existing RunArtifact / ComparisonArtifact files through `Sim.Report`.
  - It does not run simulation and does not choose a runner.

## Shared inventory status

Partially implemented.

`WarehouseUnifiedOperationRunner` uses a single `WarehouseInventoryLedger` and produces `FinalInventorySnapshot`. Existing tests cover conservation and non-negative inventory behavior on that unified path.

The default `WarehouseScenarioRunner.Run(...)`, `RunWithTrace(...)`, sample CLI, run-file CLI, export-artifact path, and compare-files path do not use the unified ledger. They still run inbound, outbound, and each-pick child scenarios separately and merge child results. In this default path, inventory is not truly shared across inbound / outbound / each-pick.

## Shared resource competition status

Partially implemented.

`WarehouseUnifiedOperationRunner` schedules operations by `ResourceId` through `WarehouseSharedResourceTimelineRunner.RunCapacityOne(...)`. Existing tests cover capacity-one queueing, non-overlapping allocations, deterministic resource timelines, and operation waiting time on the unified path.

The default `WarehouseScenarioRunner.Run(...)` and `RunWithTrace(...)` still construct separate resource pools inside `InboundScenarioRunner`, `OutboundScenarioRunner`, and `EachPickScenarioRunner`. Those child flows can record resource leases into one trace collector, but they do not compete for one shared worker / forklift / dock pool across flows. Cross-flow queueing delay is therefore not truly produced by the current customer-facing default path.

## Single timeline status

Partially implemented.

`WarehouseUnifiedOperationRunner` creates a deterministic unified event log from shared resource events and inventory mutation events, ordered by time and deterministic tie-breakers.

The default `WarehouseScenarioRunner.Run(...)` path still runs child flows separately and merges their event log lines with flow prefixes. That merge is deterministic, but it is not a single event scheduler where inbound, outbound, and each-pick events all compete on one authoritative timeline.

## RunArtifact connection status

Not implemented for the unified runner.

`export-artifact` currently uses `WarehouseScenarioRunner.RunWithTrace(...)`, which is the traditional child-flow aggregation path plus resource lease trace collection. The RunArtifact `layout.resources` and `position_timeline` fields are mapped from that trace result. They are not generated from `WarehouseUnifiedOperationRunner`.

The current `position_timeline` remains baseline layout positions, NOT simulated movement.

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
  - Freezes sample conversion, deterministic adapter output, stable resource ids, unified runner compatibility, and the fact that the default runner remains unchanged.
- `WarehouseScenarioRunnerUnifiedTests` CORE-U3a coverage
  - Freezes that `RunWithUnifiedAdapter(...)` can run sample-small-warehouse, matches legacy core counts and quantities, produces a final inventory snapshot, leaves default `Run(...)` unchanged, leaves `RunWithTrace(...)` on the legacy trace path, and documents the current coarse operation mapping gap.

## R1 gap list

- Default `WarehouseScenarioRunner.Run(...)` does not use `WarehouseUnifiedOperationRunner`.
- `RunWithTrace(...)` and `export-artifact` do not use `WarehouseUnifiedOperationRunner`.
- `compare-files` does not use `WarehouseUnifiedOperationRunner`.
- Traditional inbound / outbound / each-pick states still own separate inventory models.
- Traditional inbound / outbound / each-pick runners still construct separate process-local resource pools.
- RunArtifact position timeline is trace-backed baseline layout handoff, not unified movement or real movement.
- There is no single authoritative runner for all customer-facing warehouse runs yet.

## Recommended next task split

- CORE-U2: Added an internal `WarehouseScenario` → `WarehouseUnifiedScenario` adapter with characterization tests.
  - CLI and RunArtifact output remain unchanged.
  - The adapter currently uses one coarse unified operation per inbound receipt, outbound order, or each-pick order.
  - Multi-stage legacy details such as separate dock / forklift / worker / station leases are not yet 1:1 mapped into unified operations.
- CORE-U3: Wire `WarehouseScenarioRunner` behind an explicitly tested internal unified path.
- CORE-U3a: Added `RunWithUnifiedAdapter(...)` as an explicit opt-in unified path behind `WarehouseScenarioRunner`.
  - Default `Run(...)` remains legacy.
  - `RunWithTrace(...)` / `export-artifact` remain legacy.
  - CLI / RunArtifact / compare-files remain unchanged.
  - CORE-U3b is still required before any default runner switch.
- CORE-U3b: Make the unified path the single authority only after parity and gap tests are green.
  - Preserve RunArtifact schema and golden files unless a dedicated artifact task authorizes updates.
  - Ensure `run-file`, `export-artifact`, and `compare-files` cannot silently diverge.
- CORE-U4: Reconcile resource lease trace / position timeline generation with unified operation intervals.
  - Keep the honesty label: baseline layout positions, NOT simulated movement.
  - Do not claim real movement until R2 movement-driven RunArtifact exists.

## Boundaries

This audit does not change contracts, artifacts, CLI behavior, golden files, schema versions, generated contracts, scripts, CI, reports, validation, license files, or customer-facing claims.
