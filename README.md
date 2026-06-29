# AI Warehouse Twin

[![CI](https://github.com/mxz-dddd/ai-warehouse-twin/actions/workflows/ci.yml/badge.svg)](https://github.com/mxz-dddd/ai-warehouse-twin/actions/workflows/ci.yml)

AI Warehouse Twin 是面向真实仓库客户的创业产品，目标是让客户输入真实仓库数据，得到可信、可视、带可信度的评估与优化建议。

当前阶段是最小可验证产品基线：先用 artifact-backed deterministic simulation 固化仓库运行事实、A/B 对比和客户报告，再围绕统一资源竞争、真实路径位置、客户级 KPI、引擎可视化、校准可信度和 WMS 试点逐步推进真实落地。

- GitHub 仓库：https://github.com/mxz-dddd/ai-warehouse-twin
- H1 RunArtifact 里程碑：`v0.2.0-h1-runartifact`
- H2 ComparisonArtifact 里程碑：`v0.3.0-h2-comparison`

产品闭环目标：

```text
真实仓库数据输入
→ 确定性仓库仿真
→ RunArtifact / ComparisonArtifact
→ 客户可读报告与后续可视化
→ 带可信度分级的评估与优化建议
```

## 当前能力

- 纯 C# deterministic warehouse simulation core。
- 确定性 DES 离散事件仿真内核：
  - 仿真时钟
  - 确定性随机数
  - 稳定事件队列
  - 确定性事件日志
  - `WorldState` 状态快照
- 领域规则与不变量：
  - 库存状态机
  - 数量守恒检查
  - 领域规则异常
- 资源建模：
  - 有限资源池
  - FIFO 等待队列
  - 利用率快照
- 统一运行与客户 KPI 基线：
  - 统一 operation runner
  - 资源利用率 KPI
  - bottleneck candidate baseline
  - grouped customer KPI summaries
- 最小业务流程：
  - 入库 receipt 流程
  - 出库整箱 order 流程
  - each-pick 拣选流程
- 仓库级场景聚合：
  - 聚合 inbound、outbound、each-pick 三个子流程
  - 汇总完成数和数量指标
  - 给子流程事件日志增加 flow 前缀
  - 合并子流程 `WorldState`
- Artifact handoff：
  - `RunArtifact v1`：仿真运行产物，包含 KPI、event log、layout resources 和 position timeline。
  - `ComparisonArtifact v1`：baseline / candidate A/B 指标差异。
  - deterministic golden artifacts 与 smoke / CI guard。
  - 当前 `RunArtifact v1` 的 layout resources 与 position timeline 只能表示 baseline layout positions, NOT simulated movement。它们是 deterministic layout handoff，不是由路径规划或移动仿真产生的真实 movement trace。
- 报告与客户交付层：
  - `Sim.Report` 可读取 artifact 并渲染 Markdown。
  - `render-report` CLI 可从 RunArtifact + ComparisonArtifact 生成 customer Markdown report。
  - `customer-report.v1.md` golden 保护客户报告输出。
- 输入与验证：
  - 外部 JSON scenario 输入。
  - APP-020 输入校验模块基线。
  - APP-030 多订单 golden dataset 基线。
- 稳定样例场景：
  - `sample-small-warehouse`
  - `sample-small-warehouse` candidate scenario
  - APP-030 multi-order golden dataset
  - 通过 `Sim.Cli` 输出确定性 JSON
  - 通过 smoke 脚本做稳定性验收
  - [Each-pick 最小确定性样例与 smoke 工作流](docs/each-pick.md)

## 快速运行

在仓库根目录执行：

```bash
export PATH="/usr/bin:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"

dotnet build
dotnet test
bash scripts/check-all.sh
```

运行小型仓库样例：

```bash
dotnet run --project src/Sim.Cli -- sample-small-warehouse
```

期望核心输出：

```json
{
  "scenario_id": "sample-small-warehouse",
  "seed": 20240627,
  "started_at_ms": 10,
  "finished_at_ms": 220,
  "completed_receipts": 1,
  "completed_outbound_orders": 1,
  "completed_each_pick_orders": 1,
  "total_quantity_available": 7,
  "total_quantity_shipped": 8,
  "total_quantity_picked": 9,
  "final_world_time_ms": 220
}
```

CLI 还会输出 `event_log_text`，其中包含 inbound、outbound、each-pick 三条子流程的确定性事件日志。

生成 artifact 与客户报告：

```bash
tmpdir="$(mktemp -d)"

dotnet run --project src/Sim.Cli -- export-artifact \
  datasets/sample-small-warehouse/scenario.json \
  -o "$tmpdir/run-artifact.v1.json"

dotnet run --project src/Sim.Cli -- compare-files \
  datasets/sample-small-warehouse/scenario.json \
  datasets/sample-small-warehouse/scenario-candidate.json \
  -o "$tmpdir/comparison-artifact.v1.json"

dotnet run --project src/Sim.Cli -- render-report \
  "$tmpdir/run-artifact.v1.json" \
  "$tmpdir/comparison-artifact.v1.json" \
  -o "$tmpdir/customer-report.v1.md"

cat "$tmpdir/customer-report.v1.md"

rm -rf "$tmpdir"
```

当前 sample golden artifacts：

```text
datasets/sample-small-warehouse/artifacts/run-artifact.v1.json
datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json
datasets/sample-small-warehouse/artifacts/customer-report.v1.md
```

## 验收命令

每次修改仿真核心、场景、CLI、报告、校验或 smoke 脚本后，执行：

```bash
./scripts/check-no-unityengine.sh
./scripts/check-contract-drift.sh
bash scripts/check-consumer-no-core.sh
dotnet build
dotnet test
bash scripts/smoke-sample-warehouse.sh
bash scripts/smoke-sample-warehouse-run-file.sh
bash scripts/smoke-export-artifact.sh
bash scripts/smoke-comparison-artifact.sh
bash scripts/smoke-customer-report.sh
bash scripts/check-all.sh
```

当前期望结果：

```text
no UnityEngine references: PASS
contract drift: PASS
dotnet build: 0 warnings, 0 errors
dotnet test: all tests passed
sample warehouse smoke: PASS
customer report smoke: PASS
full local validation: PASS
```

## 项目结构

```text
src/Sim.Core
  DES 内核、领域规则、业务流程、场景模型、场景 runner。

src/Sim.Core.Tests
  xUnit 测试，覆盖领域规则、事件、流程 runner、warehouse runner 和样例输出。

src/Sim.Contracts
  Artifact contracts，包括 RunArtifact v1 和 ComparisonArtifact v1。

src/Sim.Cli
  命令行入口，用于运行 sample、导出 artifact、对比 scenario 和生成 customer report。

src/Sim.Report
  Markdown report loading/rendering，包括 comparison summary 和 customer report。

src/Sim.Report.Tests
  Report renderer / golden tests。

src/Sim.Validation
  客户输入校验基线，用于把真实仓库客户数据输入逐步产品化。

scripts
  合约生成、漂移检查、UnityEngine 边界检查、smoke 验收脚本。

docs
  架构和领域说明文档。

datasets/sample-small-warehouse
  小型仓库样例说明。

datasets/sample-small-warehouse/artifacts
  Deterministic run/comparison/customer report golden artifacts。
```

## 主要入口

场景 runner：

```text
InboundScenarioRunner
OutboundScenarioRunner
EachPickScenarioRunner
WarehouseScenarioRunner
```

样例工厂：

```text
WarehouseSampleScenarioFactory.CreateSmallWarehouse()
```

CLI 入口：

```bash
dotnet run --project src/Sim.Cli -- sample-small-warehouse
dotnet run --project src/Sim.Cli -- run-file datasets/sample-small-warehouse/scenario.json
dotnet run --project src/Sim.Cli -- export-artifact datasets/sample-small-warehouse/scenario.json -o /tmp/run-artifact.v1.json
dotnet run --project src/Sim.Cli -- compare-files datasets/sample-small-warehouse/scenario.json datasets/sample-small-warehouse/scenario-candidate.json -o /tmp/comparison-artifact.v1.json
dotnet run --project src/Sim.Cli -- render-report datasets/sample-small-warehouse/artifacts/run-artifact.v1.json datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json -o /tmp/customer-report.v1.md
```

Smoke 入口：

```bash
bash scripts/smoke-sample-warehouse.sh
```

## 技术边界

- `src/Sim.Core` 必须保持纯 C#，不能依赖 `UnityEngine`。
- DES 仿真主循环运行在 C# 中。
- Python 只用于粗粒度优化、校准和工具脚本，不能进入逐事件仿真循环。
- Unity 或其他展示层代码不能进入 `src/Sim.Core`。
- LLM / AI 组件不能直接修改库存或控制真实设备。
- 每次可复现仿真运行都必须有显式 seed。

## 产品路线限制与 next phases

当前阶段是最小可验证产品基线，已具备确定性仿真 artifact、A/B comparison、customer report、golden、smoke 和 CI。以下能力属于 planned / next phases，不能写成已完成：

在 R2 完成 PathGraph、A* 与真实 position timeline 之前，禁止把当前 layout resources / position timeline 坐标用于“人/叉车/货物正在移动”的客户演示、宣传或移动动画。这些坐标当前只能被解释为 baseline layout positions, NOT simulated movement。

- 统一资源竞争的真实客户场景扩展
- 真实路径移动与路径耗时
- 客户级 KPI 与可信度分级
- 校准可信度与真实仓数据回放
- 完整 WMS 集成
- 真实仓试点
- Unity 引擎可视化
- 优化建议闭环
- 输送线
- 复核和包装
- 补货

这些内容必须通过独立任务卡逐步增加，并通过 artifact、golden、smoke、CI 和客户数据校准闭环验证。

## 外部 JSON 场景输入

除了内置样例：

    dotnet run --project src/Sim.Cli -- sample-small-warehouse

当前 CLI 也支持从外部 JSON 场景文件运行仿真：

    dotnet run --project src/Sim.Cli -- run-file datasets/sample-small-warehouse/scenario.json

这条链路用于把项目从“代码内置样例”推进到“用户/客户提供场景输入 -> 仿真运行 -> JSON 输出”的创业 MVP 形态。

当前外部输入样例位于：

    datasets/sample-small-warehouse/scenario.json

对应验收脚本：

    bash scripts/smoke-sample-warehouse-run-file.sh

该脚本会比较内置 sample-small-warehouse 与 run-file scenario.json 的输出是否完全一致，并检查关键指标与事件日志。GitHub Actions CI 已包含该验收步骤。
