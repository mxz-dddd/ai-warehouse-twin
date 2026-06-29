# Each-pick 最小确定性样例

本文档说明当前 smoke-tested sample scenario 的运行与验收方式。它是后续 pick、pack 和资源扩展的基础，不代表完整 WMS 或完整订单履约系统。

## 当前能力

Each-pick 最小闭环使用以下入口：

- Sample scenario：`datasets/sample-each-pick/scenario.json`
- Sample smoke：`scripts/smoke-sample-each-pick.sh`
- Artifact smoke：`scripts/smoke-each-pick-export-artifact.sh`

固定输入的当前结果为：

```text
orders released: 1
orders completed: 1
units picked: 5
events: 4
sim completed: true
```

事件链覆盖订单释放、到达工作站、拣选完成和移至暂存区。固定 seed、输入与代码版本相同时，事件日志和 artifact 输出保持确定。

## 运行 sample smoke

在仓库根目录执行：

```bash
bash scripts/smoke-sample-each-pick.sh
```

期望输出包含：

```text
PASS: sample each-pick scenario ran
orders released: 1
orders completed: 1
units picked: 5
events: 4
sim completed: true
```

该脚本通过现有 `run-file` CLI 入口读取 sample scenario，并检查完成数量、拣选数量、事件数量、完成时间和关键 each-pick 事件。

## 运行 artifact smoke

执行：

```bash
bash scripts/smoke-each-pick-export-artifact.sh
```

该脚本会：

1. 使用 `export-artifact` 连续导出两份 each-pick artifact。
2. 使用 `cmp` 验证两份文件字节级一致。
3. 检查 RunArtifact v1 的场景信息、KPI summary 和 4 条 each-pick 事件。
4. 检查 completed orders、units picked 和 event count。
5. 仅在临时目录写入输出，不修改现有 warehouse golden artifact。

## 完整本地验证

运行统一的本地验收入口：

```bash
bash scripts/check-all.sh
```

该脚本覆盖 UnityEngine 引用检查、contract drift、consumer dependency boundary（消费侧项目不得引用 `Sim.Core`）、build/test、each-pick sample smoke、each-pick artifact smoke、warehouse smoke、export artifact smoke、H2 comparison artifact smoke，以及 warehouse、each-pick 和 comparison artifact 的字节级确定性检查。该入口既用于本地开发，也由 Linux CI 的 full validation 步骤调用。

H2 comparison artifact smoke 可单独运行：

```bash
bash scripts/smoke-comparison-artifact.sh
```

该脚本验证 `compare-files` 输出的 comparison JSON 可重复生成、与 golden artifact 字节一致，并包含 `comparison_artifact.v1`、baseline、candidate、deltas 和关键指标字段。

也可以在仓库根目录逐项执行：

```bash
./scripts/check-no-unityengine.sh
./scripts/check-contract-drift.sh
dotnet build
dotnet test
bash scripts/smoke-sample-each-pick.sh
bash scripts/smoke-each-pick-export-artifact.sh
bash scripts/smoke-sample-warehouse.sh
bash scripts/smoke-sample-warehouse-run-file.sh
bash scripts/smoke-export-artifact.sh
bash scripts/smoke-comparison-artifact.sh
bash scripts/smoke-customer-report.sh
bash scripts/smoke-report-demo-docs.sh
```

## 已知限制

- RunArtifact v1 当前不含 picked quantity；artifact smoke 通过 `run-file` 输出交叉验证 `units picked = 5`。
- Tote 由 runner 在订单释放时按确定性规则创建，不是 JSON 输入字段。
- 当前 each-pick sample 是 minimal deterministic each-pick sample，不包含完整仓储波次、路径规划或人机资源调度模型。
- 当前流程不涉及 Unity 可视化逻辑，也不表示 Unity 已支持 each-pick。
