using System.Text.Json;
using Sim.Contracts.Artifacts;
using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public class RunArtifactLoaderTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Load_SampleWarehouseGoldenArtifact_PopulatesContract()
    {
        var artifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());

        Assert.Equal("run-artifact.v1", artifact.SchemaVersion);
        Assert.Equal("warehouse-simulation-run", artifact.ArtifactKind);
        Assert.Equal("sample-small-warehouse", artifact.ScenarioId);
        Assert.Equal(20240627, artifact.Seed);
        Assert.Equal(3, artifact.KpiSummary.TotalCompletedWorkItems);
        Assert.Equal(13, artifact.EventLog.Count);
    }

    [Fact]
    public void ExportArtifact_IncludesLayoutResources()
    {
        var artifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());

        Assert.NotNull(artifact.Layout);
        Assert.NotEmpty(artifact.Layout.Resources);
        Assert.All(
            artifact.Layout.Resources,
            resource =>
            {
                Assert.False(string.IsNullOrWhiteSpace(resource.ResourceId));
                Assert.False(string.IsNullOrWhiteSpace(resource.NodeId));
            });
    }

    [Fact]
    public void ExportArtifact_IncludesPositionTimeline()
    {
        var artifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());

        Assert.NotEmpty(artifact.PositionTimeline);
        Assert.Contains(
            artifact.PositionTimeline,
            entry => entry.EventType == "start");
        Assert.Contains(
            artifact.PositionTimeline,
            entry => entry.EventType == "finish");

        AssertOperationTypes(
            artifact.PositionTimeline,
            "inbound",
            "outbound",
            "each_pick");
        AssertStageTypes(
            artifact.PositionTimeline,
            "operation");
    }

    [Fact]
    public void RunArtifact_LayoutAndPositionTimeline_RoundTrip()
    {
        var artifact = new RunArtifact
        {
            SchemaVersion = RunArtifact.CurrentSchemaVersion,
            ArtifactKind = RunArtifact.CurrentArtifactKind,
            ScenarioId = "round-trip",
            Seed = 1,
            StartedAtMs = 0,
            FinishedAtMs = 10,
            FinalWorldTimeMs = 10,
            KpiSummary = new RunArtifactKpiSummary
            {
                TotalDurationMs = 10,
                TotalCompletedWorkItems = 1,
                EventLogLineCount = 1,
                ReceiptThroughputPerHour = 0m,
                OutboundOrderThroughputPerHour = 0m,
                EachPickOrderThroughputPerHour = 0m,
                TotalWorkItemThroughputPerHour = 0m,
            },
            Layout = new RunArtifactLayout
            {
                Resources =
                [
                    new RunArtifactLayoutResource(
                        "dock-1",
                        "dock-node",
                        1m,
                        2m),
                ],
            },
            PositionTimeline =
            [
                new RunArtifactPositionTimelineEntry(
                    "receipt-1",
                    "inbound",
                    "dock",
                    "dock-1",
                    0,
                    "start",
                    "dock-node",
                    1m,
                    2m),
                new RunArtifactPositionTimelineEntry(
                    "receipt-1",
                    "inbound",
                    "dock",
                    "dock-1",
                    10,
                    "finish",
                    "dock-node",
                    1m,
                    2m),
            ],
            EventLog = ["event"],
        };

        var json = JsonSerializer.Serialize(artifact, JsonOptions);
        var roundTripped = RunArtifactLoader.Deserialize(json);

        Assert.Equal(artifact.Layout.Resources.ToArray(), roundTripped.Layout.Resources.ToArray());
        Assert.Equal(artifact.PositionTimeline.ToArray(), roundTripped.PositionTimeline.ToArray());
    }

    [Fact]
    public void Deserialize_UnsupportedSchemaVersion_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v0",
          "artifact_kind": "warehouse-simulation-run",
          "scenario_id": "x",
          "kpi_summary": {},
          "event_log": []
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_UnsupportedArtifactKind_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v1",
          "artifact_kind": "something-else",
          "scenario_id": "x",
          "kpi_summary": {},
          "event_log": []
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_EmptyScenarioId_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v1",
          "artifact_kind": "warehouse-simulation-run",
          "scenario_id": "",
          "kpi_summary": {},
          "event_log": []
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_NullKpiSummary_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v1",
          "artifact_kind": "warehouse-simulation-run",
          "scenario_id": "x",
          "kpi_summary": null,
          "event_log": []
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_NullEventLog_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v1",
          "artifact_kind": "warehouse-simulation-run",
          "scenario_id": "x",
          "kpi_summary": {},
          "event_log": null
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_NullJson_Throws()
    {
        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize("null"));
    }

    private static void AssertOperationTypes(
        IEnumerable<RunArtifactPositionTimelineEntry> timeline,
        params string[] expectedOperationTypes)
    {
        Assert.Equal(
            expectedOperationTypes.Order(StringComparer.Ordinal),
            timeline
                .Select(entry => entry.OperationType)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
    }

    private static void AssertStageTypes(
        IEnumerable<RunArtifactPositionTimelineEntry> timeline,
        params string[] expectedStageTypes)
    {
        Assert.Equal(
            expectedStageTypes.Order(StringComparer.Ordinal),
            timeline
                .Select(entry => entry.StageType)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
    }
}
