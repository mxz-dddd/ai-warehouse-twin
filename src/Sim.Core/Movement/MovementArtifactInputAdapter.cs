using Sim.Core.Domain;
using Sim.Core.Resources;
using Sim.Core.Scenarios;
using Sim.Core.Spatial;

namespace Sim.Core.Movement;

public static class MovementArtifactInputAdapter
{
    private const long FixtureTravelTimeMs = 100;

    public static MovementArtifactGenerationRequest FromScenario(
        WarehouseScenario scenario,
        MovementArtifactInputAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(options);

        var trace = new WarehouseScenarioRunner().RunWithTrace(scenario);
        var nodes = BuildNodes(trace.Layout.Resources);
        var actors = BuildActors(trace.Layout.Resources);

        if (actors.Count == 0)
        {
            throw new DomainRuleViolationException(
                "MovementArtifact input adapter requires at least one actor for fixture-scale movement legs.");
        }

        if (nodes.Count < 2)
        {
            throw new DomainRuleViolationException(
                "MovementArtifact input adapter requires at least two static nodes for fixture-scale movement legs.");
        }

        var actor = actors[0];
        var fromNode = nodes.Single(node => node.NodeId == actor.InitialNodeId);
        var toNode = nodes.First(node => node.NodeId != fromNode.NodeId);
        var edge = BuildEdge(fromNode, toNode);
        var operationId = FirstOperationId(trace.ResourceLeaseTimeline);

        var leg = new MovementLegInput(
            $"seg-{actor.ActorId}-001",
            actor.ActorId,
            operationId,
            fromNode.NodeId,
            toNode.NodeId,
            StartMs: 0,
            EndMs: FixtureTravelTimeMs,
            edge.DistanceM,
            [fromNode.NodeId, toNode.NodeId],
            [edge.EdgeId],
            FixtureTravelTimeMs);

        return new MovementArtifactGenerationRequest(
            scenario.ScenarioId,
            options.RunId,
            scenario.Seed,
            options.SourceRunArtifact,
            nodes,
            [edge],
            actors,
            [leg],
            options.GraphSource,
            options.GeneratorVersion);
    }

    public static MovementArtifactGenerationRequest FromScenario(
        WarehouseScenario scenario,
        MovementArtifactInputAdapterOptions options,
        PathGraph layoutGraph,
        IReadOnlyDictionary<string, string> resourceHomeNodeIds)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(layoutGraph);
        ArgumentNullException.ThrowIfNull(resourceHomeNodeIds);

        var trace = TryRunTrace(scenario, resourceHomeNodeIds);
        var nodes = BuildNodes(layoutGraph.Nodes);
        var edges = BuildEdges(layoutGraph.Edges);
        var actors = trace is null
            ? BuildActors(resourceHomeNodeIds, layoutGraph.Nodes)
            : BuildActors(
                trace.Layout.Resources,
                resourceHomeNodeIds,
                layoutGraph.Nodes);

        if (actors.Count == 0)
        {
            throw new DomainRuleViolationException(
                "MovementArtifact input adapter requires at least one actor for layout graph movement legs.");
        }

        var actor = actors[0];
        var route = SelectModeledRoute(layoutGraph, actor.InitialNodeId);
        var operationId = trace is null
            ? FirstOperationId(scenario)
            : FirstOperationId(trace.ResourceLeaseTimeline);
        var distanceM = route.TotalDistanceMm / 1000.0;

        var leg = new MovementLegInput(
            $"seg-{actor.ActorId}-001",
            actor.ActorId,
            operationId,
            route.PathNodeIds[0],
            route.PathNodeIds[^1],
            StartMs: 0,
            EndMs: FixtureTravelTimeMs,
            distanceM,
            route.PathNodeIds,
            route.EdgeIds,
            FixtureTravelTimeMs);

