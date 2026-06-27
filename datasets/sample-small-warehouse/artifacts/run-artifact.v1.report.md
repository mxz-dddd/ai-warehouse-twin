# 仿真运行报告 - sample-small-warehouse

> 本报告由一次确定性仿真运行产物 (run-artifact) 自动生成，供客户查看。

## 场景摘要

- 场景 ID: sample-small-warehouse
- 随机种子 (seed): 20240627
- 契约版本 (schema_version): run-artifact.v1
- 产物类型 (artifact_kind): warehouse-simulation-run

## 仿真时间

- 开始 (started_at_ms): 10 ms
- 结束 (finished_at_ms): 220 ms
- 仿真总时长 (total_duration_ms): 210 ms
- 最终世界时间 (final_world_time_ms): 220 ms

## 完成任务

- 完成工作项总数 (total_completed_work_items): 3

## 吞吐

注意：以下吞吐为按仿真时间线性换算的结果，不代表真实设备产能。

- 入库 receipt: 17142.857 / 小时
- 出库整箱 outbound: 17142.857 / 小时
- 拣选 each-pick: 17142.857 / 小时
- 合计 work item: 51428.571 / 小时

## 事件摘要

- 事件日志总行数 (event_log_line_count): 10
- inbound: 3 条
- outbound: 3 条
- each_pick: 4 条

## 当前限制

- 吞吐为按仿真时间换算，不代表真实产能。
- 当前仿真为最小聚合层，尚未建模跨流程库存共享、资源竞争、布局路径规划等。
- 本报告仅消费仿真产物 (run-artifact) 与数据契约，不参与仿真计算。
