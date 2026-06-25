# Contracts

`packages/contracts` is the single source of truth for domain objects and event contracts. Simulation, optimization, calibration, and integration code must consume generated types from these schemas instead of redefining contract shapes by hand.

The C# and Python contract types are generated because duplicate handwritten models drift quickly. Schema-first generation keeps both runtimes aligned with one reviewed source.

## Generate Contracts

```bash
./scripts/gen-contracts.sh
```

## Check Contract Drift

```bash
./scripts/check-contract-drift.sh
```

The drift check regenerates contract outputs and fails if tracked generated files differ from the committed versions.

## Current Schema Scope

- `domain`
- `events`
- `optimization`
- `calibration`

## Current Limits

The generator is intentionally minimal for Phase 0. It supports basic JSON Schema type mapping only. Nested objects currently map to `object` in C# and `dict[str, Any]` in Python.
