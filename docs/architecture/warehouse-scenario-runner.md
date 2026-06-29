# Warehouse Scenario Runner

本文档说明当前仓库级最小场景聚合链路。

当前阶段的重点不是做完整仓库仿真，而是把已经完成的 inbound、outbound、each-pick 三条最小流程聚合成一个稳定、可重复、可验收的 warehouse-level 输出。

## 当前入口

生产代码入口：

    src/Sim.Core/Scenarios/WarehouseScenario.cs
    src/Sim.Core/Scenarios/WarehouseRunResult.cs
    src/Sim.Core/Scenarios/WarehouseScenarioRunner.cs
    src/Sim.Core/Scenarios/Samples/WarehouseSampleScenarioFactory.cs

CLI 入口：

    src/Sim.Cli/Program.cs

Smoke 入口：

    scripts/smoke-sample-warehouse.sh

## 聚合关系

当前 warehouse-level 场景由三个可选子场景组成：

    WarehouseScenario
      InboundScenario?
      OutboundScenario?
      EachPickScenario?

对应 runner：

    WarehouseScenarioRunner
      InboundScenarioRunner
      OutboundScenarioRunner
      EachPickScenarioRunner

每个子 runner 独立运行，并产生自己的 run result、事件日志和最终 WorldState。

## 当前执行方式

当前 `WarehouseScenarioRunner` 采用最小聚合策略：

1. 如果存在 inbound 子场景，则运行 `InboundScenarioRunner`。
2. 如果存在 outbound 子场景，则运行 `OutboundScenarioRunner`。
3. 如果存在 each-pick 子场景，则运行 `EachPickScenarioRunner`。
4. 汇总各子流程的完成数量和数量指标。
5. 取所有子流程最早开始时间作为 `StartedAtMs`。
6. 取所有子流程最晚结束时间作为 `FinishedAtMs`。
7. 为每条子流程事件日志增加 flow 前缀。
8. 合并各子流程最终 `WorldState` 中的实体。

事件日志前缀格式：

    inbound|...
    outbound|...
    each_pick|...

## 当前输出

`WarehouseRunResult` 当前提供以下核心信息：

    ScenarioId
    Seed
    InboundResult
    OutboundResult
    EachPickResult

    CompletedReceipts
    CompletedOutboundOrders
    CompletedEachPickOrders

    TotalQuantityAvailable
    TotalQuantityShipped
    TotalQuantityPicked

    StartedAtMs
    FinishedAtMs
    EventLogText
    FinalWorldState

CLI 会把这些关键字段序列化为 JSON，便于外部脚本、未来展示层或后续验收工具读取。

## Sample Small Warehouse

当前稳定样例由以下工厂创建：

    WarehouseSampleScenarioFactory.CreateSmallWarehouse()

样例名称：

    sample-small-warehouse

固定 seed：

    20240627

当前样例包含：

    1 个 inbound receipt
    1 个 outbound order
    1 个 each-pick order

期望核心指标：

    completed_receipts: 1
    completed_outbound_orders: 1
    completed_each_pick_orders: 1

    total_quantity_available: 7
    total_quantity_shipped: 8
    total_quantity_picked: 9

    started_at_ms: 10
    finished_at_ms: 220
    final_world_time_ms: 220

## 验收方式

完整本地验收命令：

    ./scripts/check-no-unityengine.sh
    ./scripts/check-contract-drift.sh
    dotnet build
    dotnet test
    bash scripts/smoke-sample-warehouse.sh

直接查看 sample JSON：

    dotnet run --project src/Sim.Cli -- sample-small-warehouse

当前期望：

    dotnet test: all tests passed
    smoke: PASS: sample warehouse CLI smoke

## 当前设计边界

当前 warehouse runner 是最小可验证产品基线的一部分，而不是完整的全局仓库调度器。它服务于 artifact-backed deterministic simulation：先让真实仓库客户数据输入能够形成可信、可复现的运行产物，再逐步补齐真实路径、校准可信度和优化建议闭环。

当前 RunArtifact position timeline entries reflect deterministic baseline layout coordinates rather than simulated travel paths。它们是 baseline layout positions, NOT simulated movement；在 R2 完成 PathGraph、A* 与真实 position timeline 前，不得解释成真实 movement trace 或用于客户侧移动动画。

它当前不做：

- inbound、outbound、each-pick 之间的共享库存扣减
- 统一资源竞争的真实客户场景扩展
- 全局资源池调度
- 真实路径移动与路径耗时
- A* 路径搜索
- 输送线建模
- 包装复核
- 补货策略
- WMS 读写
- Unity 引擎可视化
- 校准可信度
- 真实仓试点
- 优化建议闭环
- AI 助手决策

这些能力必须在后续独立任务卡中逐步增加。

## 后续扩展原则

后续如果增强 warehouse-level 能力，应保持以下原则：

1. `src/Sim.Core` 继续保持纯 C#。
2. 不引入 `UnityEngine` 依赖。
3. 每个新增行为必须有确定性测试。
4. 每个新增 sample 输出必须能被 smoke 脚本验收。
5. 不直接把优化、AI、WMS 写回逻辑塞进 DES 逐事件循环。
6. 先扩展可验收输出，再扩展复杂业务。
7. 所有面向客户的评估必须区分当前已实现能力与 planned / next phases。

## 外部 JSON 场景输入链路

`WarehouseScenarioRunner` 当前支持两类入口：

    1. 内置样例：
       WarehouseSampleScenarioFactory.CreateSmallWarehouse()

    2. 外部 JSON 输入：
       WarehouseScenarioJsonLoader.Load(path)

CLI 对应命令为：

    dotnet run --project src/Sim.Cli -- sample-small-warehouse
    dotnet run --project src/Sim.Cli -- run-file datasets/sample-small-warehouse/scenario.json

外部 JSON 输入链路如下：

    datasets/sample-small-warehouse/scenario.json
      ↓
    WarehouseScenarioJsonLoader
      ↓
    WarehouseScenario
      ↓
    WarehouseScenarioRunner
      ↓
    WarehouseRunResult
      ↓
    CLI JSON 输出

`WarehouseScenarioJsonLoader` 负责将 snake_case JSON 输入转换为核心仿真模型，包括：

    1. InboundScenario
    2. OutboundScenario
    3. EachPickScenario
    4. process parameters
    5. resource counts
    6. inventory
    7. orders / receipts

该设计让 `Sim.Core` 继续保持纯 C# 仿真内核，同时把场景输入从硬编码样例逐步迁移到外部文件。后续创业 MVP 可以在此基础上继续扩展客户输入模板、JSON schema 校验、CSV/WMS 只读导入和多场景 A/B 对比。

外部输入链路已通过脚本固化验收：

    bash scripts/smoke-sample-warehouse-run-file.sh

CI 中同时运行：

    bash scripts/smoke-sample-warehouse.sh
    bash scripts/smoke-sample-warehouse-run-file.sh
