using System.Text;
using Sim.Contracts.Artifacts;

namespace Sim.Report;

public static class CustomerMarkdownReportRenderer
{
    private static readonly string[] KeyMetricNames =
    [
        "finished_at_ms",
        "total_work_item_throughput_per_hour",
    ];

    public static string Render(
        RunArtifact runArtifact,
        ComparisonArtifact comparisonArtifact)
    {
        return Render(
            runArtifact,
            comparisonArtifact,
            new CustomerReportRenderOptions());
    }

    public static string Render(
        RunArtifact runArtifact,
        ComparisonArtifact comparisonArtifact,
        CustomerReportRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(runArtifact);
        ArgumentNullException.ThrowIfNull(comparisonArtifact);
        ArgumentNullException.ThrowIfNull(options);

        var baselineMetrics = comparisonArtifact.Baseline.Metrics;
        var sb = new StringBuilder();

        void Line(string text = "") => sb.Append(text).Append('\n');

        Line("# AI Warehouse Twin Report");
        Line();

        Line("## Run Summary");
        Line();
        Line($"- Scenario: {runArtifact.ScenarioId}");
        Line($"- Finished at: {runArtifact.FinishedAtMs} ms");
        Line($"- Completed receipts: {baselineMetrics.CompletedReceipts}");
        Line($"- Completed outbound orders: {baselineMetrics.CompletedOutboundOrders}");
        Line($"- Completed each-pick orders: {baselineMetrics.CompletedEachPickOrders}");
        Line();

        Line("## Artifact Handoff");
        Line();
        Line($"- Layout resources: {runArtifact.Layout.Resources.Count}");
        Line($"- Position timeline entries: {runArtifact.PositionTimeline.Count}");
        Line($"- Event log entries: {runArtifact.EventLog.Count}");
        Line();

        Line("## A/B Comparison Summary");
        Line();
        Line($"- Baseline: {comparisonArtifact.Baseline.ScenarioId}");
        Line($"- Candidate: {comparisonArtifact.Candidate.ScenarioId}");
        Line();
        ComparisonMarkdownTableRenderer.RenderDeltaTable(
            Line,
            comparisonArtifact.Deltas.Where(delta =>
                KeyMetricNames.Contains(delta.MetricName, StringComparer.Ordinal)));
        Line();

        if (options.HasRunnerProvenance)
        {
            Line("## Runner Provenance");
            Line();
            Line($"- Run artifact runner: {options.RunRunnerMode}");
            Line($"- Comparison artifact runner: {options.ComparisonRunnerMode}");
            Line("- Provenance source: operator-provided render-report flags");

            if (!string.Equals(
                    options.RunRunnerMode,
                    options.ComparisonRunnerMode,
                    StringComparison.Ordinal))
            {
                Line();
                Line("Warning: Run artifact and comparison artifact were generated with different runner modes. KPI values may not be directly comparable.");
            }

            Line();
        }

        Line("## Notes");
        Line();
        Line("- This report is generated from deterministic simulation artifacts.");
        Line("- The comparison table reports objective numeric deltas only.");
        Line("- Layout coordinates are deterministic baseline positions, not a full warehouse map.");

        return sb.ToString();
    }
}
