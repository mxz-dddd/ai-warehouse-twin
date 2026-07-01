using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public sealed class MovementProvenanceRendererTests
{
    [Fact]
    public void Render_SampleWarehouseGoldenArtifact_MatchesGoldenProvenance()
    {
        var artifact = MovementArtifactLoader.Load(TestPaths.MovementArtifactPath());

        var rendered = TestPaths.NormalizeNewlines(MovementProvenanceRenderer.Render(artifact));
        var golden = TestPaths.NormalizeNewlines(File.ReadAllText(TestPaths.MovementProvenanceGoldenPath()));

        Assert.Equal(golden, rendered);
    }

    [Fact]
    public void Render_ContainsSchemaVersion()
    {
        var artifact = MovementArtifactLoader.Load(TestPaths.MovementArtifactPath());

        Assert.Contains("schema_version: movement-artifact.v1", MovementProvenanceRenderer.Render(artifact));
    }

    [Fact]
    public void Render_ContainsPositionTimelineNote()
    {
        var artifact = MovementArtifactLoader.Load(TestPaths.MovementArtifactPath());

        Assert.Contains(
            "RunArtifact v1 position_timeline contains baseline layout positions, NOT simulated movement.",
            MovementProvenanceRenderer.Render(artifact));
    }

    [Fact]
    public void Render_DoesNotInferKpiOrDistance()
    {
        var artifact = MovementArtifactLoader.Load(TestPaths.MovementArtifactPath());
        var output = MovementProvenanceRenderer.Render(artifact);

        // Level 1 must not compute movement KPIs or distances — only provenance metadata
        Assert.DoesNotContain("distance", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("throughput", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("duration", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_UsesLfNewlinesOnly()
    {
        var artifact = MovementArtifactLoader.Load(TestPaths.MovementArtifactPath());

        Assert.DoesNotContain("\r", MovementProvenanceRenderer.Render(artifact));
    }

    // --- baseline isolation: existing report renderer must not change ---

    [Fact]
    public void MarkdownReportRenderer_WithoutMovementArtifact_Unchanged()
    {
        var run = RunArtifactLoader.Load(TestPaths.ArtifactPath());

        var rendered = TestPaths.NormalizeNewlines(MarkdownReportRenderer.Render(run));
        var golden = TestPaths.NormalizeNewlines(File.ReadAllText(TestPaths.GoldenReportPath()));

        Assert.Equal(golden, rendered);
    }
}
