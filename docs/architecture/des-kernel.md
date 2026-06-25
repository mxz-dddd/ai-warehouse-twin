# DES Kernel

The Phase 0 DES kernel provides deterministic simulation infrastructure for later warehouse workflows.

## Responsibilities

- `SimClock` tracks simulation time in milliseconds.
- `DeterministicRng` is the only source of randomness for reproducible runs.
- `SimEventQueue` schedules events with stable `(time, sequence)` ordering.
- `Simulator` advances the clock, executes queued events, records the event log, and synchronizes `WorldState` time.
- `SimEventLog` provides deterministic text output for future golden-run checks.
- `WorldState` is an immutable-style snapshot intended for future Unity presentation reads.

## Time

`SimClock` is separate from wall-clock time. Advancing the simulation does not depend on real elapsed time.

## Event Ordering

Events are ordered by `OccursAtMs` and then by enqueue sequence. Events with the same time execute in the order they were scheduled.

## Randomness

All random behavior must use `DeterministicRng(seed)`. A simulation run must have an explicit seed so the event log can be reproduced.

## Current Limits

This stage has no warehouse business process yet. Inbound, outbound, picking, resources, and pathing are deferred to later tasks. `EPIC-SIM-001B` will connect the kernel to the first minimal warehouse flow.
