# Medium Warehouse Dataset Draft

`medium-warehouse` is an R3 Phase-1 demo dataset draft for a medium warehouse scenario.

This is not a golden dataset. It does not contain real A/B comparison results, KPI conclusions, or generated artifacts. Golden artifacts should be generated later by A4b after A1 movement and A3 KPI work are complete.

## Scope

The draft provides:

- 30 SKU master records across high, medium, and low velocity classes.
- 80 outbound work orders split between case-pick and each-pick flows.
- 2 named forklifts: `forklift-01`, `forklift-02`.
- 3 named workers: `worker-01`, `worker-02`, `worker-03`.
- A/B warehouse zones with pick-face and reserve locations.
- Inbound dock, outbound dock, inbound staging, outbound staging, each-pick station, merge/consolidation staging, aisles, racks, and stable path nodes.
- Inbound receipts, case-pick outbound work, each-pick work, merge/consolidation pressure, and a clear peak release window.

## File Shape

`scenario.json` keeps the existing warehouse scenario input shape so the current loader can parse the supported `inbound`, `outbound`, and `each_pick` sections without loader changes.

The same file also carries draft-only metadata sections:

- `layout`
- `sku_master`
- `resources`
- `slotting_notes`
- `dataset_quality`

These metadata sections are intentionally static dataset context for later A1 movement, A3 KPI, and A5 ABC slotting work. They do not require changes to `Sim.Core`, `Sim.Cli`, contracts, generated files, or Unity.

## Slotting Intent

High velocity SKUs are deliberately not all placed in the best A-zone pick faces. `SKU-HF-007` through `SKU-HF-010` start in B-zone pick locations, leaving visible optimization room for later A5 ABC slotting work.

## Out Of Scope

This draft does not:

- Generate golden artifacts.
- Run real A/B comparison.
- State KPI conclusions.
- Implement movement logic.
- Implement ABC slotting.
- Change simulation, CLI, contracts, generated files, or Unity code.
