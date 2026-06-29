# Golden Update Policy

## Status

Policy only. No golden files are updated in this PR.

## Scope

This policy governs tracked golden artifacts and expected report outputs used as
customer-facing regression baselines. It applies to every PR that changes a
tracked artifact JSON / Markdown file, a schema-generated output, or default
behavior that would make those files differ.

## Golden files covered

Current tracked golden files include:

- `datasets/sample-small-warehouse/artifacts/run-artifact.v1.json`
- `datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json`
- `datasets/sample-small-warehouse/artifacts/customer-report.v1.md`
- `datasets/sample-small-warehouse/artifacts/run-artifact.v1.report.md`
- `datasets/multi-order-warehouse/artifacts/run-artifact.v1.json`

If future datasets add tracked `artifacts/**` JSON / Markdown outputs, they are
covered by this policy unless a dedicated governance document says otherwise.

## What counts as a golden update

The following all count as golden updates:

- Directly modifying tracked artifact JSON / Markdown files.
- Regenerating tracked artifact JSON / Markdown files.
- Changing the default runner in a way that causes artifact golden diffs.
- Changing the report renderer in a way that causes customer report golden diffs.
- Changing schema / contracts in a way that causes generated output diffs.
- Changing ordering, rounding, timestamps, or line endings in a way that causes
  golden diffs.

## When golden updates are allowed

Golden updates are allowed only when the product meaning is clear and the PR
contains enough evidence for reviewers to understand the customer-visible
change. Examples include:

1. Product semantics intentionally change, such as a default runner switch from
   legacy to unified.
2. A schema version is explicitly upgraded and has contract review.
3. Customer report wording or structure changes for a clear product reason.
4. A known artifact or report error is fixed and before/after evidence is
   provided.
5. The change has passed a readiness audit and that audit explicitly allows the
   golden update.

## When golden updates are forbidden

Golden updates are forbidden when they hide uncertainty or mix unrelated work.
Do not update golden files:

1. Just to make CI pass.
2. Without explaining the diff.
3. Without describing customer-visible impact.
4. In a PR that also mixes unrelated code or documentation changes.
5. Without distinguishing legacy and unified runner semantics.
6. Without preserving the honesty label: baseline layout positions, NOT
   simulated movement.
7. After contracts freeze by bypassing contract governance to change schema or
   generated outputs.

## Required evidence before a golden update

Every golden update PR must provide:

- Old vs new diff summary.
- Changed metrics table.
- Changed layout resources.
- Changed position timeline count.
- Changed event log count.
- Runner mode used to generate each artifact.
- Exact commands used to regenerate artifacts.
- Reason why the change is intended.
- Customer-visible impact statement.
- Rollback plan.

## Required PR shape

Golden update PRs must be easy to review and revert:

- The PR title must include `GOLDEN-`.
- The golden update must be in a dedicated PR, not mixed into an ordinary
  feature PR.
- The PR body must include a `Golden Diff Summary` section.
- The PR body must include a `Customer Impact` section.
- The PR body must include a `Regeneration Commands` section.
- The PR body must include a `Rollback` section.
- If contracts or schema are involved, the PR must also follow the relevant
  `CONTRACT-` governance rules.

## Required review

Golden updates require reviewer coverage from the affected ownership area:

- Core / product owner review for simulation metrics or runner semantics.
- Visualization / artifact consumer review for RunArtifact layout or position
  timeline changes.
- Report review for customer-report golden changes.
- Contract review for schema, `src/Sim.Contracts`, or `packages/contracts`
  changes.
- Self-approval is not allowed; the author must not self-review and self-merge
  a golden update PR.

## Required validation commands

At minimum, run:

```bash
dotnet build
dotnet test

bash scripts/smoke-export-artifact.sh
bash scripts/smoke-comparison-artifact.sh
bash scripts/smoke-customer-report.sh

bash scripts/smoke-unified-export-artifact.sh
bash scripts/smoke-unified-comparison-artifact.sh
bash scripts/smoke-customer-report-runner-provenance.sh
bash scripts/audit-unified-export-artifact-diff.sh

git diff -- datasets/sample-small-warehouse/artifacts
git status -sb
```

The unified smoke scripts are manual validation unless and until a dedicated CI
task adds them to `check-all.sh` or GitHub Actions.

## Special rule for unified runner default switch

A default switch from legacy to unified runner is expected to change golden
artifacts. That change is not allowed inside a generic implementation PR. It
requires a dedicated `GOLDEN-` PR after readiness audit and provenance policy are
in place.

This rule applies to:

- `export-artifact` default switch.
- `compare-files` default switch.
- Customer report golden updates caused by runner semantic changes.
- Runner provenance requirements for report consumers.
- Release note / migration note requirements.

## Rollback rule

Golden update PRs must be independently revertible. If customer-visible KPI or
artifact behavior changes unexpectedly, first revert the golden/default switch
PR, then investigate runner logic in a separate fix. Rollback must not change
schema unless a separate schema PR explicitly handles that rollback.

## Current decision

`GOLDEN-U3d-default-unified-runner` is the dedicated golden update PR for the
default legacy-to-unified runner switch.

Default unified runner becomes the customer-facing golden baseline.

Explicit `--runner legacy` remains available for pre-switch reproduction and
rollback investigations, but legacy output no longer matches the tracked
default golden files after this switch.

This update follows the policy requirements:

- dedicated `GOLDEN-` branch / PR;
- golden diff summary;
- customer impact statement;
- regeneration commands;
- rollback plan;
- no RunArtifact / ComparisonArtifact schema changes.
