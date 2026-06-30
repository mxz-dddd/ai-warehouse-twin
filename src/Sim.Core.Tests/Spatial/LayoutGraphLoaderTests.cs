using Sim.Core.Domain;
using Sim.Core.Spatial;
using Xunit;

namespace Sim.Core.Tests.Spatial;

public sealed class LayoutGraphLoaderTests
{
    [Fact]
    public void Load_ValidLayout_ReturnsPathGraphWithExpectedRoute()
    {
        var graph = LayoutGraphLoader.Load(SampleLayoutJson());

        var route = graph.GetRoute("node-dock-in", "node-pack-out");

        Assert.Equal(
            ["node-dock-in", "node-aisle-w", "node-pick-a", "node-pack-out"],
            route.PathNodeIds);
        Assert.Equal(
            ["edge-dock-aisle", "edge-aisle-pick-a", "edge-pick-a-pack"],
            route.EdgeIds);
        Assert.Equal(8100, route.TotalDistanceMm);
    }

    [Fact]
    public void Load_MissingNodes_ThrowsDiagnosticDomainRuleViolation()
    {
        const string json = """
        {
          "edges": [
            {
              "edgeId": "edge-a-b",
              "fromNodeId": "node-a",
              "toNodeId": "node-b",
              "distanceMm": 1000,
              "bidirectional": true
            }
          ]
        }
        """;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => LayoutGraphLoader.Load(json));

