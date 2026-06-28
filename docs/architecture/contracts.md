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

## RunArtifact Handoff

`src/Sim.Contracts/Artifacts/RunArtifact.cs` is the handwritten C#
handoff contract for exported simulation runs. It is intentionally separate
from the generated `packages/contracts` schemas in the current phase.

RunArtifact v1 now includes:

- `layout.resources`: a deterministic default layout for resources observed
  in the real resource lease trace. Resources are sorted by `resource_id`;
  the baseline position is `x = N`, `y = 0`.
- `position_timeline`: start/finish position entries derived from the real
  resource lease timeline. This is a deterministic handoff baseline, not a
  full warehouse map, path model, or Unity visualization implementation.

## Current Limits

The generator is intentionally minimal for Phase 0. It supports basic JSON Schema type mapping only. Nested objects currently map to `object` in C# and `dict[str, Any]` in Python.
