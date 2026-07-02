# R3 Phase-1 演示交接与诚实边界说明

本文档面向后续演示人员，说明 R3 Phase-1 中型仓库演示的 artifact 使用方式、Unity 侧加载边界，以及客户沟通时必须保留的证据与诚实边界。

本文档基于 `origin/main` 提交：

```text
1e2399acfa8f0ec3d95c2ed7d2c473e34e166932
```

该提交已经包含 PR #100 Final Unity integration smoke 之后的状态。本文档只用于演示交接说明。它不新增或重新生成 artifact，不修改 contracts，不修改 Unity 功能，也不改变任何运行逻辑。

## 演示状态与范围

R3 演示面向以下内容：

- `datasets/medium-warehouse` baseline 场景。
- `datasets/medium-warehouse/optimized` ABC slotting candidate 场景。
- 已提交的 RunArtifact、MovementArtifact 和 ComparisonArtifact。
- Unity 侧 B1 仓库平面、B2 演员运动、B3 A/B Showcase 的加载与展示接缝。
- 基于确定性仿真的 KPI 讲解。

本演示不是生产级 WMS 集成，不是传感器校准定位系统，也不是全局最优优化系统。演示时应明确说明：当前结果来自确定性建模仿真，需要保留证据标签和能力边界。

当前 `main` 已包含 R3 Phase-1 相关能力：

- B0 Unity R3 DTO / loader 接缝：`RunArtifactLoader`、`MovementArtifactLoader`、`ComparisonArtifactLoader`。
- B1 仓库平面、图结构和 layout renderer。
- B2 基于 MovementArtifact 路径数据的演员动画时间线。
- B3 使用 ComparisonArtifact DTO 的 mock A/B Showcase 视图模型与 presenter。
- A1-S5 MovementArtifact 导出。
- A2a `RunArtifact.warehouse_graph` 导出。
- A3b KPI 写入 RunArtifact。
- A4b medium baseline golden artifacts。
- A5b medium A/B comparison artifacts。
- Final Unity integration smoke 已验证 Unity 侧 artifact loader 以及 B1 / B2 / B3 view-model integration path。

## Artifact 清单

### RunArtifact

RunArtifact 包含确定性仿真运行结果。medium 演示的 baseline 文件为：

```text
datasets/medium-warehouse/artifacts/run-artifact.v1.json
```

medium 演示的 optimized 文件为：

```text
datasets/medium-warehouse/optimized/artifacts/run-artifact.v1.json
```

R3 阶段的 RunArtifact 包含：

- 仿真运行事实和事件输出。
- 用于静态仓库图和平面展示的 `warehouse_graph`。
- `kpi_summary`，包括 `order_cycle_p50_ms`、`order_cycle_p90_ms`、`order_cycle_p95_ms`、`avg_wait_ms`、`resource_utilization`、`bottlenecks`、`travel_distance_m_by_actor_type`。
- 既有的 `position_timeline` 字段。

Unity B1 应通过 loader 接缝读取 `RunArtifact.warehouse_graph`，用于仓库平面和图结构展示。KPI 面板可以读取 `RunArtifact.kpi_summary`。

不要把 `RunArtifact.position_timeline` 讲成真实运动轨迹。它是确定性交接数据，不应用于客户演示中的演员运动推断。

仓库中已有的 baseline 生成命令形态来自 `src/Sim.Cli` usage 和 `scripts/smoke-medium-warehouse-golden.sh`：

```bash
dotnet run --project src/Sim.Cli -- \
  export-artifact datasets/medium-warehouse/scenario.json \
  -o /tmp/medium-run-artifact.v1.json
```

客户演示优先使用已提交的 golden artifact。只有在明确验证再生成能力时，才运行：

```bash
bash scripts/smoke-medium-warehouse-golden.sh
```

### MovementArtifact

MovementArtifact 包含确定性建模运动数据。medium 演示的 baseline 文件为：

```text
datasets/medium-warehouse/artifacts/movement-artifact.v1.json
```

medium 演示的 optimized 文件为：

```text
datasets/medium-warehouse/optimized/artifacts/movement-artifact.v1.json
```

MovementArtifact 包含：

