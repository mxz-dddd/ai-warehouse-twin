# AI Warehouse Twin Engineering Rules

This repository is a monorepo for AI Warehouse Twin, a startup product for real warehouse customers. The product goal is to let customers input real warehouse data and receive credible, visual, confidence-scored evaluation and optimization guidance. Follow these rules on every task.

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
- Clearly separate implemented capabilities from planned capabilities such as real path movement, Unity visualization, calibration confidence, WMS pilots, and closed-loop optimization recommendations.
- Until R2 movement semantics are implemented and reviewed, `position_timeline` must only be described as operation handoff points at baseline layout positions, NOT simulated movement. Do not build or approve movement animation, route claims, distance claims, or path-optimization claims from the current v1 position timeline. See `docs/architecture/position-timeline-semantics.md`.
- Do not claim calibration, confidence grading, error intervals, or closed-loop optimization recommendations as implemented until the corresponding R5/R7 tasks land with tests, artifacts, and CI guards.

## Engineering Rules

- Every bug fix must include a regression test.
- Each task must modify only files related to that task.
- Do not expand scope beyond the current task card.
- Contracts v1 are frozen product handoff boundaries after FIX-004; see `docs/architecture/contracts-v1-freeze.md`. Any contract change must use a dedicated `CONTRACT-` PR, justify the customer-product need, bump version when compatibility changes, regenerate contracts, run drift checks, and explicitly review artifact golden diffs.
- Generated contracts must not be edited casually or inside unrelated feature PRs.
- Artifact golden files are customer-facing regression baselines and must not change unless the task explicitly allows it.
- This repository is proprietary. Do not add open-source license text, reuse third-party code, or copy external assets unless the license and commercial-use rights are explicitly reviewed.
- Do not mark the project as open source in README/docs/issues/PRs.
- Public visibility does not grant usage rights; preserve LICENSE and NOTICE wording unless a dedicated legal/licensing task changes it.

## Golden update governance

Tracked golden artifacts must not be updated casually. Any PR that changes tracked golden files must follow `docs/architecture/golden-update-policy.md`. Golden updates require explicit diff evidence, customer-visible impact notes, regeneration commands, and reviewer approval. Do not update golden files just to make tests pass.
