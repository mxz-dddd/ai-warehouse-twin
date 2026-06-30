using System.Text.Json;
using Sim.Core.Movement;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Samples;
using Sim.Report;
using Xunit;

namespace Sim.Core.Tests.Movement;

public sealed class MovementArtifactEndToEndBoundaryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    [Fact]
    public void AdapterGeneratorLoader_MinimalScenario_LoadsGeneratedMovementArtifact()
    {
        var request = MovementArtifactInputAdapter.FromScenario(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse(),
            Options());
        var artifact = MovementArtifactGenerator.Generate(request);
        var json = JsonSerializer.Serialize(artifact, JsonOptions);

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
    public void AdapterGeneratorLoader_MinimalScenario_ProducesDeterministicJson()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var options = Options();

        var first = SerializeGeneratedArtifact(scenario, options);
        var second = SerializeGeneratedArtifact(scenario, options);

        Assert.Equal(first, second);
        Assert.Equal(
            "sample-small-warehouse",
            MovementArtifactLoader.Deserialize(first).scenario_id);
        Assert.Equal(
            "sample-small-warehouse",
            MovementArtifactLoader.Deserialize(second).scenario_id);
    }

    [Fact]
    public void AdapterGeneratorLoader_DoesNotRequireFilesOrGoldenArtifacts()
    {
        var sourceRunArtifact = Path.Combine(
            Path.GetTempPath(),
            $"movement-artifact-e2e-{Guid.NewGuid():N}.json");

        Assert.False(File.Exists(sourceRunArtifact));

        var json = SerializeGeneratedArtifact(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse(),
            Options(sourceRunArtifact));
        var loaded = MovementArtifactLoader.Deserialize(json);

        Assert.Equal(sourceRunArtifact, loaded.source_run_artifact);
        Assert.False(File.Exists(sourceRunArtifact));
    }

    [Fact]
    public void AdapterGeneratorLoader_DoesNotExposeCustomerFacingReportOrCliBehavior()
    {
        var json = SerializeGeneratedArtifact(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse(),
            Options());

        var loaded = MovementArtifactLoader.Deserialize(json);

        Assert.NotEmpty(loaded.movement_events);
        Assert.NotEmpty(loaded.route_segments);
        Assert.Equal("run-001", loaded.run_id);
        Assert.Equal("run-artifact.v1.json", loaded.source_run_artifact);
        Assert.NotNull(loaded.provenance);
        Assert.False(File.Exists("run-artifact.v1.json"));
    }

    private static string SerializeGeneratedArtifact(
        WarehouseScenario scenario,
        MovementArtifactInputAdapterOptions options)
    {
        var request = MovementArtifactInputAdapter.FromScenario(
            scenario,
            options);
        return JsonSerializer.Serialize(
            MovementArtifactGenerator.Generate(request),
            JsonOptions);
    }

    private static MovementArtifactInputAdapterOptions Options(
        string sourceRunArtifact = "run-artifact.v1.json")
    {
        return new MovementArtifactInputAdapterOptions(
            sourceRunArtifact,
            "fixture-scale-e2e-test",
            "test-generator",
            runId: "run-001");
    }
}
