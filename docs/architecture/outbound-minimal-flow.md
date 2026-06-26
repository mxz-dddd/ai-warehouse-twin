# Outbound Minimal Flow

The current outbound minimal flow connects the DES kernel, resource pools, inventory state machine, event log, and `WorldState` for full-case orders.

## Flow

```text
order released -> allocate AVAILABLE inventory -> acquire worker -> pick completed -> staged -> acquire dock -> load completed -> SHIPPED
```

The flow is advanced by DES events:

- `OutboundOrderReleasedEvent`
- `OutboundPickCompletedEvent`
- `OutboundLoadCompletedEvent`

Event IDs are deterministic and derived from the order id. The flow does not use GUIDs, system time, or unscoped randomness.

## Resource Queues

Worker and dock resources are finite `ResourcePool` instances. If a worker is unavailable at release, the order waits in the worker FIFO queue. If a dock is unavailable after picking, the order waits in the dock FIFO queue. `Release` immediately assigns the released resource to the oldest waiting request.

## Inventory Status

Outbound inventory starts as `Available`, then transitions through `Allocated`, `Picking`, `Picked`, `Staged`, `Loaded`, and `Shipped`. These transitions are enforced through `InventoryStateMachine`.

## WorldState

Each order is represented as `order:{OrderId}` in `WorldState`. Coordinates are fixed placeholders for Phase 1 and are only intended to make status snapshots visible to future presentation code.

## Current Limits

There is no each-pick flow, no tote handling, no conveyor, no merge barrier, no review, no packing, no pathing, no A*, no real layout integration, no file import, and no complex KPI report. `EPIC-SIM-001E` will extend order fulfillment beyond this minimal full-case flow.
