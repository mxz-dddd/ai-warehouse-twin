# Phase2 Demo Guide

本文档是 Phase2 demo 的可更新骨架版操作说明。它用于演示准备、交接和 QA 记录，不代表所有脚本层能力都已经完成最终场景总装。

## Status Legend

- `已接入`: 当前 `app/demo-phase2` 主线已有明确 scene 或 controller wiring。
- `脚本层已完成`: 脚本和测试已合并，但最终 scene / prefab wiring 仍需独立确认。
- `待最终集成确认`: 正在由其他 Line 收束或尚未在主场景中完成验收。
- `演示诚实边界`: 可以解释演示流程，但不能宣称生产 WMS、真实运营承诺或实测优化收益。

## Open The Unity Demo

Unity project path:

```text
engine/unity/AIWarehouseTwin
```

Scene path:

```text
Assets/Scenes/Phase2DemoScene.unity
```

Recommended opening flow:

1. Open Unity Hub or Unity Editor.
2. Add/open the project at `engine/unity/AIWarehouseTwin`.
3. Open `Assets/Scenes/Phase2DemoScene.unity`.
4. Check Console for import or compile errors before entering Play Mode.
5. If using batchmode QA, keep the Editor log and any generated XML test output together with the run notes.

## Current Demo Flow

The current Phase2 demo flow should be described as artifact-backed and integration-in-progress:

1. Load the medium artifact.
   - Status: `已接入` for the current `Phase2DemoController` artifact loading path.
   - Current controller default filename: `medium-warehouse-artifact.json`.
   - Expected location during Unity runtime: `Assets/StreamingAssets`.
2. Generate warehouse structure.
   - Status: `已接入` for `Phase2DemoController.GenerateWarehouseStructure`.
   - Structure generation uses layout generation plus `WarehouseStructureBuilder`.
   - Expected visual objects include Floor, Zones, Shelves, and Docks.
3. Generate Actors and play animation.
   - Status: `脚本层已完成 / 待最终集成确认`.
   - `ActorDirector`, actor scripts, and animation-related tests exist in the codebase.
   - Final ActorDirector wiring into the demo controller and scene should be confirmed by the Line1 integration task before claiming end-to-end demo playback.
4. Pick-complete visual effect.
   - Status: `脚本层已完成 / 待最终集成确认`.
   - `PickCompleteVFX` exists, but final demo scene behavior should be confirmed by the Line2 VFX task before presenting it as part of the official flow.
5. SetupPanel.
   - Status: `脚本层已完成`.
   - It provides parameter binding and scenario build callback seams.
   - Final scene/prefab placement must be confirmed separately.
6. KPI HUD.
   - Status: `脚本层已完成`.
   - `KpiHudPanel` reads KPI through an injectable snapshot source and uses a playback seam for speed label/cycle behavior.
   - Final HUD placement in the upper-right demo UI must be confirmed separately.
7. ABComparePanel.
   - Status: `脚本层已完成`.
   - It can present A/B comparison data and honesty notes.
   - Final scene/prefab placement must be confirmed separately.
8. Toast.
   - Status: `脚本层已完成`.
   - It is available as a reusable notification component.
   - Final scene/prefab placement must be confirmed separately.

## Playback Speed Controls

The playback speed selector uses the following labels:

```text
1x / 5x / 10x / ⚡
```

In Unity UI, the multiplication label may render as:

```text
1× / 5× / 10× / ⚡
```

Demo guidance:

- `1x`: normal speed.
- `5x`: accelerated playback for shorter demo loops.
- `10x`: fast playback for scanning route behavior.
- `⚡`: fastest demo playback mode.

Do not describe speed cycling as a real-time warehouse control interface. It is a demo playback control.

## Parameter Configuration

The SetupPanel script layer exposes demo parameters for repeatable scenario setup:

| Parameter | Meaning | Demo note |
| --- | --- | --- |
| `lengthM` | Warehouse length in meters | Used to size/layout the demo warehouse. |
| `widthM` | Warehouse width in meters | Used to size/layout zones and shelf spacing. |
| `shelfRows` | Number of shelf rows | Must remain compatible with warehouse width. |
| `skuCount` | SKU count | Demo workload/input scale, not a live inventory count. |
| `workerCount` | Worker actor count | Demo resource count. |
| `forkliftCount` | Forklift actor count | Demo resource count. |
| `orderCount` | Order count | Demo workload count. |

Parameter changes should be described as deterministic demo inputs. They are not a production WMS write-back path.

## A/B Compare Flow

The A/B comparison panel should be presented as a deterministic artifact comparison:

1. Load baseline artifact data.
2. Load candidate / optimized artifact data.
3. Display KPI deltas and improvement summary.
4. Preserve honesty notes.
5. If improvement is zero or not meaningful, show:

```text
优化差异待进一步仿真迭代
```

Do not claim that A/B improvement is measured production improvement unless a future task explicitly connects verified WMS or operational telemetry.

## Common Questions

### Unity XML test result is missing, but batchmode exits 0

If Unity batchmode exits `0` and the Editor log has no compile errors, import errors, or failing tests, record the run as:

```text
Unity batchmode exit 0; XML result missing; Editor log reviewed with no errors.
```

Do not fabricate test counts. Attach or reference the Editor log path in the QA notes, and mark the XML artifact as missing.

### Is the demo data real WMS data?

No. Phase2 uses deterministic simulation and committed demo artifacts. It is suitable for explaining the product flow and integration seams, not for claiming real customer WMS measurements.

Recommended wording:

```text
这是可解释的动态仓库数字孪生 Demo。
当前用于演示流程和接口闭环，不等同于生产 WMS 数据。
```

### What does deterministic fallback route mean?

A deterministic fallback route is a stable, repeatable route generated when richer movement data is unavailable or not yet wired into the final scene. It is useful for demo continuity and testing, but it must be labeled clearly:

```text
Fallback route used; not claimed as a real simulated route.
```

Do not describe fallback routes as real WMS tracks, sensor traces, or physically validated warehouse travel paths.

## Update Notes For Future Lines

- When ActorDirector final integration is merged and verified, update the Actor playback status from `待最终集成确认` to the verified state.
- When PickCompleteVFX scene wiring is merged and verified, update the VFX status.
- When SetupPanel, KPI HUD, ABComparePanel, and Toast are wired into the final scene/prefabs, update their statuses with the PR number and validation evidence.
- Keep the honesty wording in this guide aligned with `docs/demo/phase2-honesty-notes.md`.
