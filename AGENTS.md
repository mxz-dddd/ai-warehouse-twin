# AI Warehouse Twin Engineering Rules

This repository is a monorepo for an AI warehouse digital twin simulation platform. Follow these rules on every task.

## Core Boundaries

- `src/Sim.Core` is pure C# and must never depend on `UnityEngine`.
- The DES simulation core must run in C# / in-engine runtime.
- Python is only for coarse-grained optimization and calibration. Python must not enter the per-event real-time simulation loop.
- `packages/contracts` is the single source of truth for domain objects and event contracts.
- Presentation code is allowed only under `engine/*` and must not leak into `src/Sim.Core`.

## Safety Rules

- LLM/AI components must not directly modify inventory or control physical equipment.
- Every simulation run must use an explicit seed, and results must be reproducible.
- WMS writes and equipment control require explicit approval, auditability, and idempotency.

## Engineering Rules

- Every bug fix must include a regression test.
- Each task must modify only files related to that task.
- Do not expand scope beyond the current task card.
