using System.Text.Json;
using Sim.Contracts.Artifacts;
using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public sealed class PositionTimelineSemanticsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void SampleGoldenPositionTimeline_UsesOperationHandoffAtBaselineResourcePositions()
    {
        var artifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());

        Assert.Equal("run-artifact.v1", artifact.SchemaVersion);
        Assert.Equal("sample-small-warehouse", artifact.ScenarioId);
        Assert.Equal(6, artifact.PositionTimeline.Count);

        var resourcesById = artifact.Layout.Resources.ToDictionary(
            resource => resource.ResourceId,
            StringComparer.Ordinal);

        Assert.Equal(
            ["dock-1", "station-1"],
            resourcesById.Keys.Order(StringComparer.Ordinal));

        Assert.All(
            artifact.PositionTimeline,
            entry =>
            {
                Assert.Equal("operation", entry.StageType);
                Assert.Contains(
                    entry.EventType,
                    new[] { "start", "finish" });
                Assert.True(
                    resourcesById.TryGetValue(entry.ResourceId, out var resource),
                    $"Timeline resource_id must exist in layout.resources: {entry.ResourceId}");

                Assert.Equal(resource.NodeId, entry.NodeId);
                Assert.Equal(resource.X, entry.X);
                Assert.Equal(resource.Y, entry.Y);
            });
    }

    [Fact]
    public void SampleGoldenPositionTimeline_DoesNotExposeMovementSemantics()
    {
        var artifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());
        var timelineJson = JsonSerializer.Serialize(
            artifact.PositionTimeline,
            JsonOptions).ToLowerInvariant();

        foreach (var forbidden in MovementVocabulary())
        {
            Assert.DoesNotContain(forbidden, timelineJson, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CustomerReportGolden_DoesNotClaimMovementMapOrRouteTrace()
    {
        var report = TestPaths.NormalizeNewlines(
            File.ReadAllText(TestPaths.CustomerReportGoldenPath()));
        var lowerReport = report.ToLowerInvariant();

        Assert.True(
            lowerReport.Contains("baseline positions", StringComparison.Ordinal) ||
            lowerReport.Contains("not a full warehouse map", StringComparison.Ordinal),
            "Customer report should keep conservative baseline-position wording.");

        foreach (var forbidden in CustomerReportMovementClaims())
        {
            Assert.DoesNotContain(forbidden, lowerReport, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PositionTimelineSemanticsDocument_ContainsRequiredSafetyBoundaries()
    {
        var document = TestPaths.NormalizeNewlines(
            File.ReadAllText(FindRepoFile(
                "docs",
                "architecture",
                "position-timeline-semantics.md")));

        Assert.Contains(
            "baseline layout positions, NOT simulated movement",
            document,
            StringComparison.Ordinal);
        Assert.Contains(
            "Do not implement or claim real movement",
            document,
            StringComparison.Ordinal);
        Assert.Contains("CONTRACT-", document, StringComparison.Ordinal);
        Assert.Contains("R2", document, StringComparison.Ordinal);
    }

    private static string FindRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                new[] { directory.FullName }.Concat(parts).ToArray());

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Cannot find repository file: {string.Join("/", parts)}");
    }

    private static string[] MovementVocabulary()
    {
        return
        [
            "route",
            "path",
            "trajectory",
            "speed",
            "distance",
            "travel",
            "collision",
            "movement",
        ];
    }

    private static string[] CustomerReportMovementClaims()
    {
        return
        [
            "movement map",
            "route trace",
            "travel trace",
            "forklift movement",
            "worker route",
            "goods trajectory",
            "path optimization",
            "collision-aware",
            "distance-based",
        ];
    }
}
