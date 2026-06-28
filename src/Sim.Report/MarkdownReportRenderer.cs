using System.Globalization;
using System.Text;
using Sim.Contracts.Artifacts;

namespace Sim.Report;

public static class MarkdownReportRenderer
{
    public static string Render(RunArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var kpi = artifact.KpiSummary;
        var sb = new StringBuilder();

        void Line(string text = "") => sb.Append(text).Append('\n');

        Line($"# 仿真运行报告 - {artifact.ScenarioId}");
        Line();
        Line("> 本报告由一次确定性仿真运行产物 (run-artifact) 自动生成，供客户查看。");
        Line();

        Line("## 场景摘要");
        Line();
        Line($"- 场景 ID: {artifact.ScenarioId}");
        Line($"- 随机种子 (seed): {artifact.Seed}");
        Line($"- 契约版本 (schema_version): {artifact.SchemaVersion}");
        Line($"- 产物类型 (artifact_kind): {artifact.ArtifactKind}");
        Line();

        Line("## 仿真时间");
        Line();
        Line($"- 开始 (started_at_ms): {artifact.StartedAtMs} ms");
        Line($"- 结束 (finished_at_ms): {artifact.FinishedAtMs} ms");
        Line($"- 仿真总时长 (total_duration_ms): {kpi.TotalDurationMs} ms");
        Line($"- 最终世界时间 (final_world_time_ms): {artifact.FinalWorldTimeMs} ms");
        Line();

        Line("## 完成任务");
        Line();
        Line($"- 完成工作项总数 (total_completed_work_items): {kpi.TotalCompletedWorkItems}");
        Line();

        Line("## 吞吐");
        Line();
        Line("注意：以下吞吐为按仿真时间线性换算的结果，不代表真实设备产能。");
        Line();
        Line($"- 入库 receipt: {FormatDecimal(kpi.ReceiptThroughputPerHour)} / 小时");
        Line($"- 出库整箱 outbound: {FormatDecimal(kpi.OutboundOrderThroughputPerHour)} / 小时");
        Line($"- 拣选 each-pick: {FormatDecimal(kpi.EachPickOrderThroughputPerHour)} / 小时");
        Line($"- 合计 work item: {FormatDecimal(kpi.TotalWorkItemThroughputPerHour)} / 小时");
        Line();

        Line("## 事件摘要");
        Line();
        Line($"- 事件日志总行数 (event_log_line_count): {kpi.EventLogLineCount}");
        Line();

        Line("## 当前限制");
        Line();
        Line("- 吞吐为按仿真时间换算，不代表真实产能。");
        Line("- 当前仿真为最小聚合层，尚未建模跨流程库存共享、资源竞争、布局路径规划等。");
        Line("- 本报告仅消费仿真产物 (run-artifact) 与数据契约，不参与仿真计算。");

        return sb.ToString();
    }

    private static string FormatDecimal(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero)
            .ToString("0.###", CultureInfo.InvariantCulture);
    }
}
