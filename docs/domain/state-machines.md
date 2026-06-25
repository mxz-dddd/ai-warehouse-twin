# State Machines

Phase 0 currently implements the inventory status state machine in `src/Sim.Core`.

## Inventory Status Transitions

| From | To |
| --- | --- |
| Expected | Received |
| Received | QcHold |
| Received | Available |
| QcHold | Available |
| Available | Allocated |
| Allocated | Picking |
| Picking | Picked |
| Picked | Consolidating |
| Consolidating | Staged |
| Staged | Loaded |
| Loaded | Shipped |

Same-status transitions are not allowed. Illegal transitions throw `DomainRuleViolationException` when enforced through `EnsureCanTransition`.

This state machine is the foundation for later DES event validation and WMS event validation.
