# Sample Small Warehouse

`sample-small-warehouse` 是当前项目的最小可运行仓库样例。

它不是完整真实仓库数据集，而是一个用于验证 DES 仿真核心、业务流程、仓库级聚合和 CLI 输出稳定性的确定性样例。

## 样例目标

该样例用于验证以下能力：

- inbound 入库最小流程可以完成。
- outbound 出库整箱最小流程可以完成。
- each-pick 拣选最小流程可以完成。
- warehouse runner 可以聚合三个子流程结果。
- CLI 可以输出稳定 JSON。
- smoke 脚本可以检查关键指标和事件日志。

## 运行方式

在仓库根目录执行：

    dotnet run --project src/Sim.Cli -- sample-small-warehouse

也可以执行 smoke 验收：

    bash scripts/smoke-sample-warehouse.sh

期望 smoke 输出：

    PASS: sample warehouse CLI smoke

## 当前样例内容

当前样例由代码工厂生成：

    src/Sim.Core/Scenarios/Samples/WarehouseSampleScenarioFactory.cs

入口方法：

    WarehouseSampleScenarioFactory.CreateSmallWarehouse()

该方法会创建一个 warehouse-level 场景，包含：

    InboundScenario
    OutboundScenario
    EachPickScenario

## 固定输入

### Inbound

    receipt_id: receipt-1
    warehouse_id: warehouse-1
    sku_id: sku-inbound-1
    quantity: 7
    arrives_at_ms: 10

### Outbound

    order_id: order-1
    warehouse_id: warehouse-1
    sku_id: sku-outbound-1
    quantity: 8
    released_at_ms: 20

### Each-pick

    order_id: each-order-1
    warehouse_id: warehouse-1
    sku_id: sku-each-1
    quantity: 9
    released_at_ms: 30

## 期望输出指标

    scenario_id: sample-small-warehouse
    seed: 20240627
    started_at_ms: 10
    finished_at_ms: 220

    completed_receipts: 1
    completed_outbound_orders: 1
    completed_each_pick_orders: 1

    total_quantity_available: 7
    total_quantity_shipped: 8
    total_quantity_picked: 9

    final_world_time_ms: 220

## 期望事件日志

CLI 输出中的 `event_log_text` 应包含 10 条事件：

    inbound|0|10|inbound.receipt_arrived.receipt-1|InboundReceiptArrived
    inbound|1|110|inbound.unload_completed.receipt-1|InboundUnloadCompleted
    inbound|2|210|inbound.putaway_completed.receipt-1|InboundPutawayCompleted
    outbound|0|20|outbound.order_released.order-1|OutboundOrderReleased
    outbound|1|120|outbound.pick_completed.order-1|OutboundPickCompleted
    outbound|2|220|outbound.load_completed.order-1|OutboundLoadCompleted
    each_pick|0|30|each_pick.order_released.each-order-1|EachPickOrderReleased
    each_pick|1|60|each_pick.at_station.each-order-1|EachPickAtStation
    each_pick|2|90|each_pick.completed.each-order-1|EachPickCompleted
    each_pick|3|130|each_pick.staged.each-order-1|EachPickStaged

## 当前限制

该样例只用于稳定性验收，不代表完整仓库业务闭环。

当前尚未包含：

- 文件导入
- 外部 WMS 数据
- 全局共享库存
- 跨流程资源竞争
- 仓库布局
- 路径规划
- 输送线
- 补货
- 包装复核
- Unity 展示
- 优化算法

## scenario.json 输入文件

当前样例已经提供外部 JSON 场景输入文件：

    datasets/sample-small-warehouse/scenario.json

可以通过以下命令直接从该文件运行仿真：

    dotnet run --project src/Sim.Cli -- run-file datasets/sample-small-warehouse/scenario.json

该 JSON 文件描述了最小仓库场景的输入数据，包括：

    1. scenario_id 与 seed
    2. inbound receipt
    3. outbound order 与 inventory
    4. each-pick order 与 inventory
    5. 各流程资源数量
    6. 各流程处理时长
    7. expected 验收摘要

外部输入链路的 smoke 验收命令为：

    bash scripts/smoke-sample-warehouse-run-file.sh

该脚本会验证：

    1. run-file 输出与内置 sample 输出完全一致
    2. 样例关键指标符合预期
    3. event_log_text 包含稳定的关键事件
    4. 事件日志行数为 10