- `warehouse_graph`。
- `movement_events`。
- `route_segments`。
- 描述确定性建模生成方式的 provenance。

MovementArtifact 中的路径来自 layout graph 最短路径和确定性模型。它不是传感器校准轨迹，不是 WMS 实采轨迹，也不是叉车、人员或 AGV 的真实定位记录。

仓库中已有的 baseline MovementArtifact 生成命令形态来自 `scripts/smoke-medium-warehouse-golden.sh`：

```bash
dotnet run --project src/Sim.Cli -- \
  export-movement-artifact datasets/medium-warehouse/scenario.json \
  -o /tmp/medium-movement-artifact.v1.json \
  --run-id medium-warehouse \
  --source-run-artifact datasets/medium-warehouse/artifacts/run-artifact.v1.json \
  --graph-source medium-warehouse-layout \
  --generator-version cli-a4b
```

Unity B2 应读取 `MovementArtifact.route_segments` 和 `MovementArtifact.movement_events`。不要从 `RunArtifact.position_timeline` 推断运动。

### ComparisonArtifact

ComparisonArtifact 包含 baseline 与 candidate 的对比数据。medium A/B 演示文件为：

```text
datasets/medium-warehouse/optimized/artifacts/comparison-artifact.v1.json
```

ComparisonArtifact 的 DTO 字段名是 `candidate`。Unity 画面可以把这一侧显示为 “Optimized”，但 artifact 字段仍然是 `candidate`。不要发明 `optimized` DTO 字段。

R3 阶段的 ComparisonArtifact 包含：

- `baseline`。
- `candidate`。
- `deltas`。
- `kpi_deltas`。
- `improvement_pct`。
- `baseline_run_id`。
- `optimized_run_id`。
- `optimization_note`。
- `evidence_level`。

B3 A/B Showcase 应使用 `ComparisonArtifactLoader`，并从 DTO 读取 `candidate`、`kpi_deltas` 和 `improvement_pct`。Unity 侧不应重新计算真实 KPI 差异，也不应重新计算真实改善百分比。

`datasets/medium-warehouse/optimized` 目录已经包含 A5b 提交的 ComparisonArtifact。验证确定性再生成时使用：

```bash
bash scripts/smoke-medium-warehouse-ab-comparison.sh
```

该 smoke 使用的命令形态为：

```bash
dotnet run --project src/Sim.Cli -- \
  export-medium-ab-comparison \
  datasets/medium-warehouse/scenario.json \
  datasets/medium-warehouse/optimized/abc-slotting-config.json \
  --baseline-run-artifact datasets/medium-warehouse/artifacts/run-artifact.v1.json \
  --output-dir /tmp/medium-warehouse-optimized
```

该命令会在指定输出目录下写入 derived optimized `scenario.json`、`artifacts/run-artifact.v1.json`、`artifacts/movement-artifact.v1.json` 和 `artifacts/comparison-artifact.v1.json`。

## Unity 加载交接

Unity 侧已经具备 loader 和 view-model 接缝，且 Final Unity integration smoke 已验证 Unity 侧 artifact loader 以及 B1 / B2 / B3 view-model integration path。B3 仍是 demo UI skeleton / fixture Showcase；final integration smoke 只验证它可以消费 A5b real ComparisonArtifact DTO path，不代表已经完成全部真实客户 UI wiring。除非在独立任务中确认了当前 demo scene，不要宣称所有 scene 都已经完成接入。

artifact 与 Unity 展示的对应关系如下：

- B1 仓库平面、图结构和 layout：通过 `RunArtifactLoader` 读取 RunArtifact，使用 `warehouse_graph`。
- KPI 展示：通过 `RunArtifactLoader` 读取 RunArtifact，使用 `kpi_summary` 展示 P50 / P90 / P95、等待时间、利用率、瓶颈和行走距离字段。
- B2 演员运动：通过 `MovementArtifactLoader` 读取 MovementArtifact，使用 `route_segments` 和 `movement_events`。
- B3 A/B Showcase：通过 `ComparisonArtifactLoader` 读取 ComparisonArtifact，使用 `candidate`、`kpi_deltas` 和 `improvement_pct`。