        Assert.Contains("requires at least one node", exception.Message);
    }

    [Fact]
    public void Load_MissingNodeField_ThrowsDiagnosticDomainRuleViolation()
    {
        const string json = """
        {
          "nodes": [
            {
              "nodeId": "node-a",
              "xMm": 0,
              "yMm": 0
            }
          ],
          "edges": [
            {
              "edgeId": "edge-a-b",
              "fromNodeId": "node-a",
              "toNodeId": "node-b",
              "distanceMm": 1000,
              "bidirectional": true
            }
          ]
        }
        """;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => LayoutGraphLoader.Load(json));

        Assert.Contains("nodeType", exception.Message);
        Assert.Contains("node-a", exception.Message);
    }

    [Fact]
    public void Load_EdgeReferencesMissingFromNode_ThrowsDiagnosticDomainRuleViolation()
    {
        var json = LayoutJson(
            nodes: """
            [
              { "nodeId": "node-b", "nodeType": "aisle", "xMm": 1000, "yMm": 0 }
            ]
            """,
            edges: """
            [
              {
                "edgeId": "edge-a-b",
                "fromNodeId": "node-a",
                "toNodeId": "node-b",
                "distanceMm": 1000,
                "bidirectional": true
              }
            ]
            """);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => LayoutGraphLoader.Load(json));

        Assert.Contains("edge-a-b", exception.Message);
        Assert.Contains("node-a", exception.Message);
    }

    [Fact]
    public void Load_EdgeReferencesMissingToNode_ThrowsDiagnosticDomainRuleViolation()
    {
        var json = LayoutJson(
            nodes: """
            [
              { "nodeId": "node-a", "nodeType": "dock", "xMm": 0, "yMm": 0 }
            ]
            """,
            edges: """
            [
              {
                "edgeId": "edge-a-b",
                "fromNodeId": "node-a",
                "toNodeId": "node-b",
                "distanceMm": 1000,
                "bidirectional": true
              }
            ]
            """);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => LayoutGraphLoader.Load(json));

        Assert.Contains("edge-a-b", exception.Message);
        Assert.Contains("node-b", exception.Message);
    }

    [Fact]
    public void Load_DuplicateNodeId_ThrowsDiagnosticDomainRuleViolation()
    {
        var json = LayoutJson(
            nodes: """
            [
              { "nodeId": "node-a", "nodeType": "dock", "xMm": 0, "yMm": 0 },
              { "nodeId": "node-a", "nodeType": "aisle", "xMm": 1000, "yMm": 0 }
            ]
            """,
            edges: """
            [
              {
                "edgeId": "edge-a-b",
                "fromNodeId": "node-a",
                "toNodeId": "node-a",
                "distanceMm": 1000,
                "bidirectional": true
              }
            ]
            """);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => LayoutGraphLoader.Load(json));

        Assert.Contains("node-a", exception.Message);
    }

    [Fact]
    public void Load_DuplicateEdgeId_ThrowsDiagnosticDomainRuleViolation()
    {
        var json = LayoutJson(
            nodes: """
            [
              { "nodeId": "node-a", "nodeType": "dock", "xMm": 0, "yMm": 0 },
              { "nodeId": "node-b", "nodeType": "aisle", "xMm": 1000, "yMm": 0 },
              { "nodeId": "node-c", "nodeType": "station", "xMm": 2000, "yMm": 0 }
            ]
            """,
            edges: """
            [
              {
                "edgeId": "edge-dup",
                "fromNodeId": "node-a",
                "toNodeId": "node-b",
                "distanceMm": 1000,
                "bidirectional": true
              },
              {
                "edgeId": "edge-dup",
                "fromNodeId": "node-b",
                "toNodeId": "node-c",
                "distanceMm": 1000,
                "bidirectional": true
              }
            ]
            """);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => LayoutGraphLoader.Load(json));

        Assert.Contains("edge-dup", exception.Message);
    }

    [Fact]
    public void Load_InvalidDistance_ThrowsDiagnosticDomainRuleViolation()
    {
        var json = LayoutJson(
            nodes: """
            [
              { "nodeId": "node-a", "nodeType": "dock", "xMm": 0, "yMm": 0 },
              { "nodeId": "node-b", "nodeType": "aisle", "xMm": 1000, "yMm": 0 }
            ]
            """,
            edges: """
            [
              {
                "edgeId": "edge-a-b",
                "fromNodeId": "node-a",
                "toNodeId": "node-b",
                "distanceMm": 0,
                "bidirectional": true
              }
            ]
            """);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => LayoutGraphLoader.Load(json));

        Assert.Contains("edge-a-b", exception.Message);
        Assert.Contains("0", exception.Message);
    }

    [Fact]
    public void Load_DirectionalEdges_PreserveReachabilitySemantics()
    {
        var graph = LayoutGraphLoader.Load(DirectionLayoutJson(bidirectional: false));

        var route = graph.GetRoute("node-a", "node-b");

        Assert.Equal(["node-a", "node-b"], route.PathNodeIds);
        Assert.False(graph.TryGetRoute("node-b", "node-a", out _));
    }

    [Fact]
    public void Load_BidirectionalEdges_PreserveReachabilitySemantics()
    {
        var graph = LayoutGraphLoader.Load(DirectionLayoutJson(bidirectional: true));

        var forward = graph.GetRoute("node-a", "node-b");
        var reverse = graph.GetRoute("node-b", "node-a");

        Assert.Equal(1000, forward.TotalDistanceMm);
        Assert.Equal(1000, reverse.TotalDistanceMm);
    }

    [Fact]
    public void Load_MultiPathLayout_UsesPathGraphShortestRoute()
    {
        var graph = LayoutGraphLoader.Load(SampleLayoutJson());

        var route = graph.GetRoute("node-dock-in", "node-pack-out");

        Assert.Equal(["edge-dock-aisle", "edge-aisle-pick-a", "edge-pick-a-pack"], route.EdgeIds);
        Assert.Equal(8100, route.TotalDistanceMm);
    }

    [Fact]
    public void Load_ShuffledInput_RemainsDeterministic()
    {
        var ordered = LayoutGraphLoader.Load(SampleLayoutJson());
        var shuffled = LayoutGraphLoader.Load(ShuffledLayoutJson());

        var first = ordered.GetRoute("node-dock-in", "node-pack-out");
        var second = shuffled.GetRoute("node-dock-in", "node-pack-out");

        Assert.Equal(first.PathNodeIds, second.PathNodeIds);
        Assert.Equal(first.EdgeIds, second.EdgeIds);
        Assert.Equal(first.TotalDistanceMm, second.TotalDistanceMm);
    }

    private static string DirectionLayoutJson(bool bidirectional)
    {
        return LayoutJson(
            nodes: """
            [
              { "nodeId": "node-a", "nodeType": "dock", "xMm": 0, "yMm": 0 },
              { "nodeId": "node-b", "nodeType": "aisle", "xMm": 1000, "yMm": 0 }
            ]
            """,
            edges: $$"""
            [
              {
                "edgeId": "edge-a-b",
                "fromNodeId": "node-a",
                "toNodeId": "node-b",
                "distanceMm": 1000,
                "bidirectional": {{bidirectional.ToString().ToLowerInvariant()}}
              }
            ]
            """);
    }

    private static string SampleLayoutJson()
    {
        return LayoutJson(
            nodes: """
            [
              { "nodeId": "node-dock-in", "nodeType": "dock", "xMm": 0, "yMm": 4000 },
              { "nodeId": "node-aisle-w", "nodeType": "aisle", "xMm": 2500, "yMm": 4000 },
              { "nodeId": "node-pick-a", "nodeType": "pick_face", "xMm": 5200, "yMm": 1600 },
              { "nodeId": "node-pack-out", "nodeType": "pack_station", "xMm": 7600, "yMm": 1600 }
            ]
            """,
            edges: """
            [
              {
                "edgeId": "edge-dock-aisle",
                "fromNodeId": "node-dock-in",
                "toNodeId": "node-aisle-w",
                "distanceMm": 2500,
                "bidirectional": true
              },
              {
                "edgeId": "edge-aisle-pick-a",
                "fromNodeId": "node-aisle-w",
                "toNodeId": "node-pick-a",
                "distanceMm": 3200,
                "bidirectional": true
              },
              {
                "edgeId": "edge-pick-a-pack",
                "fromNodeId": "node-pick-a",
                "toNodeId": "node-pack-out",
                "distanceMm": 2400,
                "bidirectional": true
              },
              {
                "edgeId": "edge-direct-expensive",
                "fromNodeId": "node-dock-in",
                "toNodeId": "node-pack-out",
                "distanceMm": 12000,
                "bidirectional": true
              }
            ]
            """);
    }

    private static string ShuffledLayoutJson()
    {
        return LayoutJson(
            nodes: """
            [
              { "nodeId": "node-pack-out", "nodeType": "pack_station", "xMm": 7600, "yMm": 1600 },
              { "nodeId": "node-pick-a", "nodeType": "pick_face", "xMm": 5200, "yMm": 1600 },
              { "nodeId": "node-dock-in", "nodeType": "dock", "xMm": 0, "yMm": 4000 },
              { "nodeId": "node-aisle-w", "nodeType": "aisle", "xMm": 2500, "yMm": 4000 }
            ]
            """,
            edges: """
            [
              {
                "edgeId": "edge-direct-expensive",
                "fromNodeId": "node-dock-in",
                "toNodeId": "node-pack-out",
                "distanceMm": 12000,
                "bidirectional": true
              },
              {
                "edgeId": "edge-pick-a-pack",
                "fromNodeId": "node-pick-a",
                "toNodeId": "node-pack-out",
                "distanceMm": 2400,
                "bidirectional": true
              },
              {
                "edgeId": "edge-dock-aisle",
                "fromNodeId": "node-dock-in",
                "toNodeId": "node-aisle-w",
                "distanceMm": 2500,
                "bidirectional": true
              },
              {
                "edgeId": "edge-aisle-pick-a",
                "fromNodeId": "node-aisle-w",
                "toNodeId": "node-pick-a",
                "distanceMm": 3200,
                "bidirectional": true
              }
            ]
            """);
    }

    private static string LayoutJson(string nodes, string edges)
    {
        return $$"""
        {
          "nodes": {{nodes}},
          "edges": {{edges}}
        }
        """;
    }
}
