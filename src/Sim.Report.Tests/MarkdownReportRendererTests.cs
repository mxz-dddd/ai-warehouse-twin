using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public class MarkdownReportRendererTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "WarehouseTwin.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (WarehouseTwin.sln).");
    }

    private static string ArtifactPath()
    {
        return Path.Combine(
            RepoRoot(), "datasets", "sample-small-warehouse", "artifacts", "run-artifact.v1.json");
    }

    private static string GoldenReportPath()
    {
        return Path.Combine(
            RepoRoot(), "datasets", "sample-small-warehouse", "artifacts", "run-artifact.v1.report.md");
    }

    private static string NormalizeNewlines(string value)
    {
        return value.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    [Fact]
    public void Render_SampleWarehouseGoldenArtifact_MatchesGoldenReport()
    {
        var artifact = RunArtifactLoader.Load(ArtifactPath());

        var rendered = NormalizeNewlines(MarkdownReportRenderer.Render(artifact));
        var golden = NormalizeNewlines(File.ReadAllText(GoldenReportPath()));

        Assert.Equal(golden, rendered);
    }

    [Fact]
    public void Render_AnnotatesThroughputAsSimulationDerived()
    {
        var artifact = RunArtifactLoader.Load(ArtifactPath());

        var rendered = MarkdownReportRenderer.Render(artifact);

        Assert.Contains("不代表真实", rendered);
    }

    [Fact]
    public void Load_SampleWarehouseGoldenArtifact_PopulatesContract()
    {
        var artifact = RunArtifactLoader.Load(ArtifactPath());

        Assert.Equal("run-artifact.v1", artifact.SchemaVersion);
        Assert.Equal("warehouse-simulation-run", artifact.ArtifactKind);
        Assert.Equal("sample-small-warehouse", artifact.ScenarioId);
        Assert.Equal(20240627, artifact.Seed);
        Assert.Equal(3, artifact.KpiSummary.TotalCompletedWorkItems);
        Assert.Equal(10, artifact.EventLog.Count);
    }
}
