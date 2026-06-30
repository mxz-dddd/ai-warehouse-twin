using System.Text.Json;
using Sim.Core.Movement;
using Xunit;

namespace Sim.Core.Tests.Movement;

public sealed class MovementArtifactGeneratorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    [Fact]
    public void Generate_MinimalRequest_ReturnsMovementArtifactIdentity()
    {
        var artifact = MovementArtifactGenerator.Generate(MinimalRequest());

        Assert.Equal("movement-artifact.v1", artifact.schema_version);
        Assert.Equal("warehouse-movement", artifact.artifact_kind);
        Assert.Equal("sample-small-warehouse", artifact.scenario_id);
        Assert.Equal("run-001", artifact.run_id);
        Assert.Equal(20240627, artifact.seed);
        Assert.Equal("run-artifact.v1.json", artifact.source_run_artifact);
        Assert.NotNull(artifact.warehouse_graph);
        Assert.NotEmpty(artifact.actors);
        Assert.NotEmpty(artifact.movement_events);
        Assert.NotEmpty(artifact.route_segments);
        Assert.NotNull(artifact.provenance);
    }

    [Fact]
    public void Generate_MinimalRequest_ProducesDeterministicGraphActorsEventsSegments()
    {
        var request = MinimalRequest() with
        {
            Nodes =
            [
                new MovementGraphNodeInput("station-1", "station", 10, 0),
                new MovementGraphNodeInput("dock-1", "dock", 0, 0),
            ],
            Actors =
            [
                new MovementActorInput("worker-2", "worker", "worker-2", "dock-1", 1, "empty"),
                new MovementActorInput("worker-1", "worker", "worker-1", "dock-1", 1, "empty"),
            ],
            MovementLegs =
            [
                new MovementLegInput(
                    "seg-b",
                    "worker-1",
                    "operation-b",
                    "dock-1",
                    "station-1",
                    100,
                    200,
                    10,
                    ["dock-1", "station-1"],
                    ["edge-dock-1-station-1"],
                    100),
                new MovementLegInput(
                    "seg-a",
                    "worker-1",
                    "operation-a",
                    "dock-1",
                    "station-1",
                    0,
                    100,
                    10,
                    ["dock-1", "station-1"],
                    ["edge-dock-1-station-1"],
                    100),
            ],
        };

        using var document = ToJsonDocument(MovementArtifactGenerator.Generate(request));
        var root = document.RootElement;

        Assert.Equal(
            new[] { "dock-1", "station-1" },
            root
                .GetProperty("warehouse_graph")
                .GetProperty("nodes")
                .EnumerateArray()
                .Select(node => node.GetProperty("node_id").GetString())
                .ToArray());
        Assert.Equal(
            new[] { "worker-1", "worker-2" },
            root
                .GetProperty("actors")
                .EnumerateArray()
                .Select(actor => actor.GetProperty("actor_id").GetString())
                .ToArray());
        Assert.Equal(
            new[] { "seg-a", "seg-b" },
            root
                .GetProperty("route_segments")
                .EnumerateArray()
                .Select(segment => segment.GetProperty("segment_id").GetString())
                .ToArray());
        Assert.Equal(
            new[]
            {
                "evt-seg-a-started",
                "evt-seg-a-arrived",
                "evt-seg-b-started",
                "evt-seg-b-arrived",
            },
            root
                .GetProperty("movement_events")
                .EnumerateArray()
                .Select(movementEvent => movementEvent.GetProperty("event_id").GetString())
                .ToArray());
    }

    [Fact]
    public void Generate_SameInputTwice_ProducesEquivalentJson()
    {
        var request = MinimalRequest();

        var first = JsonSerializer.Serialize(
            MovementArtifactGenerator.Generate(request),
            JsonOptions);
        var second = JsonSerializer.Serialize(
            MovementArtifactGenerator.Generate(request),
            JsonOptions);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Generate_DoesNotRequireFilesOrGoldenArtifacts()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"movement-artifact-generator-{Guid.NewGuid():N}.json");
        var request = MinimalRequest() with
        {
            SourceRunArtifact = path,
        };

        var artifact = MovementArtifactGenerator.Generate(request);

        Assert.Equal(path, artifact.source_run_artifact);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Generate_DuplicateNodeId_ThrowsClearError()
    {
        var request = MinimalRequest() with
        {
            Nodes =
            [
                new MovementGraphNodeInput("dock-1", "dock", 0, 0),
                new MovementGraphNodeInput("dock-1", "dock", 1, 1),
            ],
        };

        var exception = Assert.Throws<ArgumentException>(
            () => MovementArtifactGenerator.Generate(request));

        Assert.Contains("Duplicate node_id 'dock-1'", exception.Message);
    }

    [Fact]
    public void Generate_EdgeReferencesMissingNode_ThrowsClearError()
    {
        var request = MinimalRequest() with
        {
            Edges =
            [
                new MovementGraphEdgeInput(
                    "edge-missing",
                    "dock-1",
                    "missing-node",
                    10,
                    100,
                    true),
            ],
        };

        var exception = Assert.Throws<ArgumentException>(
            () => MovementArtifactGenerator.Generate(request));

        Assert.Contains("edge_id 'edge-missing' to_node_id", exception.Message);
        Assert.Contains("missing-node", exception.Message);
    }

    [Fact]
    public void Generate_ActorReferencesMissingInitialNode_ThrowsClearError()
    {
        var request = MinimalRequest() with
        {
            Actors =
            [
                new MovementActorInput(
                    "worker-1",
                    "worker",
                    "worker-1",
                    "missing-node",
                    1,
                    "empty"),
            ],
        };

        var exception = Assert.Throws<ArgumentException>(
            () => MovementArtifactGenerator.Generate(request));

        Assert.Contains("actor_id 'worker-1' initial_node_id", exception.Message);
        Assert.Contains("missing-node", exception.Message);
    }

    [Fact]
    public void Generate_LegReferencesMissingActor_ThrowsClearError()
    {
        var request = MinimalRequest() with
        {
            MovementLegs =
            [
                MinimalLeg() with
                {
                    ActorId = "missing-actor",
                },
            ],
        };

        var exception = Assert.Throws<ArgumentException>(
            () => MovementArtifactGenerator.Generate(request));

        Assert.Contains("segment_id 'seg-worker-1-001' actor_id", exception.Message);
        Assert.Contains("missing-actor", exception.Message);
    }

    [Fact]
    public void Generate_LegWithNonMonotonicTime_ThrowsClearError()
    {
        var request = MinimalRequest() with
        {
            MovementLegs =
            [
                MinimalLeg() with
                {
                    StartMs = 100,
                    EndMs = 99,
                },
            ],
        };

        var exception = Assert.Throws<ArgumentException>(
            () => MovementArtifactGenerator.Generate(request));

        Assert.Contains("end_ms for segment_id 'seg-worker-1-001'", exception.Message);
    }

    [Fact]
    public void Generate_LegWithNegativeDistance_ThrowsClearError()
    {
        var request = MinimalRequest() with
        {
            MovementLegs =
            [
                MinimalLeg() with
                {
                    DistanceM = -1,
                },
            ],
        };

        var exception = Assert.Throws<ArgumentException>(
            () => MovementArtifactGenerator.Generate(request));

        Assert.Contains("distance_m for segment_id 'seg-worker-1-001'", exception.Message);
    }

    private static MovementArtifactGenerationRequest MinimalRequest()
    {
        return new MovementArtifactGenerationRequest(
            ScenarioId: "sample-small-warehouse",
            RunId: "run-001",
            Seed: 20240627,
            SourceRunArtifact: "run-artifact.v1.json",
            Nodes:
            [
                new MovementGraphNodeInput("dock-1", "dock", 0, 0),
                new MovementGraphNodeInput("station-1", "station", 10, 0),
            ],
            Edges:
            [
                new MovementGraphEdgeInput(
                    "edge-dock-1-station-1",
                    "dock-1",
                    "station-1",
                    10,
                    100,
                    true),
            ],
            Actors:
            [
                new MovementActorInput(
                    "worker-1",
                    "worker",
                    "worker-1",
                    "dock-1",
                    1,
                    "empty"),
            ],
            MovementLegs:
            [
                MinimalLeg(),
            ],
            GraphSource: "unit-test",
            GeneratorVersion: "test-generator");
    }

    private static MovementLegInput MinimalLeg()
    {
        return new MovementLegInput(
            SegmentId: "seg-worker-1-001",
            ActorId: "worker-1",
            OperationId: "each-pick-1",
            FromNodeId: "dock-1",
            ToNodeId: "station-1",
            StartMs: 0,
            EndMs: 100,
            DistanceM: 10,
            PathNodeIds: ["dock-1", "station-1"],
            EdgeIds: ["edge-dock-1-station-1"],
            TravelTimeMs: 100);
    }

    private static JsonDocument ToJsonDocument(object value)
    {
        return JsonDocument.Parse(JsonSerializer.Serialize(value, JsonOptions));
    }
}
