using WarehouseTwin.Contracts;

namespace Sim.Core.Movement;

public static class MovementArtifactGenerator
{
    public const string SchemaVersion = "movement-artifact.v1";
    public const string ArtifactKind = "warehouse-movement";

    public static MovementArtifact Generate(MovementArtifactGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var nodesById = request.Nodes.ToDictionary(
            node => node.NodeId,
            StringComparer.Ordinal);
        var edgesById = request.Edges.ToDictionary(
            edge => edge.EdgeId,
            StringComparer.Ordinal);
        var actorsById = request.Actors.ToDictionary(
            actor => actor.ActorId,
            StringComparer.Ordinal);

        var orderedNodes = request.Nodes
            .OrderBy(node => node.NodeId, StringComparer.Ordinal)
            .Select(ToNodeObject)
            .ToArray();
        var orderedEdges = request.Edges
            .OrderBy(edge => edge.EdgeId, StringComparer.Ordinal)
            .Select(ToEdgeObject)
            .ToArray();
        var orderedActors = request.Actors
            .OrderBy(actor => actor.ActorId, StringComparer.Ordinal)
            .Select(ToActorObject)
            .ToArray();
        var orderedSegments = request.MovementLegs
            .OrderBy(leg => leg.StartMs)
            .ThenBy(leg => leg.SegmentId, StringComparer.Ordinal)
            .Select(ToSegmentObject)
            .ToArray();
        var orderedEvents = request.MovementLegs
            .SelectMany(leg => ToMovementEvents(
                leg,
                nodesById,
                actorsById[leg.ActorId]))
            .OrderBy(movementEvent => Convert.ToInt64(movementEvent["at_ms"]))
            .ThenBy(movementEvent => Convert.ToString(movementEvent["event_id"]), StringComparer.Ordinal)
            .ToArray();

        var warehouseGraph = new Dictionary<string, object?>
        {
            ["nodes"] = orderedNodes,
            ["edges"] = orderedEdges,
        };

        var provenance = new Dictionary<string, object?>
        {
            ["movement_generator_version"] = request.GeneratorVersion,
            ["graph_source"] = request.GraphSource,
            ["movement_enabled"] = true,
            ["deterministic_generation_policy"] =
                "explicit-inputs-ordered-by-id-and-time",
        };

        return new MovementArtifact(
            schema_version: SchemaVersion,
            artifact_kind: ArtifactKind,
            scenario_id: request.ScenarioId,
            run_id: request.RunId ?? string.Empty,
            seed: request.Seed,
            source_run_artifact: request.SourceRunArtifact,
            warehouse_graph: warehouseGraph,
            actors: orderedActors,
            movement_events: orderedEvents,
            route_segments: orderedSegments,
            provenance: provenance);
    }

    private static void ValidateRequest(MovementArtifactGenerationRequest request)
    {
        RequireNotEmpty(request.ScenarioId, "scenario_id");
        RequireNotEmpty(request.SourceRunArtifact, "source_run_artifact");
        RequireNotEmpty(request.GraphSource, "graph_source");
        RequireNotEmpty(request.GeneratorVersion, "movement_generator_version");
        RequireList(request.Nodes, "nodes");
        RequireList(request.Edges, "edges");
        RequireList(request.Actors, "actors");
        RequireList(request.MovementLegs, "movement_legs");

        var nodeIds = ValidateUniqueIds(
            request.Nodes,
            node => node.NodeId,
            "node_id");
        var edgeIds = ValidateUniqueIds(
            request.Edges,
            edge => edge.EdgeId,
            "edge_id");
        var actorIds = ValidateUniqueIds(
            request.Actors,
            actor => actor.ActorId,
            "actor_id");
        ValidateUniqueIds(
            request.MovementLegs,
            leg => leg.SegmentId,
            "segment_id");

        foreach (var node in request.Nodes)
        {
            RequireNotEmpty(node.NodeType, $"node_type for node_id '{node.NodeId}'");
        }

        foreach (var edge in request.Edges)
        {
            RequireNotEmpty(edge.FromNodeId, $"from_node_id for edge_id '{edge.EdgeId}'");
            RequireNotEmpty(edge.ToNodeId, $"to_node_id for edge_id '{edge.EdgeId}'");
            RequireNode(nodeIds, edge.FromNodeId, $"edge_id '{edge.EdgeId}' from_node_id");
            RequireNode(nodeIds, edge.ToNodeId, $"edge_id '{edge.EdgeId}' to_node_id");
            RequireNonnegative(edge.DistanceM, $"distance_m for edge_id '{edge.EdgeId}'");
            RequireNonnegative(edge.TravelTimeMs, $"travel_time_ms for edge_id '{edge.EdgeId}'");
        }

        foreach (var actor in request.Actors)
        {
            RequireNotEmpty(actor.ActorType, $"actor_type for actor_id '{actor.ActorId}'");
            RequireNotEmpty(actor.InitialNodeId, $"initial_node_id for actor_id '{actor.ActorId}'");
            RequireNode(nodeIds, actor.InitialNodeId, $"actor_id '{actor.ActorId}' initial_node_id");
            if (actor.Capacity is < 0)
            {
                throw new ArgumentException(
                    $"capacity for actor_id '{actor.ActorId}' must be nonnegative.");
            }
        }

        foreach (var leg in request.MovementLegs)
        {
            ValidateLeg(leg, nodeIds, edgeIds, actorIds);
        }
    }

