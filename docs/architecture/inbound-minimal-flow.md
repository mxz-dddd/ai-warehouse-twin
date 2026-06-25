# Inbound Minimal Flow

The current inbound minimal flow connects the DES kernel, resource pools, inventory state machine, event log, and `WorldState`.

## Flow

```text
receipt arrives -> acquire dock -> unload completed -> staging -> acquire forklift -> putaway completed -> AVAILABLE
```

The flow is advanced by DES events:

- `InboundReceiptArrivedEvent`
- `InboundUnloadCompletedEvent`
- `InboundPutawayCompletedEvent`

Event IDs are deterministic and derived from the receipt id. The flow does not use GUIDs, system time, or unscoped randomness.

## Resource Queues

Dock and forklift resources are finite `ResourcePool` instances. If a dock is unavailable, the receipt waits in the dock FIFO queue. If a forklift is unavailable after unloading, the receipt waits in the forklift FIFO queue. `Release` immediately assigns the released resource to the oldest waiting request.

## Inventory Status

Inbound inventory starts as `Expected`, transitions to `Received` after unloading, and transitions to `Available` after putaway. These transitions are enforced through `InventoryStateMachine`.

## WorldState

Each receipt is represented as `receipt:{ReceiptId}` in `WorldState`. Coordinates are fixed placeholders for Phase 1 and are only intended to make status snapshots visible to future presentation code.

## Current Limits

There is no pathing, no A*, no outbound flow, no real layout integration, no file import, and no full KPI report. `EPIC-SIM-001D` will extend the simulation toward outbound flow.
