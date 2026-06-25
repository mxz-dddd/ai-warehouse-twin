# Resource Pools

Warehouse simulation needs finite resources because workers, forklifts, docks, stations, and conveyors cannot serve unlimited tasks at the same time. Resource pools model that contention without implementing any warehouse business process yet.

## Supported Resource Types

- Worker
- Forklift
- Dock
- Station
- Conveyor

## Acquire Semantics

`TryAcquire` attempts to lease an available resource immediately. If the pool is fully busy, it returns `null` and does not join the waiting queue.

`AcquireOrQueue` also attempts immediate acquisition. If no resource is available, it appends the request to a FIFO waiting queue and returns `null`.

## Release Semantics

`Release` frees a busy lease and records the busy duration. If the waiting queue is non-empty, the released resource is immediately assigned to the oldest waiting request and a new lease is returned. Otherwise, the resource returns to the available set.

## Snapshot

`ResourcePoolSnapshot` currently provides minimal utilization statistics: capacity, available count, busy count, waiting count, total busy time, and utilization. Utilization is calculated from total busy time over `Capacity * nowMs`.

This stage has no inbound, outbound, picking, or other business workflow. `EPIC-SIM-001C` will connect resource pools to warehouse processes.
