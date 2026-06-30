using System.Text.Json;
using Sim.Core.Domain;
using Sim.Core.Movement;
using Sim.Core.Spatial;
using Xunit;

namespace Sim.Core.Tests.Movement;

public sealed class MovementArtifactRouteSegmentTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    [Fact]
    public void Generate_RouteSegments_ExpandShortestPathIntoContinuousSegments()
    {
        using var document = ToJsonDocument(MovementArtifactGenerator.Generate(SampleRequest()));

        var segments = document.RootElement.GetProperty("route_segments").EnumerateArray().ToArray();

        Assert.Equal(2, segments.Length);
        Assert.Equal(new[] { "seg-forklift-1-001-001", "seg-forklift-1-001-002" }, SegmentIds(segments));
        Assert.Equal("forklift-1", segments[0].GetProperty("actor_id").GetString());
        Assert.Equal("node-a", segments[0].GetProperty("from_node_id").GetString());
        Assert.Equal("node-b", segments[0].GetProperty("to_node_id").GetString());
        Assert.Equal("node-b", segments[1].GetProperty("from_node_id").GetString());
        Assert.Equal("node-c", segments[1].GetProperty("to_node_id").GetString());
        Assert.Equal(0, segments[0].GetProperty("start_ms").GetInt64());
        Assert.Equal(300, segments[0].GetProperty("end_ms").GetInt64());
        Assert.Equal(300, segments[1].GetProperty("start_ms").GetInt64());
        Assert.Equal(900, segments[1].GetProperty("end_ms").GetInt64());
        Assert.Equal(300, segments[0].GetProperty("travel_time_ms").GetInt64());
        Assert.Equal(600, segments[1].GetProperty("travel_time_ms").GetInt64());
        Assert.Equal(1.0, segments[0].GetProperty("distance_m").GetDouble());
        Assert.Equal(2.0, segments[1].GetProperty("distance_m").GetDouble());
        Assert.Equal(new[] { "node-a", "node-b" }, PathNodeIds(segments[0]));
        Assert.Equal(new[] { "node-b", "node-c" }, PathNodeIds(segments[1]));
        Assert.Equal(new[] { "edge-a-b" }, EdgeIds(segments[0]));
        Assert.Equal(new[] { "edge-b-c" }, EdgeIds(segments[1]));
    }

    [Fact]
    public void Generate_RouteSegments_DistanceSumMatchesPathRouteTotalDistance()
    {
        using var document = ToJsonDocument(MovementArtifactGenerator.Generate(SampleRequest()));
        var segments = document.RootElement.GetProperty("route_segments").EnumerateArray().ToArray();
        var segmentDistanceMm = segments.Sum(segment =>
            checked((long)Math.Round(
                segment.GetProperty("distance_m").GetDouble() * 1000,
                MidpointRounding.AwayFromZero)));
        var route = LayoutGraphLoader.Load(SampleLayoutJson()).GetRoute("node-a", "node-c");

        Assert.Equal(route.TotalDistanceMm, segmentDistanceMm);
        Assert.Equal(3000, route.TotalDistanceMm);
    }

    [Fact]
    public void Generate_RouteSegments_IgnoresLegacyStraightLineLegPathFields()
    {
        var request = SampleRequest() with
        {
            MovementLegs =
            [
                SampleLeg() with
                {
                    DistanceM = 999,
                    PathNodeIds = ["node-a", "node-c"],
                    EdgeIds = ["edge-a-c-direct"],
                    TravelTimeMs = 1,
                },
            ],
        };

        using var document = ToJsonDocument(MovementArtifactGenerator.Generate(request));

        var segments = document.RootElement.GetProperty("route_segments").EnumerateArray().ToArray();
        Assert.Equal(new[] { "edge-a-b" }, EdgeIds(segments[0]));
        Assert.Equal(new[] { "edge-b-c" }, EdgeIds(segments[1]));
        Assert.Equal(3.0, segments.Sum(segment => segment.GetProperty("distance_m").GetDouble()));
    }

    [Fact]
    public void Generate_RouteSegments_SameInputSameSeedProducesIdenticalJson()
    {
        var request = SampleRequest();

        var first = JsonSerializer.Serialize(MovementArtifactGenerator.Generate(request), JsonOptions);
        var second = JsonSerializer.Serialize(MovementArtifactGenerator.Generate(request), JsonOptions);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Generate_RouteSegments_UnreachableRouteThrowsDiagnosticException()
    {
        var request = SampleRequest() with
        {
            Nodes =
            [
                new MovementGraphNodeInput("node-a", "dock", 0, 0),
                new MovementGraphNodeInput("node-b", "aisle", 1000, 0),
                new MovementGraphNodeInput("node-z", "station", 3000, 0),
            ],
            Edges =
            [
                new MovementGraphEdgeInput("edge-a-b", "node-a", "node-b", 1.0, 100, true),
            ],
            MovementLegs =
            [
                SampleLeg() with
                {
                    ToNodeId = "node-z",
                },
            ],
        };

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => MovementArtifactGenerator.Generate(request));

        Assert.Contains("Movement route could not be resolved", exception.Message);
        Assert.Contains("seg-forklift-1-001", exception.Message);
        Assert.Contains("forklift-1", exception.Message);
        Assert.Contains("node-a", exception.Message);
        Assert.Contains("node-z", exception.Message);
    }

    [Fact]
    public void Generate_RouteSegments_AnnotatesDeterministicModeledMovement()
    {
        using var document = ToJsonDocument(MovementArtifactGenerator.Generate(SampleRequest()));

        var provenance = document.RootElement.GetProperty("provenance");
        var policy = provenance.GetProperty("deterministic_generation_policy").GetString();
        Assert.Contains("deterministic modeled movement", policy);
        Assert.Contains("layout graph shortest paths", policy);
        Assert.Contains("PathGraph Dijkstra", policy);
        Assert.Equal("loaded", document.RootElement
            .GetProperty("movement_events")[0]
            .GetProperty("load_state")
            .GetString());
    }

    private static MovementArtifactGenerationRequest SampleRequest()
    {
        return new MovementArtifactGenerationRequest(
            ScenarioId: "route-segment-test",
            RunId: "run-route-001",
            Seed: 20260701,
            SourceRunArtifact: "run-artifact.v1.json",
            Nodes:
            [
                new MovementGraphNodeInput("node-a", "dock", 0, 0),
                new MovementGraphNodeInput("node-b", "aisle", 1000, 0),
                new MovementGraphNodeInput("node-c", "station", 3000, 0),
            ],
            Edges:
            [
                new MovementGraphEdgeInput("edge-a-c-direct", "node-a", "node-c", 10.0, 1000, true),
                new MovementGraphEdgeInput("edge-a-b", "node-a", "node-b", 1.0, 100, true),
                new MovementGraphEdgeInput("edge-b-c", "node-b", "node-c", 2.0, 200, true),
            ],
            Actors:
            [
                new MovementActorInput("forklift-1", "forklift", "forklift-1", "node-a", 1, "loaded"),
            ],
            MovementLegs:
            [
                SampleLeg(),
            ],
            GraphSource: "unit-test layout graph fixture / deterministic modeled",
            GeneratorVersion: "test-generator");
    }

    private static MovementLegInput SampleLeg()
    {
        return new MovementLegInput(
            SegmentId: "seg-forklift-1-001",
            ActorId: "forklift-1",
            OperationId: "move-pallet-1",
            FromNodeId: "node-a",
            ToNodeId: "node-c",
            StartMs: 0,
            EndMs: 900,
            DistanceM: 10.0,
            PathNodeIds: ["node-a", "node-c"],
            EdgeIds: ["edge-a-c-direct"],
            TravelTimeMs: 900);
    }

    private static string SampleLayoutJson()
    {
        return """
        {
          "nodes": [
            { "nodeId": "node-a", "nodeType": "dock", "xMm": 0, "yMm": 0 },
            { "nodeId": "node-b", "nodeType": "aisle", "xMm": 1000, "yMm": 0 },
            { "nodeId": "node-c", "nodeType": "station", "xMm": 3000, "yMm": 0 }
          ],
          "edges": [
            { "edgeId": "edge-a-c-direct", "fromNodeId": "node-a", "toNodeId": "node-c", "distanceMm": 10000, "bidirectional": true },
            { "edgeId": "edge-a-b", "fromNodeId": "node-a", "toNodeId": "node-b", "distanceMm": 1000, "bidirectional": true },
            { "edgeId": "edge-b-c", "fromNodeId": "node-b", "toNodeId": "node-c", "distanceMm": 2000, "bidirectional": true }
          ]
        }
        """;
    }

    private static string[] SegmentIds(JsonElement[] segments)
    {
        return segments
            .Select(segment => segment.GetProperty("segment_id").GetString()!)
            .ToArray();
    }

    private static string[] PathNodeIds(JsonElement segment)
    {
        return segment
            .GetProperty("path_node_ids")
            .EnumerateArray()
            .Select(node => node.GetString()!)
            .ToArray();
    }

    private static string[] EdgeIds(JsonElement segment)
    {
        return segment
            .GetProperty("edge_ids")
            .EnumerateArray()
            .Select(edge => edge.GetString()!)
            .ToArray();
    }

    private static JsonDocument ToJsonDocument(object value)
    {
        return JsonDocument.Parse(JsonSerializer.Serialize(value, JsonOptions));
    }
}
