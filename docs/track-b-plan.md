# 成员2 工程计划 · AI Warehouse Twin · Track B（产品 / 输入输出 / 可视化 · 消费侧）

> **本文件用途**：交给 **Claude** 阅读并执行。
> **你协助的人**：成员2（Windows 环境，项目新加入的伙伴）。
> **总目标不变**：AI Warehouse Twin 是面向真实仓库客户的创业产品。本 Track 负责"把输入产品化、把仿真产物变成客户能看的报告与画面"，服务于可校准、可视、可信度分级的评估闭环。
> **协作对象**：成员1（Mac，使用 GPT）拥有并深化仿真内核 `Sim.Core`，向你**导出 run-artifact 运行产物文件**。你**只消费产物与契约，绝不改他的内核**。

---

## 0. 给执行 AI（Claude）的总则（铁律，每个任务都遵守）

1. **你只消费、不生产仿真**：你的代码**只读 run-artifact 文件 + 只依赖 `Sim.Contracts`**，**禁止引用 `Sim.Core`**（有 CI 红线 `check-consumer-no-core` 守门）。
2. **你只在 Track B 的目录里写代码**（§3）；**绝不碰**成员1 的 `Sim.Core`/`Sim.Cli`/内核（§4）。
3. **契约只读+提议**：`Sim.Contracts` 是共管接缝；你可以提出修改，但**必须开 `CONTRACT-` PR 让成员1 一起评审**、bump `schema_version`，不可单方改。
4. **模块独立可测**：用成员1 提交的**固定 golden run-artifact 文件**做快照测试，不依赖他的仿真是否在跑。
5. **小步提交**：一个模块 = 一个分支 `app/<id>` = 一个 PR，含测试，跑绿 CI 才算完成。
6. **跨平台**：Windows 上开发，但代码/换行/路径必须跨平台（§7）；近期统一 .NET，不引入多余工具链。
7. **不做**：WMS 写回、设备直控；不要去"修"仿真逻辑（那是成员1 的 Track A）。

---

## 1. 项目基线（共享起点，2026-06-27）

```text
仓库:    github.com/mxz-dddd/ai-warehouse-twin   分支 main
质量:    dotnet build 0/0 · dotnet test 258 passed · CI success
成员1 已建成: 纯C# 确定性DES内核 + 三条最小流程 + warehouse 聚合 +
             外部 scenario.json 输入闭环 + WarehouseKpiSummary 核心模型
你将面对的缺口(正是你的活): 报告化 KPI、客户输入模板/校验、多订单数据集、以及全部可视化
```

> 你**不需要读懂 `Sim.Core` 内部**。你只需要熟悉两样东西：`Sim.Contracts` 契约 + 成员1 导出的 **run-artifact JSON 结构**。

---

## 2. 协作全景：两个成员 + 一条接缝

- **成员1（Track A，用 GPT）**：`Sim.Core`/`Sim.Cli` 深化 → **导出 run-artifact**。
- **成员2（你协助的人，本 Track B）**：读 run-artifact → 报告生成 / 输入校验 / Unity 可视化。
- **接缝（唯一协调点）**：`Sim.Contracts`（数据契约）+ **run-artifact**（一次仿真的完整产物 JSON）。
  ```
  成员1: 仿真 → 导出 run-artifact JSON → 提交到 datasets/**/artifacts/ 作 golden
  你:    读这些 JSON 文件做报告/校验/可视化, 只认契约, 不碰内核
  ```

---

## 3. 你的职责与拥有的模块（只在这些目录写代码）

| 模块 / 目录 | 职责 |
|---|---|
| `src/Sim.Report/` + `Sim.Report.Tests/` | 客户报告生成：run-artifact → Markdown/HTML 报告 |
| `src/Sim.Validation/` + `Sim.Validation.Tests/` | 输入校验：JSON Schema + 友好报错 + CSV/JSON 模板 |
| `datasets/`（**你主导**，成员1 校验可跑） | 多订单/多 SKU/多资源 场景 + `expected` 验收集 |
| `engine/unity/`（后期里程碑） | Unity 引擎可视化：播放器 / 2D 空间 / 布局设计器 / A/B 对比交付视图 |
| `src/Sim.Contracts/` | **只读 + 提议**（改动走 `CONTRACT-` PR 双评审） |

