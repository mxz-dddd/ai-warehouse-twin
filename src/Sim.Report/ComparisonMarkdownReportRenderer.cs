using System.Text;
using Sim.Contracts.Artifacts;

namespace Sim.Report;

public static class ComparisonMarkdownReportRenderer
{
    private static readonly string[] KeyMetricNames =
    [
        "finished_at_ms",
        "total_work_item_throughput_per_hour",
    ];

    public static string Render(ComparisonArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var sb = new StringBuilder();

        void Line(string text = "") => sb.Append(text).Append('\n');

        Line("# Warehouse Scenario Comparison");
        Line();
        Line("## Scenarios");
        Line();
        Line($"- Baseline: {artifact.Baseline.ScenarioId}");
        Line($"- Candidate: {artifact.Candidate.ScenarioId}");
        Line();

        Line("## Key Metrics");
        Line();
        ComparisonMarkdownTableRenderer.RenderDeltaTable(
            Line,
            artifact.Deltas.Where(delta =>
                KeyMetricNames.Contains(delta.MetricName, StringComparer.Ordinal)));
        Line();

        Line("## All Deltas");
        Line();
        ComparisonMarkdownTableRenderer.RenderDeltaTable(Line, artifact.Deltas);

        return sb.ToString();
    }
}
