using Sim.Contracts.Artifacts;
using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public sealed class ComparisonMarkdownReportRendererTests
{
    [Fact]
    public void ComparisonMarkdownReportRenderer_RendersScenarioIds()
    {
        var artifact = LoadArtifact();
        var report = ComparisonMarkdownReportRenderer.Render(artifact);

        Assert.Contains("# Warehouse Scenario Comparison", report);
        Assert.Contains(artifact.Baseline.ScenarioId, report);
        Assert.Contains(artifact.Candidate.ScenarioId, report);
    }

    [Fact]
    public void ComparisonMarkdownReportRenderer_RendersKeyDeltas()
    {
        var report = ComparisonMarkdownReportRenderer.Render(LoadArtifact());

        Assert.Contains("finished_at_ms", report);
        Assert.Contains("total_work_item_throughput_per_hour", report);
        Assert.Contains("decrease", report);
        Assert.Contains("increase", report);
    }

    [Fact]
    public void ComparisonMarkdownReportRenderer_IsDeterministic()
    {
        var artifact = LoadArtifact();

        var first = ComparisonMarkdownReportRenderer.Render(artifact);
        var second = ComparisonMarkdownReportRenderer.Render(artifact);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ComparisonMarkdownReportRenderer_RendersNullDeltaPercentAsNotApplicable()
    {
        var artifact = new ComparisonArtifact(
            ComparisonArtifact.CurrentSchemaVersion,
            new ComparisonArtifactScenarioSummary(
                "baseline",
                Metrics()),
            new ComparisonArtifactScenarioSummary(
                "candidate",
                Metrics()),
            [
                new ComparisonArtifactDelta(
                    "zero_baseline_metric",
                    baselineValue: 0m,
                    candidateValue: 10m,
                    delta: 10m,
                    deltaPercent: null,
                    direction: "increase"),
            ]);

        var report = ComparisonMarkdownReportRenderer.Render(artifact);

        Assert.Contains("| zero_baseline_metric | 0 | 10 | 10 | n/a | increase |", report);
    }

    private static ComparisonArtifact LoadArtifact()
    {
        return ComparisonArtifactLoader.Load(TestPaths.ComparisonArtifactPath());
    }

    private static ComparisonArtifactMetrics Metrics()
    {
        return new ComparisonArtifactMetrics(
            finishedAtMs: 0,
            completedReceipts: 0,
            completedOutboundOrders: 0,
            completedEachPickOrders: 0,
            totalQuantityReceived: 0m,
            totalQuantityShipped: 0m,
            totalQuantityPicked: 0m,
            inboundReceiptThroughputPerHour: 0m,
            outboundOrderThroughputPerHour: 0m,
            eachPickOrderThroughputPerHour: 0m,
            totalWorkItemThroughputPerHour: 0m);
    }
}