## 4. 你不可触碰的边界（成员1 的地盘）

```text
src/Sim.Core/      ← 成员1: 仿真内核 (你禁止引用)
src/Sim.Cli/       ← 成员1: 运行与导出
services/          ← 成员1: 后期 Python 优化/校准
```
> 若你需要 run-artifact 里多一个字段、或需要新场景能跑通，**通过 `CONTRACT-` PR 或 issue 找成员1**，不要自己去改内核。

---

## 5. 你的任务序列（按顺序，带验收）

> ⚠️ **开工前提（交接点 H0）**：必须等成员1 交付 `Sim.Contracts` v1 + 第一份 golden run-artifact（落在 `datasets/**/artifacts/`）。在那之前，先做 §8 的环境准备与契约研读。

### 第一波（H0 之后立刻可做，最解耦、最好测）
- **APP-010 报告生成器 `Sim.Report`**：读 run-artifact → 渲染**客户可读报告**（场景摘要 / 仿真时间 / 完成任务 / 吞吐 / 事件摘要 / 当前限制说明）。先 Markdown，后 HTML。
  - *验收*：`dotnet run --project src/Sim.Report -- <artifact.json> -o report.md`；**golden artifact → golden 报告**快照测试；吞吐标注"基于仿真时间换算，不代表真实产能"。
- **APP-020 输入校验 `Sim.Validation`**：JSON Schema + `schema_version`/必填/数量>0/时长≥0/状态枚举/expected 校验，**友好错误定位**；附 `scenario.template.json` + CSV 模板 + 字段说明文档。
  - *验收*：一组合法/非法样例 golden 用例全过；坏输入给出可读错误。
- **APP-030 多订单 golden 数据集**：在 `datasets/` 增加 多 SKU/多订单/多资源/多时段 场景 + `expected` 字段。
  - *验收*：成员1 的 `export-artifact` 能跑通该场景；`expected` 断言进 smoke/CI。

### 第二波（交接点 H1 之后：run-artifact 含 position/布局）
- **APP-040 Unity 工程脚手架 + 产物播放器**：Unity 6.3 工程；加载 run-artifact；**事件时间线播放器 + KPI 仪表盘**（先用现有 KPI，不需要空间）。
  - *验收*：Unity 载入 golden artifact，按时间轴回放事件、显示 KPI；对固定 artifact 的状态可断言（EditMode 测）。
- **APP-050 2D 俯视空间可视化**：用 artifact 的 layout+position 画区域/货架/通道/月台，人/货/车按状态插值移动；热力图。
  - *验收*：实体位置与 artifact 一致；热力与统计一致。

### 第三波（交接点 H2 之后：A/B 双产物）
- **APP-060 布局设计器**：拖拽编辑区域/货架/通道/月台导出 `layout.json`（回交成员1 跑仿真）。
  - *验收*：编辑→导出→成员1 可重跑。
- **APP-070 A/B 对比交付视图**：分屏读两份 run-artifact，呈现"现状 vs 优化"+ 改善% + 导出报告/短片（v2 §9.5）。
  - *验收*：A/B 视图读两份 artifact 正确呈现改善%。

---

## 6. 交接节点（你"收到"什么 / 你"给出"什么）★

**你从成员1 收到（你在等这些，收到才能开工对应模块）：**
- **H0**：`Sim.Contracts` v1 + `RunArtifact` schema + `export-artifact` 命令 + 1 份 golden artifact 入库。→ 解锁 APP-010/020/030。
- **H1**：run-artifact 内含 `position` + 布局信息。→ 解锁 APP-050 空间可视化。
- **H2**：A/B 双 run-artifact。→ 解锁 APP-070 对比视图。

