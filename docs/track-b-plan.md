# 成员2工程计划 · Track B：R4 引擎可视化轨道

Track B 当前主线：R4 引擎可视化轨道。

AI Warehouse Twin 是面向真实仓库客户的创业产品，不是展示包装优先。Track B 的使命是把已经冻结或正在演进的 artifact / contracts / docs 消费成真实仓库客户可视化交付面，包括引擎可视化、artifact handoff 检查、客户报告展示层和 A/B 可视化交付视图。

成员2负责引擎可视化、artifact 消费、客户报告展示层、A/B 可视化交付视图。

成员2当前不再负责输入校验、JSON template、schema validation、contract schema、Sim.Validation 主体实现。这些内容已经由主线完成或部分失效，只能作为 historical / completed / no longer primary track 记录，不能继续作为 Track B 当前主线。

---

## 1. 当前硬边界

成员2近期只消费 artifact / contracts / docs，不修改仿真内核、契约或 golden artifact。

当前禁止：

- 不改 `Sim.Core`。
- 不改 `Sim.Contracts`。
- 不改 `packages/contracts`。
- 不改 artifact golden。
- 不改 validation 主体逻辑。
- 不把当前 position timeline 做成真实移动动画。
- 不宣传“人/叉车/货物在动”。
- 不声称 calibration / optimization / WMS / confidence grading 已实现。

当前 `RunArtifact v1` 的 layout resources 与 position timeline 只能表示 baseline layout positions, NOT simulated movement。它们是 deterministic layout handoff，不是由路径规划或移动仿真产生的真实 movement trace。

R4 real movement visualization depends on R2 movement-driven RunArtifact. 在 R2 完成真实路径移动、PathGraph / A* 或等价 movement-driven RunArtifact 之前，成员2只能做静态 layout handoff、资源位置检查、artifact player 输入边界设计和 A/B view 数据需求设计。

---

## 2. 当前可做的 R0/R4 前置工作

成员2现在可以做的工作是可视化交付面的准备，不是重新实现主线已经完成的输入校验或 schema 工作。

- 阅读 `RunArtifact v1`，理解 layout resources、position timeline、KPI、event log 和 schema version。
- 阅读 `ComparisonArtifact v1`，理解 baseline / candidate / deltas 的客户交付语义。
- 阅读 `customer-report.v1.md`，理解当前 customer Markdown report 的已实现输出。
- 阅读 `docs/architecture/contracts-v1-freeze.md`，理解 contracts-v1-freeze 后的契约治理边界。
- 熟悉 baseline layout positions, NOT simulated movement 的限制，并在任何可视化设计里显式保留这个边界。
- 设计 Unity 2D artifact player 的输入边界：只读 artifact / contracts，不读 `Sim.Core`。
- 设计 Static layout handoff viewer：展示静态布局、资源 baseline position、artifact handoff 完整性。
- 设计 A/B comparison view 的数据需求清单：只列出需要从 `ComparisonArtifact v1` 或未来 contract 读取的字段，不直接改 contract。
- 明确 R2 之后才能做真实移动插值；R2 之前不得把 position timeline 插值成 movement animation。

---

## 3. 近期 Track B 任务重排

下面是当前 Track B 的建议任务顺序。它们只是成员2任务表，不在本 PR 中创建 Unity 工程、脚本或实现。

### VIS-PREP-001：Artifact contract reading checklist

目标：建立成员2读取 artifact / contracts 的 checklist。

范围：

- `RunArtifact v1`
- `ComparisonArtifact v1`
- `customer-report.v1.md`
- `contracts-v1-freeze.md`
- baseline layout positions, NOT simulated movement 边界

验收：形成可视化输入字段清单，标注已实现字段、planned 字段和不得误用的字段。

### VIS-PREP-002：Unity 2D player input boundary design

目标：定义 Unity 2D artifact player 只能读取哪些输入。

范围：

- artifact file path
- schema version
- layout resources
- position timeline 的 baseline layout handoff 语义
- event log / KPI / comparison deltas

验收：明确 Unity player 不引用 `Sim.Core`，不修改 contracts，不基于当前 v1 position timeline 做真实移动动画。

### VIS-PREP-003：Static layout handoff viewer plan

目标：设计静态 layout handoff viewer。

范围：

- 静态仓库布局展示
- 资源 baseline position 展示
- artifact handoff 完整性检查
- 对 position timeline 的 warning / honesty label

验收：viewer 文案明确 baseline layout positions, NOT simulated movement，不出现“人/叉车/货物在动”的表达。

### VIS-PREP-004：A/B comparison view data requirement

目标：为 A/B comparison view 建立数据需求清单。

范围：

- baseline / candidate KPI
- deltas
- customer-facing summary
- future visualization requirements that may require a later `CONTRACT-` proposal

验收：只消费 `ComparisonArtifact v1` 已有字段；如需新字段，只提出 `CONTRACT-` proposal，不直接修改 contracts。

### VIS-PREP-005：R2 movement dependency checklist

目标：明确 R4 真实移动可视化依赖哪些 R2 产物。

范围：

- movement-driven RunArtifact
- PathGraph / A* 或等价路径模型输出
- resource lease trace 与 position trace 对齐规则
- 真实 movement trace 与当前 deterministic layout handoff 的差异

验收：R4 真正开始前，必须确认 R2 movement-driven RunArtifact 已落地并有 tests / golden / CI guard。

---

## 4. Historical / completed / no longer primary track

以下内容曾经属于 Track B 早期设想，但现在不再是成员2当前主线。

- APP-010 customer report：主线已完成 customer Markdown report，Track B 后续只围绕报告展示层和可视化交付面消费现有 report / artifact。
- APP-020 输入校验、JSON template、schema validation、Sim.Validation 主体实现：historical / completed / no longer primary track。成员2当前不继续以输入校验为主线。
- APP-030 多订单 dataset：historical / completed / no longer primary track。后续如需可视化专用样例，必须走独立任务卡，且不得修改 artifact golden 除非任务明确授权。
- contract schema：contracts-v1-freeze 后属于治理边界；成员2只读或提出 `CONTRACT-` proposal，不单方修改。

这些历史任务不能被重新包装成当前 Track B 主线；当前主线是 R4 引擎可视化轨道。

---

## 5. 协作协议

- Track A / Core 继续拥有 `Sim.Core`、仿真逻辑、scenario runner、RunArtifact 生产逻辑。
- Track B / Visualization 只消费 artifact / contracts / docs，准备真实仓库客户可视化交付面。
- 任何 contract 字段新增或 schema version 变化都必须走 dedicated `CONTRACT-` PR。
- 任何 golden artifact 变化都必须由任务卡显式授权。
- 当前 position timeline 不是 movement trace；R2 之前不得基于它做移动动画或客户宣传。
- calibration、optimization、WMS、confidence grading 都是 planned / not yet implemented，不能写成已实现。

Track B 的成功标准不是展示包装优先，而是帮助真实仓库客户理解 artifact-backed deterministic simulation 的结果，并为 R2 之后的真实 movement-driven RunArtifact 和 R4 引擎可视化建立可信交付面。
