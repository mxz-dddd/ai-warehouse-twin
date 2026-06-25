# AI Warehouse Twin

Warehouse digital twin + discrete event simulation + optimization + customer presentation.

## Current Phase

Phase 0 bootstrap.

## Phase 0 Current Capabilities

- Contract schemas and generated C# / Python contract types.
- Contract drift check.
- `Sim.Core` .NET project.
- Inventory state machine.
- Domain invariants.
- xUnit tests.
- DES kernel skeleton.
- Deterministic RNG.
- Stable event queue.
- Minimal simulator.
- `WorldState` snapshot.
- Resource pools.
- FIFO waiting queues.
- Busy time tracking.
- Minimal utilization snapshot.
- Inbound minimal flow.
- Dock queue.
- Forklift queue.
- Deterministic inbound run result.

## Technical Boundaries

- C# `Sim.Core`: deterministic DES simulation core.
- Unity Presentation: customer-facing presentation code under `engine/*`.
- Python Optimization & Calibration: coarse-grained optimization and calibration services only.
- Contracts single source of truth: `packages/contracts`.

## Not In Scope Yet

- 3D implementation.
- WMS write-back.
- AI assistant implementation.
- Real equipment control.
- Complete simulation flow.
