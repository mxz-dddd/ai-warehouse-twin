# AI Warehouse Twin

Warehouse digital twin + discrete event simulation + optimization + customer presentation.

## Current Phase

Phase 0 bootstrap.

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
