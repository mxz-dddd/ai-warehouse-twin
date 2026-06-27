# CLAUDE.md
@AGENTS.md

## 这条线
你在协助成员2,负责 Track B(产品/输入输出/可视化,消费侧),Windows 环境。
完整任务序列见 docs/track-b-plan.md(参考,不必每次全读)。

## 铁律
- 只读 run-artifact + 只依赖 Sim.Contracts;禁止引用 Sim.Core(CI: check-consumer-no-core 必须 PASS)。
- 只在 Track B 目录写代码:src/Sim.Report、src/Sim.Validation、datasets、engine/unity。绝不碰 src/Sim.Core、src/Sim.Cli。
- 契约只读+提议:要改走 CONTRACT- PR、找成员1 双评审,不单方改、不改内核。
- 一张卡 = 一个分支 app/<id> = 一个 PR,含测试。

## 完成判据(每张卡都要满足)
- dotnet build 0/0;dotnet test 全绿
- 针对 golden artifact 的快照/用例通过
- ./scripts/check-no-unityengine.sh 与 check-consumer-no-core PASS

## Windows 注意
- .sh 脚本用 Git Bash 跑;遵守 .gitattributes(LF);路径用 Path.Combine,注意大小写。
