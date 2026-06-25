# Domain Invariants

Phase 0 currently defines the following domain invariants in `src/Sim.Core`.

- Inventory quantity cannot be negative.
- Inventory total must be conserved when a movement or transformation claims conservation.
- Location total weight must not exceed capacity.
- Location total volume must not exceed capacity.

These rules will later be included in golden scenarios and runtime simulation validation.
