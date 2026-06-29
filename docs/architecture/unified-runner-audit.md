# Unified runner audit

## Product context

AI Warehouse Twin is a startup product for real warehouse customers. R1 exists because product credibility depends on a single timeline with real shared resources, shared inventory, and queueing delays across inbound, outbound, and each-pick work.

This audit is a baseline for CORE-U2 / CORE-U3. It documents the current implementation without changing contracts, artifacts, CLI behavior, golden files, or customer-facing claims.

## Current implementation inventory

- `WarehouseScenarioRunner`
  - `Run(WarehouseScenario)` uses the traditional child-flow aggregation path.
  - `RunWithTrace(WarehouseScenario)` also uses the traditional child-flow aggregation path, with a shared trace collector for lease recording.
  - `RunUnified(WarehouseUnifiedScenario)` is an explicit unified path and is not the default `WarehouseScenario` path.
- `WarehouseUnifiedOperationRunner`
  - Exists under `src/Sim.Core/Scenarios/Unified`.
  - Runs `WarehouseUnifiedOperation` records on capacity-one resource timelines grouped by `ResourceId`.
  - Applies a shared `WarehouseInventoryLedger`.
  - Produces operation intervals, operation telemetry, grouped customer KPI summaries, resource KPI summaries, bottleneck summary, position timeline, final inventory snapshot, and a deterministic event log.
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

No new tests were added in this PR.

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

## R1 gap list

- `WarehouseScenario` JSON input does not map into `WarehouseUnifiedScenario`.
- Default `WarehouseScenarioRunner.Run(...)` does not use `WarehouseUnifiedOperationRunner`.
- `RunWithTrace(...)` and `export-artifact` do not use `WarehouseUnifiedOperationRunner`.
- `compare-files` does not use `WarehouseUnifiedOperationRunner`.
- Traditional inbound / outbound / each-pick states still own separate inventory models.
- Traditional inbound / outbound / each-pick runners still construct separate process-local resource pools.
- RunArtifact position timeline is trace-backed baseline layout handoff, not unified movement or real movement.
- There is no single authoritative runner for all customer-facing warehouse runs yet.

## Recommended next task split

- CORE-U2: Add an internal `WarehouseScenario` → `WarehouseUnifiedScenario` adapter with characterization tests.
  - Keep CLI and RunArtifact output unchanged unless explicitly authorized.
  - Start with sample-small-warehouse parity tests for completed counts, quantities, timing, event-log status, and final inventory snapshot expectations.
  - Document any semantic mismatches between child-flow stages and unified operation granularity.
- CORE-U3: Wire `WarehouseScenarioRunner` behind an explicitly tested internal unified path.
  - Make the unified path the single authority only after parity and gap tests are green.
  - Preserve RunArtifact schema and golden files unless a dedicated artifact task authorizes updates.
  - Ensure `run-file`, `export-artifact`, and `compare-files` cannot silently diverge.
- CORE-U4: Reconcile resource lease trace / position timeline generation with unified operation intervals.
  - Keep the honesty label: baseline layout positions, NOT simulated movement.
  - Do not claim real movement until R2 movement-driven RunArtifact exists.

## Boundaries

This audit does not change contracts, artifacts, CLI behavior, golden files, schema versions, generated contracts, scripts, CI, reports, validation, license files, or customer-facing claims.
