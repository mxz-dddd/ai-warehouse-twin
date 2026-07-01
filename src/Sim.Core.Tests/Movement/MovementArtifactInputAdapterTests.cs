using System.Text.Json;
using Sim.Core.Domain;
using Sim.Core.Movement;
using Sim.Core.Processes.Inbound;
using Sim.Core.Processes.Outbound;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Samples;
using Sim.Core.Spatial;
using Xunit;

namespace Sim.Core.Tests.Movement;

public sealed class MovementArtifactInputAdapterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    [Fact]
    public void FromScenario_MinimalScenario_ReturnsGenerationRequest()
    {
        var request = MovementArtifactInputAdapter.FromScenario(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse(),
            Options());

        Assert.Equal("sample-small-warehouse", request.ScenarioId);
        Assert.Equal(20240627, request.Seed);
        Assert.Equal("run-001", request.RunId);
        Assert.Equal("run-artifact.v1.json", request.SourceRunArtifact);
        Assert.NotEmpty(request.Nodes);
        Assert.NotEmpty(request.Edges);
        Assert.NotEmpty(request.Actors);
        Assert.NotEmpty(request.MovementLegs);
        Assert.Equal("fixture-scale-adapter-test", request.GraphSource);
        Assert.Equal("test-generator", request.GeneratorVersion);
    }

    [Fact]
    public void FromScenario_MinimalScenario_RequestCanGenerateMovementArtifact()
    {
        var request = MovementArtifactInputAdapter.FromScenario(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse(),
            Options());

        var artifact = MovementArtifactGenerator.Generate(request);

        Assert.Equal(MovementArtifactGenerator.SchemaVersion, artifact.schema_version);
        Assert.Equal(MovementArtifactGenerator.ArtifactKind, artifact.artifact_kind);
        Assert.NotNull(artifact.warehouse_graph);
        Assert.NotEmpty(artifact.actors);
        Assert.NotEmpty(artifact.movement_events);
        Assert.NotEmpty(artifact.route_segments);
        Assert.NotNull(artifact.provenance);
    }

    [Fact]
    public void FromScenario_MinimalScenario_GeneratedArtifactIsDeterministic()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var options = Options();

        var first = JsonSerializer.Serialize(
            MovementArtifactGenerator.Generate(
                MovementArtifactInputAdapter.FromScenario(scenario, options)),
            JsonOptions);
        var second = JsonSerializer.Serialize(
            MovementArtifactGenerator.Generate(
                MovementArtifactInputAdapter.FromScenario(scenario, options)),
            JsonOptions);

        Assert.Equal(first, second);
    }

    [Fact]
    public void FromScenario_DoesNotRequireFilesOrGoldenArtifacts()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"movement-artifact-input-adapter-{Guid.NewGuid():N}.json");

        var request = MovementArtifactInputAdapter.FromScenario(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse(),
            Options(sourceRunArtifact: path));

        Assert.Equal(path, request.SourceRunArtifact);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void FromScenario_OrdersNodesActorsEdgesAndLegsDeterministically()
    {
        var request = MovementArtifactInputAdapter.FromScenario(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse(),
            Options());

        Assert.Equal(
            new[] { "dock-1", "forklift-1", "station-1", "worker-1" },
            request.Nodes.Select(node => node.NodeId).ToArray());
        Assert.Equal(
            new[] { "forklift-1", "worker-1" },
            request.Actors.Select(actor => actor.ActorId).ToArray());
        var edge = Assert.Single(request.Edges);
        Assert.Equal("edge-forklift-1-dock-1", edge.EdgeId);
        Assert.Equal("forklift-1", edge.FromNodeId);
        Assert.Equal("dock-1", edge.ToNodeId);
        Assert.Equal(1, edge.DistanceM);
        Assert.Equal(100, edge.TravelTimeMs);

        var leg = Assert.Single(request.MovementLegs);
        Assert.Equal("seg-forklift-1-001", leg.SegmentId);
        Assert.Equal("forklift-1", leg.ActorId);
        Assert.Equal("receipt-1", leg.OperationId);
        Assert.Equal("forklift-1", leg.FromNodeId);
        Assert.Equal("dock-1", leg.ToNodeId);
        Assert.Equal(0, leg.StartMs);
        Assert.Equal(100, leg.EndMs);
        Assert.Equal(["forklift-1", "dock-1"], leg.PathNodeIds);
        Assert.Equal(["edge-forklift-1-dock-1"], leg.EdgeIds);
    }

    [Fact]
    public void FromScenario_WithLayoutGraph_UsesPathGraphRouteInputs()
    {
        var graph = new PathGraph(
            [
                new PathGraphNode("node-aisle-a", "aisle", 1000, 0),
                new PathGraphNode("node-dock-in", "dock", 0, 0),
                new PathGraphNode("node-pack-out", "pack_station", 3000, 0),
            ],
            [
                new PathGraphEdge("edge-dock-aisle", "node-dock-in", "node-aisle-a", 1000, true),
                new PathGraphEdge("edge-aisle-pack", "node-aisle-a", "node-pack-out", 2000, true),
            ]);
        var request = MovementArtifactInputAdapter.FromScenario(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse(),
            Options(),
            graph,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["forklift-1"] = "node-dock-in",
                ["worker-1"] = "node-pack-out",
            });

        Assert.Equal(
            new[] { "node-aisle-a", "node-dock-in", "node-pack-out" },
            request.Nodes.Select(node => node.NodeId).ToArray());
        Assert.Equal(
            new[] { "edge-aisle-pack", "edge-dock-aisle" },
            request.Edges.Select(edge => edge.EdgeId).ToArray());
        var actor = Assert.Single(request.Actors.Where(actor => actor.ActorId == "forklift-1"));
        Assert.Equal("node-dock-in", actor.InitialNodeId);
        var leg = Assert.Single(request.MovementLegs);
        Assert.Equal("node-dock-in", leg.FromNodeId);
        Assert.Equal("node-aisle-a", leg.ToNodeId);
        Assert.Equal(["node-dock-in", "node-aisle-a"], leg.PathNodeIds);
        Assert.Equal(["edge-dock-aisle"], leg.EdgeIds);
    }

    [Fact]
    public void FromScenario_InsufficientNodes_ThrowsClearError()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => MovementArtifactInputAdapter.FromScenario(
                OutboundWorkerOnlyTraceScenario(),
                Options()));

        Assert.Contains(
            "requires at least two static nodes",
            exception.Message);
    }

    [Fact]
    public void FromScenario_InsufficientActors_ThrowsClearError()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => MovementArtifactInputAdapter.FromScenario(
                NoRecordedResourceTraceScenario(),
                Options()));

        Assert.Contains(
            "requires at least one actor",
            exception.Message);
    }

    private static MovementArtifactInputAdapterOptions Options(
        string sourceRunArtifact = "run-artifact.v1.json")
    {
        return new MovementArtifactInputAdapterOptions(
            sourceRunArtifact,
            "fixture-scale-adapter-test",
            "test-generator",
            runId: "run-001");
    }

    private static WarehouseScenario OutboundWorkerOnlyTraceScenario()
    {
        return new WarehouseScenario(
            "outbound-worker-only",
            seed: 20240627,
            inboundScenario: null,
            outboundScenario: new OutboundScenario(
                "outbound-worker-only.outbound",
                seed: 202,
                [
                    new OutboundOrder(
                        "order-1",
                        "warehouse-1",
                        "sku-1",
                        1m,
                        "pick-1",
                        "stage-1",
                        "dock-1",
                        releasedAtMs: 0)
                ],
                [
                    new OutboundInventoryItem(
                        "inv-1",
                        "sku-1",
                        1m,
                        "pick-1",
                        InventoryStatus.Available)
                ],
                new OutboundProcessParameters(
                    pickTravelDurationMs: 10,
                    pickServiceDurationMs: 0,
                    stageDurationMs: 0,
                    loadDurationMs: 0),
                workerCount: 1,
                dockCount: 1),
            eachPickScenario: null);
    }

    private static WarehouseScenario NoRecordedResourceTraceScenario()
    {
        return new WarehouseScenario(
            "no-recorded-resource-trace",
            seed: 20240627,
            inboundScenario: new InboundScenario(
                "no-recorded-resource-trace.inbound",
                seed: 101,
                [
                    new InboundReceipt(
                        "receipt-1",
                        "warehouse-1",
                        "sku-1",
                        1m,
                        "stage-1",
                        "reserve-1",
                        arrivesAtMs: 0)
                ],
                new InboundProcessParameters(
                    unloadDurationMs: 0,
                    putawayTravelDurationMs: 0,
                    putawayServiceDurationMs: 0),
                dockCount: 1,
                forkliftCount: 1),
            outboundScenario: null,
            eachPickScenario: null);
    }
}
