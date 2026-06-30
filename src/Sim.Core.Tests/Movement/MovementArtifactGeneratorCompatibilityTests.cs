using System.Text.Json;
using Sim.Core.Movement;
using Sim.Report;
using Xunit;

namespace Sim.Core.Tests.Movement;

public sealed class MovementArtifactGeneratorCompatibilityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    [Fact]
    public void Generate_OutputJson_CanBeReadByMovementArtifactLoader()
    {
        var generated = MovementArtifactGenerator.Generate(Request());
        var json = JsonSerializer.Serialize(generated, JsonOptions);

        var loaded = MovementArtifactLoader.Deserialize(json);

        Assert.Equal(MovementArtifactGenerator.SchemaVersion, loaded.schema_version);
        Assert.Equal(MovementArtifactGenerator.ArtifactKind, loaded.artifact_kind);
        Assert.Equal("sample-small-warehouse", loaded.scenario_id);
        Assert.Equal("run-001", loaded.run_id);
        Assert.Equal(20240627, loaded.seed);
        Assert.Equal("run-artifact.v1.json", loaded.source_run_artifact);
        Assert.NotNull(loaded.warehouse_graph);
        Assert.NotEmpty(loaded.actors);
        Assert.NotEmpty(loaded.movement_events);
        Assert.NotEmpty(loaded.route_segments);
        Assert.NotNull(loaded.provenance);
    }

    [Fact]
    public void Generate_SameInputTwice_ProducesLoaderCompatibleDeterministicJson()
    {
        var request = Request();
        var first = JsonSerializer.Serialize(
            MovementArtifactGenerator.Generate(request),
            JsonOptions);
        var second = JsonSerializer.Serialize(
            MovementArtifactGenerator.Generate(request),
            JsonOptions);

        Assert.Equal(first, second);

        var loaded = MovementArtifactLoader.Deserialize(first);
        Assert.Equal("sample-small-warehouse", loaded.scenario_id);
        Assert.Equal("run-001", loaded.run_id);
    }

    private static MovementArtifactGenerationRequest Request()
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
                new MovementLegInput(
                    "seg-worker-1-001",
                    "worker-1",
                    "each-pick-1",
                    "dock-1",
                    "station-1",
                    0,
                    100,
                    10,
                    ["dock-1", "station-1"],
                    ["edge-dock-1-station-1"],
                    100),
            ],
            GraphSource: "compatibility-test",
            GeneratorVersion: "test-generator");
    }
}
