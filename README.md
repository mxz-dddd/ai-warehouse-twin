# AI Warehouse Twin

[![CI](https://github.com/mxz-dddd/ai-warehouse-twin/actions/workflows/ci.yml/badge.svg)](https://github.com/mxz-dddd/ai-warehouse-twin/actions/workflows/ci.yml)

AI Warehouse Twin 是一个面向仓储数字孪生的离散事件仿真原型项目。

- GitHub 仓库：https://github.com/mxz-dddd/ai-warehouse-twin
- 当前 Release：https://github.com/mxz-dddd/ai-warehouse-twin/releases/tag/v0.1.0-sim-core-sample

当前本地里程碑的重点是：先建立一个稳定、可重复、可验收的小型仓库仿真输出入口。

```text
入库最小流程
+ 出库整箱最小流程
+ 拣选最小流程
+ 仓库级场景聚合
+ CLI JSON 输出
+ RunArtifact / ComparisonArtifact / customer report handoff
+ smoke 脚本验收
```

## 当前能力

- 纯 C# `Sim.Core` 仿真核心。
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
- 最小业务流程：
  - 入库 receipt 流程
  - 出库整箱 order 流程
  - each-pick 拣选流程
- 仓库级场景聚合：
  - 聚合 inbound、outbound、each-pick 三个子流程
  - 汇总完成数和数量指标
  - 给子流程事件日志增加 flow 前缀
  - 合并子流程 `WorldState`
- 稳定样例场景：
  - `sample-small-warehouse`
  - 通过 `Sim.Cli` 输出确定性 JSON
  - 通过 smoke 脚本做稳定性验收
  - [Each-pick 最小确定性样例与 smoke 工作流](docs/each-pick.md)
- Artifact 与报告交付：
  - `RunArtifact v1` 可导出仿真运行产物，包含 KPI、event log、layout resources 和 position timeline。
  - `ComparisonArtifact v1` 可通过 `compare-files` 对 baseline / candidate scenario 做 A/B 指标对比。
  - `Sim.Report` 可生成 comparison Markdown summary 和 customer Markdown report。
  - `render-report` CLI 可从 RunArtifact + ComparisonArtifact 生成客户可读 Markdown。
  - [Customer report demo](docs/customer-report-demo.md) 提供可复制的端到端报告流程。

## 快速运行

在仓库根目录执行：

```bash
export PATH="/usr/bin:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"

dotnet build
dotnet test
bash scripts/smoke-sample-warehouse.sh
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

## 客户报告 Demo

下面命令会从 sample scenario 生成 RunArtifact，再从 baseline/candidate scenario 生成 ComparisonArtifact，最后生成客户可读 Markdown 报告：

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

每次修改仿真核心、场景、CLI 或 smoke 脚本后，执行：

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
bash scripts/smoke-report-demo-docs.sh
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
report demo docs smoke: PASS
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

## 当前限制

当前 warehouse runner 是最小聚合层。它会聚合多个子流程的结果，但还没有实现：

- inbound、outbound、each-pick 之间共享全局库存
- 跨流程资源竞争
- 仓库布局路径规划
- A* 路径搜索
- 输送线
- 复核和包装
- 补货
- WMS 集成
- Unity 可视化
- 优化循环
- AI 助手工作流

这些内容属于后续阶段，不能在没有明确任务卡的情况下扩展。

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
