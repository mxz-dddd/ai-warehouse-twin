using Sim.Core.Domain;
using Sim.Core.Spatial;
using Xunit;

namespace Sim.Core.Tests.Spatial;

public sealed class PathGraphTests
{
    [Fact]
    public void GetRoute_AdjacentNodes_ReturnsSingleEdgeDistance()
    {
        var graph = SampleGraph();

        var route = graph.GetRoute("node-dock-in", "node-aisle-w");

        Assert.Equal(["node-dock-in", "node-aisle-w"], route.PathNodeIds);
        Assert.Equal(["edge-dockin-aislew"], route.EdgeIds);
        Assert.Equal(2500, route.TotalDistanceMm);
    }

    [Fact]
    public void GetRoute_MultiEdgePath_ReturnsShortestRoute()
    {
        var graph = SampleGraph();

        var route = graph.GetRoute("node-dock-in", "node-loc-ra-3");

        Assert.Equal(
            ["node-dock-in", "node-aisle-w", "node-loc-ra-1", "node-loc-ra-2", "node-loc-ra-3"],
            route.PathNodeIds);
        Assert.Equal(
            ["edge-dockin-aislew", "edge-aislew-ra1", "edge-ra1-ra2", "edge-ra2-ra3"],
            route.EdgeIds);
        Assert.Equal(10000, route.TotalDistanceMm);
    }

    [Fact]
    public void GetRoute_BidirectionalEdges_AreReachableInBothDirections()
    {
        var graph = SampleGraph();

        var forward = graph.GetRoute("node-dock-in", "node-loc-rb-3");
        var reverse = graph.GetRoute("node-loc-rb-3", "node-dock-in");

        Assert.Equal(forward.TotalDistanceMm, reverse.TotalDistanceMm);
        Assert.Equal(10000, forward.TotalDistanceMm);
        Assert.Equal(
            ["node-loc-rb-3", "node-aisle-e", "node-pick-station", "node-aisle-w", "node-dock-in"],
            reverse.PathNodeIds);
        Assert.Equal(
            ["edge-rb3-aislee", "edge-pickstn-aislee", "edge-aislew-pickstn", "edge-dockin-aislew"],
            reverse.EdgeIds);
    }

    [Fact]
    public void GetRoute_UnknownNode_ThrowsDomainRuleViolationWithNodeId()
    {
        var graph = SampleGraph();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => graph.GetRoute("node-dock-in", "node-missing"));

