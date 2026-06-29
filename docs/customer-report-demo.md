# Customer report demo

本文档说明如何从确定性仓库仿真输入生成客户可读 Markdown 报告。

## Demo goal

这个 demo 展示的是：

- scenario JSON -> RunArtifact
- baseline/candidate scenario JSON -> ComparisonArtifact
- RunArtifact + ComparisonArtifact -> customer Markdown report
- output deterministic and smoke-tested

## Inputs

- `datasets/sample-small-warehouse/scenario.json`
- `datasets/sample-small-warehouse/scenario-candidate.json`
- `datasets/sample-small-warehouse/artifacts/run-artifact.v1.json`
- `datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json`
- `datasets/sample-small-warehouse/artifacts/customer-report.v1.md`

## Reproduce the report from CLI

在仓库根目录执行：

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

## Inspect the golden report

可以直接打开：

```text
datasets/sample-small-warehouse/artifacts/customer-report.v1.md
```

报告应该包含：

- `# AI Warehouse Twin Report`
- `## Run Summary`
- `## Artifact Handoff`
- `## A/B Comparison Summary`
- `finished_at_ms`
- `total_work_item_throughput_per_hour`

## Validation

```bash
bash scripts/smoke-customer-report.sh
bash scripts/smoke-report-demo-docs.sh
bash scripts/check-all.sh
```

## Interview / customer talking points

- 这不是只跑一次 CLI，而是有 artifact contract、golden、smoke 和 CI guard 的可复现交付链路。
- RunArtifact 负责记录仿真运行事实，例如 KPI、event log、layout resources 和 position timeline。
- ComparisonArtifact 负责记录 baseline/candidate 的 A/B 指标差异。
- Customer report 是面向非工程用户的交付层，把运行事实和 A/B 差异整理成 Markdown。
- 所有 sample 输出都要求确定性，并通过字节级比较保护。
- 当前仍是最小仓库样例，不宣称完整 WMS、Unity、路径规划或真实设备控制。

## Boundaries

- 不包含完整 WMS。
- 不包含 Unity 可视化。
- 不包含路径规划。
- 不控制真实设备。
- LLM / AI 组件不能直接修改库存或控制真实设备。
