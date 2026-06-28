# 仓储场景输入模板与字段说明（warehouse-scenario.v0）

本目录提供**客户填写仿真场景输入**所需的全部材料：

| 文件 | 用途 |
|---|---|
| `scenario.template.json` | 可直接通过校验的最小场景模板，复制改填即可 |
| `scenario.schema.json` | JSON Schema（Draft 2020-12），供编辑器/工具做结构校验 |
| `receipts.inbound.template.csv` | 入库收货明细的 CSV 模板 |
| `orders.outbound.template.csv` | 出库订单明细的 CSV 模板 |
| `orders.each-pick.template.csv` | 拣选订单明细的 CSV 模板 |
| `inventory.template.csv` | 库存明细的 CSV 模板 |

> CSV 模板用于在 Excel 等工具里批量整理明细；整理完后，把每个 CSV 的每一行誊到 `scenario.json` 里对应数组（`receipts` / `orders` / `inventory`）的一个对象。CSV 列名与 JSON 字段名一一对应。

## 如何校验

```bash
# 文本报错（人可读，适合自己排查）
dotnet run --project src/Sim.Validation -- datasets/templates/scenario.template.json

# JSON 报错（机器可读，适合被其他程序调用）
dotnet run --project src/Sim.Validation -- <你的 scenario.json> --format json
```

退出码：`0` = 校验通过，`1` = 校验未通过，`2` = 用法或读取文件错误。

校验**只检查输入本身是否合法**（结构、必填、取值范围、状态枚举、`expected` 形状），不运行仿真，也不和真实运行产物（run-artifact）做结果比对。

## 顶层字段

| 字段 | 类型 | 必填 | 规则 | 示例 |
|---|---|---|---|---|
| `schema_version` | 文本 | 是 | 必须为 `warehouse-scenario.v0` | `"warehouse-scenario.v0"` |
| `scenario_id` | 文本 | 是 | 非空 | `"my-scenario-001"` |
| `seed` | 整数 | 是 | ≥ 0；保证仿真可复现 | `12345` |
| `description` | 文本 | 否 | 自由说明 | `"双十一压测场景"` |
| `inbound` | 对象 | 否 | 入库流程；见下 | — |
| `outbound` | 对象 | 否 | 出库整箱流程；见下 | — |
| `each_pick` | 对象 | 否 | 拣选流程；见下 | — |
| `expected` | 对象 | 否 | 期望验收摘要；见下 | — |

> `inbound` / `outbound` / `each_pick` **至少出现一个**，否则没有任何作业可以仿真。

## inbound（入库流程）

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `dock_count` | 整数 | 是 | ≥ 1 |
| `forklift_count` | 整数 | 是 | ≥ 1 |
| `process.unload_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `process.qc_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `process.putaway_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `receipts[]` | 列表 | 是 | 至少一条 |

**receipts[] 每条收货**（对应 `receipts.inbound.template.csv` 的一行）：

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `receipt_id` | 文本 | 是 | 非空 |
| `warehouse_id` | 文本 | 是 | 非空 |
| `sku_id` | 文本 | 是 | 非空 |
| `quantity` | 整数 | 是 | > 0 |
| `staging_location_id` | 文本 | 是 | 非空 |
| `storage_location_id` | 文本 | 是 | 非空 |
| `arrives_at_ms` | 整数 | 是 | ≥ 0（毫秒） |

## outbound（出库整箱流程）

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `worker_count` | 整数 | 是 | ≥ 1 |
| `dock_count` | 整数 | 是 | ≥ 1 |
| `process.pick_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `process.stage_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `process.dock_travel_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `process.load_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `inventory[]` | 列表 | 是 | 至少一条；见“库存明细” |
| `orders[]` | 列表 | 是 | 至少一条 |

**orders[] 每条出库订单**（对应 `orders.outbound.template.csv` 的一行）：

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `order_id` | 文本 | 是 | 非空 |
| `warehouse_id` | 文本 | 是 | 非空 |
| `sku_id` | 文本 | 是 | 非空 |
| `quantity` | 整数 | 是 | > 0 |
| `pick_location_id` | 文本 | 是 | 非空 |
| `staging_location_id` | 文本 | 是 | 非空 |
| `dock_id` | 文本 | 是 | 非空 |
| `released_at_ms` | 整数 | 是 | ≥ 0（毫秒） |

## each_pick（拣选流程）

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `station_count` | 整数 | 是 | ≥ 1 |
| `worker_count` | 整数 | 是 | ≥ 1 |
| `process.tote_bind_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `process.travel_to_station_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `process.pick_service_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `process.move_to_staging_duration_ms` | 整数 | 是 | ≥ 0（毫秒） |
| `inventory[]` | 列表 | 是 | 至少一条；见“库存明细” |
| `orders[]` | 列表 | 是 | 至少一条 |

**orders[] 每条拣选订单**（对应 `orders.each-pick.template.csv` 的一行）：

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `order_id` | 文本 | 是 | 非空 |
| `warehouse_id` | 文本 | 是 | 非空 |
| `sku_id` | 文本 | 是 | 非空 |
| `quantity` | 整数 | 是 | > 0 |
| `pick_face_location_id` | 文本 | 是 | 非空 |
| `pick_station_id` | 文本 | 是 | 非空 |
| `staging_location_id` | 文本 | 是 | 非空 |
| `released_at_ms` | 整数 | 是 | ≥ 0（毫秒） |

## 库存明细 inventory[]（outbound 与 each_pick 通用）

对应 `inventory.template.csv` 的一行：

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `inventory_id` | 文本 | 是 | 非空 |
| `sku_id` | 文本 | 是 | 非空 |
| `quantity` | 整数 | 是 | > 0 |
| `location_id` | 文本 | 是 | 非空 |
| `status` | 文本 | 是 | 见下方枚举 |

### status 取值

允许：`expected`、`received`、`qc_hold`、`available`、`allocated`、`picking`、`picked`、`consolidating`、`staged`、`loaded`、`shipped`。

作为输入，库存通常填 `available`（可用）。该枚举取自共享领域契约 `packages/contracts/domain/inventory_unit.schema.json` 的状态集合（小写形式）；仿真 run-file 解析器实际接受的子集以成员1 确认为准。

## expected（期望验收，可选）

若提供，每个字段都必须是 **≥ 0 的整数**（只校验形状，不与真实运行结果比对）：

`started_at_ms`、`finished_at_ms`、`completed_receipts`、`completed_outbound_orders`、`completed_each_pick_orders`、`total_quantity_available`、`total_quantity_shipped`、`total_quantity_picked`、`final_world_time_ms`、`event_log_line_count`。

## scenario.schema.json 与校验器的关系

`scenario.schema.json` 是可移植的结构契约（编辑器、CI、其他语言的 JSON Schema 校验器都能用）。`src/Sim.Validation` 里的 C# 校验器实现**同一套规则**，并额外给出中文、可定位、一次列全的友好报错——以校验器的报错为准。两者保持一致。