        Assert.Contains("node-missing", exception.Message);
    }

    [Fact]
    public void GetRoute_UnreachableNode_ThrowsDomainRuleViolationWithFromAndTo()
    {
        var graph = new PathGraph(
            [
                new PathGraphNode("node-a", "aisle", 0, 0),
                new PathGraphNode("node-b", "aisle", 1, 0),
                new PathGraphNode("node-c", "aisle", 100, 0),
            ],
            [
                new PathGraphEdge("edge-a-b", "node-a", "node-b", 1, true),
            ]);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => graph.GetRoute("node-a", "node-c"));

        Assert.Contains("node-a", exception.Message);
        Assert.Contains("node-c", exception.Message);
    }

    [Fact]
    public void TryGetRoute_UnknownOrUnreachableNode_ReturnsFalse()
    {
        var graph = new PathGraph(
            [
                new PathGraphNode("node-a", "aisle", 0, 0),
                new PathGraphNode("node-b", "aisle", 1, 0),
                new PathGraphNode("node-c", "aisle", 100, 0),
            ],
            [
                new PathGraphEdge("edge-a-b", "node-a", "node-b", 1, true),
            ]);

        Assert.False(graph.TryGetRoute("node-a", "node-missing", out _));
        Assert.False(graph.TryGetRoute("node-a", "node-c", out _));
    }

    [Fact]
    public void GetRoute_RepeatedQuery_ReturnsIdenticalRoute()
    {
        var graph = SampleGraph();

        var first = graph.GetRoute("node-dock-in", "node-staging");
        var second = graph.GetRoute("node-dock-in", "node-staging");

        Assert.Equal(first.PathNodeIds, second.PathNodeIds);
        Assert.Equal(first.EdgeIds, second.EdgeIds);
        Assert.Equal(first.TotalDistanceMm, second.TotalDistanceMm);
    }

    [Fact]
    public void GetRoute_SameNode_ReturnsZeroDistanceRoute()
    {
        var graph = SampleGraph();

        var route = graph.GetRoute("node-pick-station", "node-pick-station");

        Assert.Equal(["node-pick-station"], route.PathNodeIds);
        Assert.Empty(route.EdgeIds);
        Assert.Equal(0, route.TotalDistanceMm);
    }

    [Fact]
    public void GetRoute_TieBreaksDeterministicallyByNodeAndEdgeIds()
    {
        var graph = new PathGraph(
            [
                new PathGraphNode("node-start", "dock", 0, 0),
                new PathGraphNode("node-a", "aisle", 1, 0),
                new PathGraphNode("node-b", "aisle", 1, 1),
                new PathGraphNode("node-end", "dock", 2, 0),
            ],
            [
                new PathGraphEdge("edge-start-b", "node-start", "node-b", 10, true),
                new PathGraphEdge("edge-b-end", "node-b", "node-end", 10, true),
                new PathGraphEdge("edge-start-a", "node-start", "node-a", 10, true),
                new PathGraphEdge("edge-a-end", "node-a", "node-end", 10, true),
            ]);

        var route = graph.GetRoute("node-start", "node-end");

        Assert.Equal(["node-start", "node-a", "node-end"], route.PathNodeIds);
        Assert.Equal(["edge-start-a", "edge-a-end"], route.EdgeIds);
        Assert.Equal(20, route.TotalDistanceMm);
    }

    private static PathGraph SampleGraph()
    {
        return new PathGraph(
            [
                new PathGraphNode("node-dock-in", "dock", 0, 4000),
                new PathGraphNode("node-aisle-w", "aisle", 2500, 4000),
                new PathGraphNode("node-pick-station", "station", 5000, 4000),
                new PathGraphNode("node-aisle-e", "aisle", 7000, 4000),
                new PathGraphNode("node-dock-out", "dock", 9000, 4000),
                new PathGraphNode("node-staging", "staging", 8000, 1000),
                new PathGraphNode("node-loc-ra-1", "location", 2500, 7000),
                new PathGraphNode("node-loc-ra-2", "location", 5000, 7000),
                new PathGraphNode("node-loc-ra-3", "location", 7000, 7000),
                new PathGraphNode("node-loc-rb-1", "location", 2500, 1000),
                new PathGraphNode("node-loc-rb-2", "location", 5000, 1000),
                new PathGraphNode("node-loc-rb-3", "location", 7000, 1000),
            ],
            [
                new PathGraphEdge("edge-dockin-aislew", "node-dock-in", "node-aisle-w", 2500, true),
                new PathGraphEdge("edge-aislew-pickstn", "node-aisle-w", "node-pick-station", 2500, true),
                new PathGraphEdge("edge-pickstn-aislee", "node-pick-station", "node-aisle-e", 2000, true),
                new PathGraphEdge("edge-aislee-dockout", "node-aisle-e", "node-dock-out", 2000, true),
                new PathGraphEdge("edge-aislee-staging", "node-aisle-e", "node-staging", 3162, true),
                new PathGraphEdge("edge-aislew-ra1", "node-aisle-w", "node-loc-ra-1", 3000, true),
                new PathGraphEdge("edge-ra1-ra2", "node-loc-ra-1", "node-loc-ra-2", 2500, true),
                new PathGraphEdge("edge-ra2-ra3", "node-loc-ra-2", "node-loc-ra-3", 2000, true),
                new PathGraphEdge("edge-ra3-aislee", "node-loc-ra-3", "node-aisle-e", 3000, true),
                new PathGraphEdge("edge-aislew-rb1", "node-aisle-w", "node-loc-rb-1", 3000, true),
                new PathGraphEdge("edge-rb1-rb2", "node-loc-rb-1", "node-loc-rb-2", 2500, true),
                new PathGraphEdge("edge-rb2-rb3", "node-loc-rb-2", "node-loc-rb-3", 2000, true),
                new PathGraphEdge("edge-rb3-aislee", "node-loc-rb-3", "node-aisle-e", 3000, true),
            ]);
    }
}
