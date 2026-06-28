using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public sealed class CustomerMarkdownReportRendererTests
{
    [Fact]
    public void CustomerMarkdownReportRenderer_RendersRunSummary()
    {
        var runArtifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());
        var comparisonArtifact = ComparisonArtifactLoader.Load(TestPaths.ComparisonArtifactPath());

        var report = CustomerMarkdownReportRenderer.Render(
            runArtifact,
            comparisonArtifact);

        Assert.Contains("# AI Warehouse Twin Report", report);
        Assert.Contains("## Run Summary", report);
        Assert.Contains($"- Scenario: {runArtifact.ScenarioId}", report);
        Assert.Contains($"- Finished at: {runArtifact.FinishedAtMs} ms", report);
        Assert.Contains(
            $"- Completed receipts: {comparisonArtifact.Baseline.Metrics.CompletedReceipts}",
            report);
        Assert.Contains(
            $"- Completed outbound orders: {comparisonArtifact.Baseline.Metrics.CompletedOutboundOrders}",
            report);
        Assert.Contains(
            $"- Completed each-pick orders: {comparisonArtifact.Baseline.Metrics.CompletedEachPickOrders}",
            report);
    }

    [Fact]
    public void CustomerMarkdownReportRenderer_RendersExpectedSampleReportSections()
    {
        var runArtifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());
        var comparisonArtifact = ComparisonArtifactLoader.Load(TestPaths.ComparisonArtifactPath());

        var report = CustomerMarkdownReportRenderer.Render(
            runArtifact,
            comparisonArtifact);

        Assert.Contains("# AI Warehouse Twin Report", report);
        Assert.Contains("## Run Summary", report);
        Assert.Contains("## Artifact Handoff", report);
        Assert.Contains("## A/B Comparison Summary", report);
        Assert.Contains("finished_at_ms", report);
        Assert.Contains("total_work_item_throughput_per_hour", report);
    }

    [Fact]
    public void CustomerMarkdownReportRenderer_RendersArtifactHandoffSummary()
    {
        var runArtifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());
        var comparisonArtifact = ComparisonArtifactLoader.Load(TestPaths.ComparisonArtifactPath());

        var report = CustomerMarkdownReportRenderer.Render(
            runArtifact,
            comparisonArtifact);

        Assert.Contains("## Artifact Handoff", report);
        Assert.Contains($"- Layout resources: {runArtifact.Layout.Resources.Count}", report);
        Assert.Contains($"- Position timeline entries: {runArtifact.PositionTimeline.Count}", report);
        Assert.Contains($"- Event log entries: {runArtifact.EventLog.Count}", report);
    }

    [Fact]
    public void CustomerMarkdownReportRenderer_RendersComparisonSummary()
    {
        var runArtifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());
        var comparisonArtifact = ComparisonArtifactLoader.Load(TestPaths.ComparisonArtifactPath());

        var report = CustomerMarkdownReportRenderer.Render(
            runArtifact,
            comparisonArtifact);

        Assert.Contains("## A/B Comparison Summary", report);
        Assert.Contains($"- Baseline: {comparisonArtifact.Baseline.ScenarioId}", report);
        Assert.Contains($"- Candidate: {comparisonArtifact.Candidate.ScenarioId}", report);
        Assert.Contains("finished_at_ms", report);
        Assert.Contains("total_work_item_throughput_per_hour", report);
        Assert.Contains("decrease", report);
        Assert.Contains("increase", report);
    }

    [Fact]
    public void CustomerMarkdownReportRenderer_IsDeterministic()
    {
        var runArtifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());
        var comparisonArtifact = ComparisonArtifactLoader.Load(TestPaths.ComparisonArtifactPath());

        var first = CustomerMarkdownReportRenderer.Render(
            runArtifact,
            comparisonArtifact);
        var second = CustomerMarkdownReportRenderer.Render(
            runArtifact,
            comparisonArtifact);

        Assert.Equal(first, second);
    }

    [Fact]
    public void CustomerReportService_RenderFromFiles_LoadsAndRenders()
    {
        var report = CustomerReportService.RenderFromFiles(
            TestPaths.ArtifactPath(),
            TestPaths.ComparisonArtifactPath());

        Assert.Contains("# AI Warehouse Twin Report", report);
        Assert.Contains("## Run Summary", report);
        Assert.Contains("## Artifact Handoff", report);
        Assert.Contains("## A/B Comparison Summary", report);
    }

    [Fact]
    public void CustomerReport_Golden_MatchesRendererOutput()
    {
        var runArtifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());
        var comparisonArtifact = ComparisonArtifactLoader.Load(TestPaths.ComparisonArtifactPath());

        var rendered = TestPaths.NormalizeNewlines(CustomerMarkdownReportRenderer.Render(
            runArtifact,
            comparisonArtifact));
        var golden = TestPaths.NormalizeNewlines(File.ReadAllText(TestPaths.CustomerReportPath()));

        Assert.Equal(golden, rendered);
    }

    [Fact]
    public void CustomerMarkdownReportRenderer_RendersObjectiveNotes()
    {
        var runArtifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());
        var comparisonArtifact = ComparisonArtifactLoader.Load(TestPaths.ComparisonArtifactPath());

        var report = CustomerMarkdownReportRenderer.Render(
            runArtifact,
            comparisonArtifact);

        Assert.Contains("deterministic simulation artifacts", report);
        Assert.Contains("objective numeric deltas", report);
        Assert.Contains("deterministic baseline positions", report);
        Assert.DoesNotContain("Unity", report);
        Assert.DoesNotContain("recommendation", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("good", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bad", report, StringComparison.OrdinalIgnoreCase);
    }
}