**你交付给成员1（产出）：**
- 多订单场景 + `expected`（`datasets/`，APP-030）→ 他 `export-artifact` 跑通并纳入 smoke。
- 输入 JSON Schema / 模板（APP-020）→ 他的 CLI 可调用校验。
- 设计器导出的 `layout.json`（APP-060）→ 他用于跑仿真。

**契约变更协议**：你需要产物多字段时，开 `CONTRACT-` PR，**成员1 一起评审**，bump `schema_version`，`check-contract-drift` 兜底。**不要单方改契约或去改内核。**

---

## 7. 工程约定（Windows 跨平台 / 测试 / Git）

- **环境**：装 **.NET 8 SDK**（`global.json` 已锁版本）+ **Git for Windows（含 Git Bash）**；后期 APP-040 起装 **Unity 6.3 LTS**。
- **跨平台**：遵守根目录 `.gitattributes`（LF 换行，避免 CRLF 冲突）；C# 用 `Path.Combine`，文件名注意大小写（Linux CI 区分大小写）；`*.sh` smoke 用 Git Bash 跑，或用项目提供的 `dotnet`/`python` 跨平台入口。
- **测试**：对 `datasets/**/artifacts/*.json` 固定样本做**快照测试**（报告快照 / 校验用例 / 可视化状态断言），完全不依赖仿真运行。
- **CI 红线**：`check-consumer-no-core`（你的 Report/Validation 不得引用 Sim.Core）必须 PASS；Windows/Ubuntu 双 job 都要绿。
- **Unity（APP-040 起）**：`.gitignore` 屏蔽 `Library/ Temp/ Obj/ Build/ Logs/`；二进制资源用 **Git LFS**；场景文件用 `unityyamlmerge` 减少冲突。
- **Git**：`main` 保护、必须绿 CI；分支 `app/<id>`；PR squash。CODEOWNERS：`/src/Sim.Report/ /src/Sim.Validation/ /engine/ @你`，`/src/Sim.Contracts/ @你 @成员1`。

---

## 8. 立即开始（给 Claude 的执行顺序）

1. **拉环境**：装 .NET 8 SDK + Git；`git clone`，本机 `dotnet build` / `dotnet test` 跑通（**验证 Windows 跨平台 OK**，这本身是第一项有价值的检查）。
2. **研读接缝**：读 `src/Sim.Contracts` 与 `docs/`，弄懂 `RunArtifact` JSON 结构即可——**不用读 `Sim.Core` 内部**。
3. **等 H0**：成员1 一旦把 golden run-artifact 提交入库，立刻开 **APP-010 报告生成器**（最易上手、最解耦）。
4. 然后 **APP-020 校验 → APP-030 数据集**；待 **H1** 到来再进 **APP-040/050** Unity。

## 9. 任务卡模板（Claude 执行每个模块时套用）

```md
## Task: <APP-xxx 标题>
目标(1-2句): <做什么、为何>
涉及目录: src/Sim.Report|Sim.Validation|datasets|engine/unity (只在 Track B 目录内)
前置: <依赖的交接点 H0/H1/H2 或前序 APP 卡>
要点: <只读 run-artifact + 依赖 Sim.Contracts; 禁止引用 Sim.Core; 只动本卡文件>
验收命令(可机器验证):
 - [ ] dotnet build 0/0 且 dotnet test 全绿
 - [ ] 针对 golden artifact 的快照/用例通过
 - [ ] check-consumer-no-core PASS
不在本卡范围: <成员1 的内核 / 后续卡>
回交影响: <是否产出 datasets/schema/layout.json 给成员1>
```

---

*Track B 的使命：只认"契约 + run-artifact 文件"，把它变成真实仓库客户能看的报告与画面，并为后续可视化、校准可信度和 WMS 试点提供交付层。绝不碰内核——这正是两人能并行、各自可测、互不打架的关键。*