    private static void ValidateLeg(
        MovementLegInput leg,
        IReadOnlySet<string> nodeIds,
        IReadOnlySet<string> edgeIds,
        IReadOnlySet<string> actorIds)
    {
        RequireNotEmpty(leg.ActorId, $"actor_id for segment_id '{leg.SegmentId}'");
        RequireNotEmpty(leg.FromNodeId, $"from_node_id for segment_id '{leg.SegmentId}'");
        RequireNotEmpty(leg.ToNodeId, $"to_node_id for segment_id '{leg.SegmentId}'");
        RequireList(leg.PathNodeIds, $"path_node_ids for segment_id '{leg.SegmentId}'");
        RequireList(leg.EdgeIds, $"edge_ids for segment_id '{leg.SegmentId}'");
        RequireActor(actorIds, leg.ActorId, $"segment_id '{leg.SegmentId}' actor_id");
        RequireNode(nodeIds, leg.FromNodeId, $"segment_id '{leg.SegmentId}' from_node_id");
        RequireNode(nodeIds, leg.ToNodeId, $"segment_id '{leg.SegmentId}' to_node_id");
        RequireNonnegative(leg.StartMs, $"start_ms for segment_id '{leg.SegmentId}'");
        RequireNonnegative(leg.DistanceM, $"distance_m for segment_id '{leg.SegmentId}'");
        RequireNonnegative(leg.TravelTimeMs, $"travel_time_ms for segment_id '{leg.SegmentId}'");

        if (leg.EndMs < leg.StartMs)
        {
            throw new ArgumentException(
                $"end_ms for segment_id '{leg.SegmentId}' must be greater than or equal to start_ms.");
        }

        foreach (var nodeId in leg.PathNodeIds)
        {
            RequireNotEmpty(nodeId, $"path_node_id for segment_id '{leg.SegmentId}'");
            RequireNode(nodeIds, nodeId, $"segment_id '{leg.SegmentId}' path_node_id");
        }

        foreach (var edgeId in leg.EdgeIds)
        {
            RequireNotEmpty(edgeId, $"edge_id for segment_id '{leg.SegmentId}'");
            RequireEdge(edgeIds, edgeId, $"segment_id '{leg.SegmentId}' edge_id");
        }
    }

