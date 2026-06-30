using Sim.Core.Domain;
using Sim.Core.Resources;
using Sim.Core.Scenarios;

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
