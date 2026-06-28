using Sim.Contracts.Artifacts;
using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public class MarkdownReportRendererTests
{
    [Fact]
    public void Render_SampleWarehouseGoldenArtifact_MatchesGoldenReport()
    {
        var artifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());

        var rendered = TestPaths.NormalizeNewlines(MarkdownReportRenderer.Render(artifact));
        var golden = TestPaths.NormalizeNewlines(File.ReadAllText(TestPaths.GoldenReportPath()));

        Assert.Equal(golden, rendered);
    }

    [Fact]
    public void Render_AnnotatesThroughputAsSimulationDerived()
    {
        var artifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());

        Assert.Contains("不代表真实", MarkdownReportRenderer.Render(artifact));
    }

    [Fact]
    public void Render_RoundsLongTailThroughputToThreeDecimals()
    {
        // A decimal with a long fractional tail must not leak into the customer report.
        var artifact = NewArtifact(
            eventLogLineCount: 0,
            eventLog: Array.Empty<string>(),
            receiptThroughput: 35999.9999999999999999999712m);

        var report = MarkdownReportRenderer.Render(artifact);

        Assert.Contains("36000", report);
        Assert.DoesNotContain("35999", report);
    }

    [Fact]
    public void Render_UsesEventLogLineCountFromKpi_NotEventLogLength()
    {
        // KPI count (7) intentionally differs from the event_log array (empty):
        // the report must report the contract KPI field, not the array length.
        var artifact = NewArtifact(eventLogLineCount: 7, eventLog: Array.Empty<string>());

        Assert.Contains("(event_log_line_count): 7", MarkdownReportRenderer.Render(artifact));
    }

    [Fact]
    public void Render_EmptyEventLog_DoesNotThrow()
    {
        var artifact = NewArtifact(eventLogLineCount: 0, eventLog: Array.Empty<string>());

        Assert.Null(Record.Exception(() => MarkdownReportRenderer.Render(artifact)));
    }

    [Fact]
    public void Render_UsesLfNewlinesOnly()
    {
        var artifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());

        Assert.DoesNotContain("\r", MarkdownReportRenderer.Render(artifact));
    }

    private static RunArtifact NewArtifact(
        int eventLogLineCount,
        IReadOnlyList<string> eventLog,
        decimal receiptThroughput = 0m)
    {
        return new RunArtifact
        {
            SchemaVersion = RunArtifact.CurrentSchemaVersion,
            ArtifactKind = RunArtifact.CurrentArtifactKind,
            ScenarioId = "unit-test",
            Seed = 1,
            StartedAtMs = 0,
            FinishedAtMs = 100,
            FinalWorldTimeMs = 100,
            KpiSummary = new RunArtifactKpiSummary
            {
                TotalDurationMs = 100,
                TotalCompletedWorkItems = 0,
                EventLogLineCount = eventLogLineCount,
                ReceiptThroughputPerHour = receiptThroughput,
                OutboundOrderThroughputPerHour = 0m,
                EachPickOrderThroughputPerHour = 0m,
                TotalWorkItemThroughputPerHour = 0m,
            },
            EventLog = eventLog,
        };
    }
}
