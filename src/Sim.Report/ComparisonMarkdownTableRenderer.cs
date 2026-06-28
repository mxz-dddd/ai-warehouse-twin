using System.Globalization;
using Sim.Contracts.Artifacts;

namespace Sim.Report;

internal static class ComparisonMarkdownTableRenderer
{
    public static void RenderDeltaTable(
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