        return new MovementArtifactGenerationRequest(
            scenario.ScenarioId,
            options.RunId,
            scenario.Seed,
            options.SourceRunArtifact,
            nodes,
            edges,
            actors,
            [leg],
            options.GraphSource,
            options.GeneratorVersion);
    }

    private static IReadOnlyList<MovementGraphNodeInput> BuildNodes(
        IReadOnlyList<WarehouseScenarioLayoutResource> resources)
    {
        return resources
            .OrderBy(resource => resource.ResourceId, StringComparer.Ordinal)
            .Select(resource => new MovementGraphNodeInput(
                resource.Position.NodeId,
                ResourceTypeFor(resource.ResourceId),
                Convert.ToDouble(resource.Position.X),
                Convert.ToDouble(resource.Position.Y)))
            .ToArray();
    }

    private static IReadOnlyList<MovementGraphNodeInput> BuildNodes(
        IReadOnlyList<PathGraphNode> nodes)
    {
        return nodes
            .OrderBy(node => node.NodeId, StringComparer.Ordinal)
            .Select(node => new MovementGraphNodeInput(
                node.NodeId,
                node.NodeType,
                Convert.ToDouble(node.XMm),
                Convert.ToDouble(node.YMm)))
            .ToArray();
    }

    private static IReadOnlyList<MovementGraphEdgeInput> BuildEdges(
        IReadOnlyList<PathGraphEdge> edges)
    {
        return edges
            .OrderBy(edge => edge.EdgeId, StringComparer.Ordinal)
            .Select(edge => new MovementGraphEdgeInput(
                edge.EdgeId,
                edge.FromNodeId,
                edge.ToNodeId,
                edge.DistanceMm / 1000.0,
                TravelTimeMs: 0,
                edge.Bidirectional))
            .ToArray();
    }

    private static IReadOnlyList<MovementActorInput> BuildActors(
        IReadOnlyList<WarehouseScenarioLayoutResource> resources)
    {
        return resources
            .Where(resource => IsMovableResource(resource.ResourceId))
            .OrderBy(resource => resource.ResourceId, StringComparer.Ordinal)
            .Select(resource => new MovementActorInput(
                resource.ResourceId,
                ResourceTypeFor(resource.ResourceId),
                resource.ResourceId,
                resource.Position.NodeId,
                Capacity: null,
                LoadState: "unknown"))
            .ToArray();
    }

    private static IReadOnlyList<MovementActorInput> BuildActors(
        IReadOnlyList<WarehouseScenarioLayoutResource> resources,
        IReadOnlyDictionary<string, string> resourceHomeNodeIds,
        IReadOnlyList<PathGraphNode> nodes)
    {
        var nodeIds = nodes
            .Select(node => node.NodeId)
            .ToHashSet(StringComparer.Ordinal);

        return resources
            .Where(resource => IsMovableResource(resource.ResourceId))
            .OrderBy(resource => resource.ResourceId, StringComparer.Ordinal)
            .Select(resource =>
            {
                if (!resourceHomeNodeIds.TryGetValue(resource.ResourceId, out var homeNodeId))
                {
                    throw new DomainRuleViolationException(
                        $"MovementArtifact layout graph export requires a home node mapping for movable resource. ResourceId: {resource.ResourceId}.");
                }

                if (!nodeIds.Contains(homeNodeId))
                {
                    throw new DomainRuleViolationException(
                        $"MovementArtifact movable resource home node is not present in layout graph. ResourceId: {resource.ResourceId}. NodeId: {homeNodeId}.");
                }

                return new MovementActorInput(
                    resource.ResourceId,
                    ResourceTypeFor(resource.ResourceId),
                    resource.ResourceId,
                    homeNodeId,
                    Capacity: null,
                    LoadState: "unknown");
            })
            .ToArray();
    }

    private static IReadOnlyList<MovementActorInput> BuildActors(
        IReadOnlyDictionary<string, string> resourceHomeNodeIds,
        IReadOnlyList<PathGraphNode> nodes)
    {
        var nodeIds = nodes
            .Select(node => node.NodeId)
            .ToHashSet(StringComparer.Ordinal);

        return resourceHomeNodeIds
            .Where(entry => IsMovableResource(entry.Key))
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry =>
            {
                if (!nodeIds.Contains(entry.Value))
                {
                    throw new DomainRuleViolationException(
                        $"MovementArtifact movable resource home node is not present in layout graph. ResourceId: {entry.Key}. NodeId: {entry.Value}.");
                }

                return new MovementActorInput(
                    entry.Key,
                    ResourceTypeFor(entry.Key),
                    entry.Key,
                    entry.Value,
                    Capacity: null,
                    LoadState: "unknown");
            })
            .ToArray();
    }

    private static MovementGraphEdgeInput BuildEdge(
        MovementGraphNodeInput fromNode,
        MovementGraphNodeInput toNode)
    {
        var distance = Distance(fromNode, toNode);
        return new MovementGraphEdgeInput(
            $"edge-{fromNode.NodeId}-{toNode.NodeId}",
            fromNode.NodeId,
            toNode.NodeId,
            distance,
            FixtureTravelTimeMs,
            Bidirectional: true);
    }

    private static string? FirstOperationId(
        IReadOnlyList<ResourceLeaseTimelineEntry> timeline)
    {
        return timeline
            .OrderBy(entry => entry.StartedAtMs)
            .ThenBy(entry => entry.OperationId, StringComparer.Ordinal)
            .ThenBy(entry => entry.StageType, StringComparer.Ordinal)
            .ThenBy(entry => entry.ResourceId, StringComparer.Ordinal)
            .Select(entry => entry.OperationId)
            .FirstOrDefault();
    }

    private static string? FirstOperationId(WarehouseScenario scenario)
    {
        var candidates = new List<(long AtMs, string OperationId)>();

        if (scenario.InboundScenario is not null)
        {
            candidates.AddRange(scenario.InboundScenario.Receipts.Select(
                receipt => (receipt.ArrivesAtMs, receipt.ReceiptId)));
        }

        if (scenario.OutboundScenario is not null)
        {
            candidates.AddRange(scenario.OutboundScenario.Orders.Select(
                order => (order.ReleasedAtMs, order.OrderId)));
        }

        if (scenario.EachPickScenario is not null)
        {
            candidates.AddRange(scenario.EachPickScenario.Orders.Select(
                order => (order.ReleasedAtMs, order.OrderId)));
        }

        return candidates
            .OrderBy(candidate => candidate.AtMs)
            .ThenBy(candidate => candidate.OperationId, StringComparer.Ordinal)
            .Select(candidate => candidate.OperationId)
            .FirstOrDefault();
    }

    private static WarehouseScenarioTraceResult? TryRunTrace(
        WarehouseScenario scenario,
        IReadOnlyDictionary<string, string> resourceHomeNodeIds)
    {
        try
        {
            return new WarehouseScenarioRunner().RunWithTrace(scenario);
        }
        catch (DomainRuleViolationException) when (HasMovableResourceHomeNode(resourceHomeNodeIds))
        {
            return null;
        }
    }

    private static bool HasMovableResourceHomeNode(
        IReadOnlyDictionary<string, string> resourceHomeNodeIds)
    {
        return resourceHomeNodeIds.Keys.Any(IsMovableResource);
    }

    private static PathRoute SelectModeledRoute(
        PathGraph graph,
        string fromNodeId)
    {
        foreach (var candidate in graph.Nodes.OrderBy(node => node.NodeId, StringComparer.Ordinal))
        {
            if (StringComparer.Ordinal.Equals(candidate.NodeId, fromNodeId))
            {
                continue;
            }

            if (graph.TryGetRoute(fromNodeId, candidate.NodeId, out var route) &&
                route.EdgeIds.Count > 0)
            {
                return route;
            }
        }

        throw new DomainRuleViolationException(
            $"MovementArtifact input adapter cannot find a reachable modeled route from actor home node. FromNodeId: {fromNodeId}.");
    }

    private static string ResourceTypeFor(string resourceId)
    {
        if (resourceId.StartsWith("dock-", StringComparison.Ordinal))
        {
            return "dock";
        }

        if (resourceId.StartsWith("forklift-", StringComparison.Ordinal))
        {
            return "forklift";
        }

        if (resourceId.StartsWith("station-", StringComparison.Ordinal))
        {
            return "station";
        }

        if (resourceId.StartsWith("worker-", StringComparison.Ordinal))
        {
            return "worker";
        }

        return "resource";
    }

    private static bool IsMovableResource(string resourceId)
    {
        return resourceId.StartsWith("forklift-", StringComparison.Ordinal) ||
               resourceId.StartsWith("worker-", StringComparison.Ordinal);
    }

    private static double Distance(
        MovementGraphNodeInput fromNode,
        MovementGraphNodeInput toNode)
    {
        var deltaX = toNode.X - fromNode.X;
        var deltaY = toNode.Y - fromNode.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
}
