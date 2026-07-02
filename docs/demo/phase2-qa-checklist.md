# Phase2 QA Checklist

This checklist is a skeleton for repeatable Phase2 demo validation. It should be updated as final scene integration, VFX, and UI prefab wiring land.

## Run Metadata

- Date:
- Tester:
- Branch / commit:
- PR:
- Unity version:
- OS:
- Demo scene:

```text
Assets/Scenes/Phase2DemoScene.unity
```

## Dotnet Gate

Run from repository root:

```bash
dotnet restore WarehouseTwin.sln -v:minimal
dotnet build WarehouseTwin.sln --no-restore -v:minimal -m:1 -p:UseSharedCompilation=false -p:NodeReuse=false
dotnet test WarehouseTwin.sln --no-restore -v:minimal -m:1 --disable-build-servers
```

Record:

- restore: PASS / FAIL / skipped
- build: PASS / FAIL / skipped
- test: PASS / FAIL / skipped
- notes:

## Unity Gate

Run or verify:

- compile/import: PASS / FAIL / skipped
- targeted EditMode: PASS / FAIL / skipped
- full EditMode: PASS / FAIL / skipped

Suggested targeted areas:

- Phase2DemoController tests
- WarehouseStructureBuilder tests
- ActorDirector tests
- PickCompleteVFX tests
- SetupPanel tests
- KpiHudPanel tests
- ABComparePanel tests
- ToastNotification tests

If Unity XML test output is missing but batchmode exits `0`, record:

```text
Unity batchmode exit 0; XML result missing; Editor log reviewed with no errors.
```

Attach the Editor log or paste the relevant no-error summary. Do not invent test counts.

## Red Line Checks

Confirm the PR or release branch does not include unrelated changes:

- no `src/Sim.Core` changes unless explicitly authorized;
- no `src/Sim.Cli` changes unless explicitly authorized;
- no datasets golden artifact changes unless explicitly authorized;
- no Unity `using Sim.Core`;
- no random `ProjectSettings` changes;
- no package lock churn;
- no unintended scene changes;
- no unintended prefab changes.

Helpful commands:

```bash
git diff --name-only
rg "using Sim.Core" engine/unity
git diff -- src/Sim.Core src/Sim.Cli datasets
git diff --check
```

Repository scripts, when applicable:

```bash
bash scripts/check-no-unityengine.sh
bash scripts/check-consumer-no-core.sh
bash scripts/check-contract-drift.sh
```

## Demo Visual QA

Open:

```text
engine/unity/AIWarehouseTwin
Assets/Scenes/Phase2DemoScene.unity
```

Check the visual demo:

- Floor visible and correctly sized.
- Zones visible and named/colored clearly.
- Shelves visible, stable, and aligned.
- Docks visible for receiving/shipping.
- Worker visible if final actor integration is present.
- Forklift visible if final actor integration is present.
- Playback speed cycles through `1x / 5x / 10x / ⚡` or `1× / 5× / 10× / ⚡`.
- KPI HUD visible if final UI wiring is present.
- AB Compare visible if final UI wiring is present.
- Toast visible for warning/info flows if final UI wiring is present.
- PickCompleteVFX visible if final VFX wiring is present.

For anything not yet integrated, mark:

```text
Not verified - waiting final integration confirmation.
```

## Functional QA Notes

### Medium Artifact Load

- Expected artifact source:

```text
Assets/StreamingAssets/medium-warehouse-artifact.json
```

- Loads without console error: PASS / FAIL
- Warehouse structure generated: PASS / FAIL
- Notes:

### Actor Playback

- ActorDirector scene wiring confirmed: YES / NO
- Worker route playback: PASS / FAIL / waiting final integration
- Forklift route playback: PASS / FAIL / waiting final integration
- Deterministic fallback route used: YES / NO
- If fallback used, honesty label recorded: YES / NO
- Notes:

### KPI HUD

- Completion rate visible: PASS / FAIL / waiting final integration
- Average wait visible: PASS / FAIL / waiting final integration
- Processed orders visible: PASS / FAIL / waiting final integration
- Path efficiency visible: PASS / FAIL / waiting final integration
- Simulation time visible: PASS / FAIL / waiting final integration
- Speed label visible: PASS / FAIL / waiting final integration
- Notes:

### A/B Compare

- Baseline displayed: PASS / FAIL / waiting final integration
- Candidate / optimized displayed: PASS / FAIL / waiting final integration
- KPI deltas displayed: PASS / FAIL / waiting final integration
- Zero improvement shows `优化差异待进一步仿真迭代`: PASS / FAIL / not applicable
- Honesty note visible: PASS / FAIL / waiting final integration
- Notes:

## Known Issue Template

Use this template for any demo blocker or caveat:

```text
Issue:
Severity: blocker / high / medium / low
Area: Unity compile / scene / artifact / actor / VFX / UI / KPI / A/B / docs
Observed:
Expected:
Reproduction:
Evidence:
Workaround:
Owner:
Status:
Follow-up PR / task:
```

## Release Readiness Checklist

Before announcing a Phase2 demo build:

- Dotnet restore/build/test gate recorded.
- Unity compile/import gate recorded.
- Targeted EditMode tests recorded or explicitly skipped with reason.
- Full EditMode tests recorded or explicitly skipped with reason.
- Red line checks recorded.
- Demo scene opens.
- Medium artifact loads.
- Warehouse structure appears.
- Actor playback status is explicitly marked.
- PickCompleteVFX status is explicitly marked.
- SetupPanel status is explicitly marked.
- KPI HUD status is explicitly marked.
- AB Compare status is explicitly marked.
- Toast status is explicitly marked.
- Honesty notes reviewed.
- Known issues are listed.
- No one claims real WMS data, real telemetry, or guaranteed operational improvement.

## Sign-off

- Engineering:
- Product/demo owner:
- QA:
- Date:
