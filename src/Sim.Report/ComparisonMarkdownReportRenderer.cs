using System.Globalization;
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
        RenderDeltaTable(
            Line,
            artifact.Deltas.Where(delta =>
                KeyMetricNames.Contains(delta.MetricName, StringComparer.Ordinal)));
        Line();

        Line("## All Deltas");
        Line();
        RenderDeltaTable(Line, artifact.Deltas);

        return sb.ToString();
    }

    private static void RenderDeltaTable(
        Action<string> line,
        IEnumerable<ComparisonArtifactDelta> deltas)
    {
        line("| Metric | Baseline | Candidate | Delta | Delta % | Direction |");
        line("|---|---:|---:|---:|---:|---|");

        foreach (var delta in deltas)
        {
            line(
                $"| {delta.MetricName} | {FormatDecimal(delta.BaselineValue)} | {FormatDecimal(delta.CandidateValue)} | {FormatDecimal(delta.Delta)} | {FormatDeltaPercent(delta.DeltaPercent)} | {delta.Direction} |");
        }
    }

    private static string FormatDeltaPercent(decimal? value)
    {
        return value is null
            ? "n/a"
            : FormatDecimal(value.Value);
    }

    private static string FormatDecimal(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero)
            .ToString("0.###", CultureInfo.InvariantCulture);
    }
}