    private static IReadOnlySet<string> ValidateUniqueIds<T>(
        IReadOnlyList<T> values,
        Func<T, string> idSelector,
        string idName)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var id = idSelector(value);
            RequireNotEmpty(id, idName);
            if (!ids.Add(id))
            {
                throw new ArgumentException($"Duplicate {idName} '{id}'.");
            }
        }

        return ids;
    }

    private static Dictionary<string, object?> ToNodeObject(MovementGraphNodeInput node)
    {
        return new Dictionary<string, object?>
        {
            ["node_id"] = node.NodeId,
            ["node_type"] = node.NodeType,
            ["x"] = node.X,
            ["y"] = node.Y,
        };
    }

    private static Dictionary<string, object?> ToEdgeObject(MovementGraphEdgeInput edge)
    {
        return new Dictionary<string, object?>
        {
            ["edge_id"] = edge.EdgeId,
            ["from_node_id"] = edge.FromNodeId,
            ["to_node_id"] = edge.ToNodeId,
            ["distance_m"] = edge.DistanceM,
            ["travel_time_ms"] = edge.TravelTimeMs,
            ["bidirectional"] = edge.Bidirectional,
        };
    }

    private static Dictionary<string, object?> ToActorObject(MovementActorInput actor)
    {
        return new Dictionary<string, object?>
        {
            ["actor_id"] = actor.ActorId,
            ["actor_type"] = actor.ActorType,
            ["resource_id"] = actor.ResourceId,
            ["initial_node_id"] = actor.InitialNodeId,
            ["capacity"] = actor.Capacity,
            ["load_state"] = actor.LoadState,
        };
    }

    private static Dictionary<string, object?> ToSegmentObject(MovementLegInput leg)
    {
        return new Dictionary<string, object?>
        {
            ["segment_id"] = leg.SegmentId,
            ["actor_id"] = leg.ActorId,
            ["operation_id"] = leg.OperationId,
            ["from_node_id"] = leg.FromNodeId,
            ["to_node_id"] = leg.ToNodeId,
            ["start_ms"] = leg.StartMs,
            ["end_ms"] = leg.EndMs,
            ["distance_m"] = leg.DistanceM,
            ["path_node_ids"] = leg.PathNodeIds.ToArray(),
            ["edge_ids"] = leg.EdgeIds.ToArray(),
            ["travel_time_ms"] = leg.TravelTimeMs,
        };
    }

    private static IEnumerable<Dictionary<string, object?>> ToMovementEvents(
        MovementLegInput leg,
        IReadOnlyDictionary<string, MovementGraphNodeInput> nodesById,
        MovementActorInput actor)
    {
        var from = nodesById[leg.FromNodeId];
        var to = nodesById[leg.ToNodeId];

        yield return ToMovementEvent(
            eventId: $"evt-{leg.SegmentId}-started",
            eventType: "movement.started",
            atMs: leg.StartMs,
            node: from,
            leg: leg,
            actor: actor);
        yield return ToMovementEvent(
            eventId: $"evt-{leg.SegmentId}-arrived",
            eventType: "movement.arrived",
            atMs: leg.EndMs,
            node: to,
            leg: leg,
            actor: actor);
    }

    private static Dictionary<string, object?> ToMovementEvent(
        string eventId,
        string eventType,
        long atMs,
        MovementGraphNodeInput node,
        MovementLegInput leg,
        MovementActorInput actor)
    {
        return new Dictionary<string, object?>
        {
            ["event_id"] = eventId,
            ["actor_id"] = leg.ActorId,
            ["operation_id"] = leg.OperationId,
            ["event_type"] = eventType,
            ["at_ms"] = atMs,
            ["node_id"] = node.NodeId,
            ["x"] = node.X,
            ["y"] = node.Y,
            ["load_state"] = actor.LoadState,
            ["related_resource_id"] = actor.ResourceId,
        };
    }

    private static void RequireList<T>(IReadOnlyList<T>? values, string name)
    {
        if (values is null)
        {
            throw new ArgumentException($"{name} must not be null.");
        }
    }

    private static void RequireNotEmpty(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} must not be empty.");
        }
    }

    private static void RequireNode(
        IReadOnlySet<string> nodeIds,
        string nodeId,
        string context)
    {
        if (!nodeIds.Contains(nodeId))
        {
            throw new ArgumentException($"{context} references unknown node_id '{nodeId}'.");
        }
    }

    private static void RequireEdge(
        IReadOnlySet<string> edgeIds,
        string edgeId,
        string context)
    {
        if (!edgeIds.Contains(edgeId))
        {
            throw new ArgumentException($"{context} references unknown edge_id '{edgeId}'.");
        }
    }

    private static void RequireActor(
        IReadOnlySet<string> actorIds,
        string actorId,
        string context)
    {
        if (!actorIds.Contains(actorId))
        {
            throw new ArgumentException($"{context} references unknown actor_id '{actorId}'.");
        }
    }

    private static void RequireNonnegative(double value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{name} must be nonnegative.");
        }
    }

    private static void RequireNonnegative(long value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{name} must be nonnegative.");
        }
    }
}