现有 Unity 代码和测试中有默认 `StreamingAssets` 约定，例如 `run-artifact.v1.json`。如果演示需要在 Unity 中加载 medium artifacts，应在独立 integration 任务中通过当前 scene 或 runner 约定放置或 wiring 对应 artifact。本文档不授权修改 Unity Scene、prefab、ProjectSettings 或功能代码。

## 证据与诚实边界

客户沟通时应使用以下表述：

- movement 是确定性建模运动。
- movement 不是传感器校准轨迹。
- 本演示没有使用真实 WMS 遥测数据。
- ABC slotting 是确定性演示启发式策略。
- ABC slotting 不保证全局最优。
- A/B 改善来自确定性仿真 artifact。
- A/B 改善不是生产 WMS 实测结果。
- medium dataset 用于演示交付；除非明确指向已提交 golden artifact，否则不要说成生产结论。
- KPI 值是确定性仿真输出，不是经审计的运营 KPI。
- ComparisonArtifact 中的 `candidate` 是 `optimized` 场景 artifact，但 “optimized” 不等于全局最优。

当前 A5b medium ComparisonArtifact 对跟踪 KPI 如实记录了 0% `improvement_pct`。演示时不要虚构改善收益。

## 客户演示流程

1. 从 medium warehouse 场景开始。
   - 说明它是 R3 Phase-1 的确定性演示数据。
   - 指出 baseline RunArtifact 和 MovementArtifact 是已提交的演示 artifact。
2. 展示 B1 仓库平面、图结构和 layout。
   - 使用 RunArtifact 的 `warehouse_graph`。
   - 说明这是用于可视化的确定性 layout graph。
3. 展示 B2 演员运动时间线。
   - 使用 MovementArtifact 的 `route_segments` 和 `movement_events`。
   - 说“确定性建模运动”，不要说“传感器轨迹”。
4. 展示 KPI。
   - 展示 P50 / P90 / P95 order cycle time。
   - 展示等待时间。
   - 展示资源利用率。
   - 展示瓶颈。
   - 说明这些是确定性仿真 KPI。
5. 展示 B3 A/B Showcase。
   - 展示 baseline 侧。
   - 展示 candidate / Optimized 侧。
   - 展示 `kpi_deltas`。
   - 展示 `improvement_pct`。
   - 说明 medium A5b artifact 当前对跟踪 KPI 记录的是 0% 改善，而不是编造收益。
6. 结束时再次说明诚实边界。
   - 这是确定性仿真。
   - 不是 WMS 实测。
   - 不是传感器校准。
   - 是启发式优化演示，不是全局优化证明。

## 可展示内容

- 使用基于 artifact 的演示流程。
- 保留来源和证据标签。
- 使用“确定性建模运动”这一表述。
- 使用“演示启发式策略”这一表述。
- 使用“基于仿真的改善”这一表述。
- 客户演示优先使用已提交的 medium artifacts，保证叙述一致。

## 不应宣称的内容

- 不要说成传感器轨迹。
- 不要说成 WMS 实测改善。
- 不要说成全局最优。
- 不要从 `RunArtifact.position_timeline` 推断运动。
- 不要把 mock、fixture 或 demo 数据说成真实 comparison。
- 不要把确定性 KPI 说成经审计的运营 KPI。

## 演示人员快速检查清单

- Baseline RunArtifact：
  `datasets/medium-warehouse/artifacts/run-artifact.v1.json`
- Baseline MovementArtifact：
  `datasets/medium-warehouse/artifacts/movement-artifact.v1.json`
- Optimized RunArtifact：
  `datasets/medium-warehouse/optimized/artifacts/run-artifact.v1.json`
- Optimized MovementArtifact：
  `datasets/medium-warehouse/optimized/artifacts/movement-artifact.v1.json`
- Medium ComparisonArtifact：
  `datasets/medium-warehouse/optimized/artifacts/comparison-artifact.v1.json`
- Baseline artifact smoke：
  `bash scripts/smoke-medium-warehouse-golden.sh`
- A/B comparison smoke：
  `bash scripts/smoke-medium-warehouse-ab-comparison.sh`

本文档是叙述和操作交接说明。任何新的 Unity 接入、artifact 再生成或额外 Unity 验证都应作为独立任务处理，并使用干净 worktree。
