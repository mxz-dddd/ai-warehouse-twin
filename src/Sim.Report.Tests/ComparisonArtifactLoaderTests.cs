using Sim.Contracts.Artifacts;
using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public sealed class ComparisonArtifactLoaderTests
{
    [Fact]
    public void ComparisonArtifactLoader_LoadsSampleComparisonArtifact()
    {
        var artifact = ComparisonArtifactLoader.Load(
            TestPaths.ComparisonArtifactPath());

        Assert.Equal(
            ComparisonArtifact.CurrentSchemaVersion,
            artifact.SchemaVersion);
        Assert.False(string.IsNullOrWhiteSpace(artifact.Baseline.ScenarioId));
        Assert.False(string.IsNullOrWhiteSpace(artifact.Candidate.ScenarioId));
        Assert.NotEmpty(artifact.Deltas);
    }
}
