# Contracts v1 freeze

AI Warehouse Twin treats contracts as startup product handoff boundaries between simulation core, reporting, validation, visualization, calibration, optimization, and WMS integration.

## Freeze status

Status: contracts v1 frozen after the R0 FIX-004 merge.

The freeze covers the current reviewed v1 handoff surfaces, including:

- RunArtifact v1
- ComparisonArtifact v1
- packages/contracts generated outputs
- tracked sample artifact golden files

This freeze does not mean the product is feature-complete. It means the current v1 contract surface is a protected compatibility boundary.

Future movement artifacts or RunArtifact movement fields must follow the
`CONTRACT-` governance described here; see
`docs/architecture/r2-movement-contract-options.md`.

A proposal-level MovementArtifact v1 outline is documented in
`docs/architecture/movement-artifact-v1-proposal.md`; it remains non-binding
until a future CONTRACT- PR updates schemas/contracts.

## Why this matters

Contracts are the boundary that lets:

- Sim.Core produce deterministic simulation facts
- Sim.Cli export artifacts
- Sim.Report render customer-facing reports
- Sim.Validation validate customer inputs
- future visualization consume artifacts without touching Sim.Core
- future calibration / optimization / WMS services integrate safely

## Rules for changing contracts

Any contract change must:

1. Use a dedicated `CONTRACT-` task / PR.
2. Explain the customer-product reason for the change.
3. Separate implemented capability from planned capability.
4. Bump schema/version when compatibility changes.
5. Regenerate generated contracts.
6. Run contract drift check.
7. Update sample artifacts only when explicitly required.
8. Show artifact golden diffs explicitly in PR.
9. Receive explicit review from the core/product track and visualization/handoff track.
10. Preserve deterministic reproducibility.

Current CODEOWNERS uses the existing GitHub owner `@mxz-dddd`. When the visualization / handoff track member is added to GitHub, contract-related CODEOWNERS entries must add that owner as an explicit reviewer so the dual-review rule becomes enforceable in GitHub.

## What is not allowed

Do not:

- casually edit generated contract outputs
- change artifact JSON shape inside unrelated feature PRs
- modify golden artifacts without an explicit task card
- claim calibration / optimization / WMS capabilities as implemented through schema alone
- build customer-facing visualizations on fields whose semantics are marked planned or baseline-only

## Post-merge freeze tag

After this PR is merged, create an annotated tag on the merge commit:

`contracts-v1-freeze`

The tag message should state:

`Freeze contracts v1 handoff boundaries after CODEOWNERS and contract governance setup.`

Do not create the tag on a feature branch.
