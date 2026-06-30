using System.Text.Json;
using Sim.Core.Domain;
using Sim.Core.Spatial;
using WarehouseTwin.Contracts;

namespace Sim.Core.Movement;

public static class MovementArtifactGenerator
{
    public const string SchemaVersion = "movement-artifact.v1";
    public const string ArtifactKind = "warehouse-movement";

    private static readonly JsonSerializerOptions LayoutGraphJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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
        var pathGraph = BuildPathGraph(request.Nodes, request.Edges);
        var directedEdges = BuildDirectedEdgeLookup(request.Edges);

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
        var orderedLegs = request.MovementLegs
            .OrderBy(leg => leg.OperationId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(leg => leg.ActorId, StringComparer.Ordinal)
            .ThenBy(leg => leg.StartMs)
            .ThenBy(leg => leg.FromNodeId, StringComparer.Ordinal)
            .ThenBy(leg => leg.ToNodeId, StringComparer.Ordinal)
            .ThenBy(leg => leg.SegmentId, StringComparer.Ordinal)
            .ToArray();
        var orderedSegments = orderedLegs
            .SelectMany(leg => ExpandRouteSegments(leg, pathGraph, directedEdges))
            .ToArray();
        var routeSegmentsDifferFromLegacyLegs = RouteSegmentsDifferFromLegacyLegs(
            orderedLegs,
            orderedSegments);
        var orderedSegmentObjects = orderedSegments
            .Select(ToSegmentObject)
            .ToArray();
        var orderedEvents = orderedLegs
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
            ["deterministic_generation_policy"] = routeSegmentsDifferFromLegacyLegs
                ? "deterministic modeled movement from layout graph shortest paths via LayoutGraphLoader and PathGraph Dijkstra; not calibrated real trajectory"
                : "explicit-inputs-ordered-by-id-and-time",
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
            route_segments: orderedSegmentObjects,
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
        ValidateUniqueIds(
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
            ValidateLeg(leg, actorIds);
        }
    }

    private static void ValidateLeg(
        MovementLegInput leg,
        IReadOnlySet<string> actorIds)
    {
        RequireNotEmpty(leg.ActorId, $"actor_id for segment_id '{leg.SegmentId}'");
        RequireNotEmpty(leg.FromNodeId, $"from_node_id for segment_id '{leg.SegmentId}'");
        RequireNotEmpty(leg.ToNodeId, $"to_node_id for segment_id '{leg.SegmentId}'");
        RequireActor(actorIds, leg.ActorId, $"segment_id '{leg.SegmentId}' actor_id");
        RequireNonnegative(leg.StartMs, $"start_ms for segment_id '{leg.SegmentId}'");
        RequireNonnegative(leg.DistanceM, $"distance_m for segment_id '{leg.SegmentId}'");
        RequireNonnegative(leg.TravelTimeMs, $"travel_time_ms for segment_id '{leg.SegmentId}'");

        if (leg.EndMs < leg.StartMs)
        {
            throw new ArgumentException(
                $"end_ms for segment_id '{leg.SegmentId}' must be greater than or equal to start_ms.");
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

    private static bool RouteSegmentsDifferFromLegacyLegs(
        IReadOnlyList<MovementLegInput> orderedLegs,
        IReadOnlyList<MovementRouteSegment> orderedSegments)
    {
        if (orderedLegs.Count != orderedSegments.Count)
        {
            return true;
        }

        for (var index = 0; index < orderedLegs.Count; index++)
        {
            var leg = orderedLegs[index];
            var segment = orderedSegments[index];
            if (!StringComparer.Ordinal.Equals(leg.SegmentId, segment.SegmentId) ||
                !StringComparer.Ordinal.Equals(leg.ActorId, segment.ActorId) ||
                !StringComparer.Ordinal.Equals(leg.OperationId ?? string.Empty, segment.OperationId) ||
                !StringComparer.Ordinal.Equals(leg.FromNodeId, segment.FromNodeId) ||
                !StringComparer.Ordinal.Equals(leg.ToNodeId, segment.ToNodeId) ||
                leg.StartMs != segment.StartMs ||
                leg.EndMs != segment.EndMs ||
                Math.Round(leg.DistanceM * 1000, MidpointRounding.AwayFromZero) !=
                    Math.Round(segment.DistanceM * 1000, MidpointRounding.AwayFromZero) ||
                leg.TravelTimeMs != segment.TravelTimeMs ||
                !SequenceEqual(leg.PathNodeIds, segment.PathNodeIds) ||
                !SequenceEqual(leg.EdgeIds, segment.EdgeIds))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SequenceEqual(
        IReadOnlyList<string>? left,
        IReadOnlyList<string> right)
    {
        return left is not null &&
            left.SequenceEqual(right, StringComparer.Ordinal);
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

    private static Dictionary<string, object?> ToSegmentObject(MovementRouteSegment segment)
    {
        return new Dictionary<string, object?>
        {
            ["segment_id"] = segment.SegmentId,
            ["actor_id"] = segment.ActorId,
            ["operation_id"] = segment.OperationId,
            ["from_node_id"] = segment.FromNodeId,
            ["to_node_id"] = segment.ToNodeId,
            ["start_ms"] = segment.StartMs,
            ["end_ms"] = segment.EndMs,
            ["distance_m"] = segment.DistanceM,
            ["path_node_ids"] = segment.PathNodeIds.ToArray(),
            ["edge_ids"] = segment.EdgeIds.ToArray(),
            ["travel_time_ms"] = segment.TravelTimeMs,
        };
    }

    private static PathGraph BuildPathGraph(
        IReadOnlyList<MovementGraphNodeInput> nodes,
        IReadOnlyList<MovementGraphEdgeInput> edges)
    {
        var layoutGraph = new
        {
            Nodes = nodes
                .OrderBy(node => node.NodeId, StringComparer.Ordinal)
                .Select(node => new
                {
                    node.NodeId,
                    node.NodeType,
                    XMm = ConvertCoordinateToMillimeters(node.X),
                    YMm = ConvertCoordinateToMillimeters(node.Y),
                })
                .ToArray(),
            Edges = edges
                .OrderBy(edge => edge.EdgeId, StringComparer.Ordinal)
                .Select(edge => new
                {
                    edge.EdgeId,
                    edge.FromNodeId,
                    edge.ToNodeId,
                    DistanceMm = ConvertDistanceToMillimeters(
                        edge.DistanceM,
                        $"distance_m for edge_id '{edge.EdgeId}'"),
                    edge.Bidirectional,
                })
                .ToArray(),
        };

        return LayoutGraphLoader.Load(JsonSerializer.Serialize(layoutGraph, LayoutGraphJsonOptions));
    }

    private static IReadOnlyDictionary<DirectedEdgeKey, DirectedMovementEdge> BuildDirectedEdgeLookup(
        IReadOnlyList<MovementGraphEdgeInput> edges)
    {
        var directedEdges = new Dictionary<DirectedEdgeKey, DirectedMovementEdge>();

        foreach (var edge in edges.OrderBy(edge => edge.EdgeId, StringComparer.Ordinal))
        {
            var distanceMm = ConvertDistanceToMillimeters(
                edge.DistanceM,
                $"distance_m for edge_id '{edge.EdgeId}'");
            AddDirectedEdge(
                directedEdges,
                new DirectedMovementEdge(
                    edge.EdgeId,
                    edge.FromNodeId,
                    edge.ToNodeId,
                    distanceMm));

            if (edge.Bidirectional)
            {
                AddDirectedEdge(
                    directedEdges,
                    new DirectedMovementEdge(
                        edge.EdgeId,
                        edge.ToNodeId,
                        edge.FromNodeId,
                        distanceMm));
            }
        }

        return directedEdges;
    }

    private static void AddDirectedEdge(
        IDictionary<DirectedEdgeKey, DirectedMovementEdge> directedEdges,
        DirectedMovementEdge edge)
    {
        var key = new DirectedEdgeKey(edge.EdgeId, edge.FromNodeId, edge.ToNodeId);
        if (!directedEdges.TryAdd(key, edge))
        {
            throw new DomainRuleViolationException(
                $"Movement route edge direction must be unique. EdgeId: {edge.EdgeId}. FromNodeId: {edge.FromNodeId}. ToNodeId: {edge.ToNodeId}.");
        }
    }

    private static IEnumerable<MovementRouteSegment> ExpandRouteSegments(
        MovementLegInput leg,
        PathGraph pathGraph,
        IReadOnlyDictionary<DirectedEdgeKey, DirectedMovementEdge> directedEdges)
    {
        PathRoute route;
        try
        {
            route = pathGraph.GetRoute(leg.FromNodeId, leg.ToNodeId);
        }
        catch (DomainRuleViolationException exception)
        {
            throw new DomainRuleViolationException(
                $"Movement route could not be resolved. SegmentId: {leg.SegmentId}. ActorId: {leg.ActorId}. FromNodeId: {leg.FromNodeId}. ToNodeId: {leg.ToNodeId}. {exception.Message}");
        }

        if (route.EdgeIds.Count == 0)
        {
            yield break;
        }

        var durationMs = leg.EndMs - leg.StartMs;
        var previousEndMs = leg.StartMs;
        long cumulativeDistanceMm = 0;

        for (var index = 0; index < route.EdgeIds.Count; index++)
        {
            var edgeId = route.EdgeIds[index];
            var fromNodeId = route.PathNodeIds[index];
            var toNodeId = route.PathNodeIds[index + 1];
            var edge = LookupDirectedEdge(leg, directedEdges, edgeId, fromNodeId, toNodeId);

            cumulativeDistanceMm = checked(cumulativeDistanceMm + edge.DistanceMm);
            var segmentEndMs = index == route.EdgeIds.Count - 1
                ? leg.EndMs
                : leg.StartMs + ScaleDuration(durationMs, cumulativeDistanceMm, route.TotalDistanceMm);
            var segmentId = route.EdgeIds.Count == 1
                ? leg.SegmentId
                : $"{leg.SegmentId}-{index + 1:000}";

            yield return new MovementRouteSegment(
                segmentId,
                leg.ActorId,
                leg.OperationId ?? string.Empty,
                fromNodeId,
                toNodeId,
                previousEndMs,
                segmentEndMs,
                ConvertMillimetersToMeters(edge.DistanceMm),
                [fromNodeId, toNodeId],
                [edgeId],
                segmentEndMs - previousEndMs);

            previousEndMs = segmentEndMs;
        }
    }

    private static DirectedMovementEdge LookupDirectedEdge(
        MovementLegInput leg,
        IReadOnlyDictionary<DirectedEdgeKey, DirectedMovementEdge> directedEdges,
        string edgeId,
        string fromNodeId,
        string toNodeId)
    {
        var key = new DirectedEdgeKey(edgeId, fromNodeId, toNodeId);
        if (directedEdges.TryGetValue(key, out var edge))
        {
            return edge;
        }

        throw new DomainRuleViolationException(
            $"Movement route returned an edge without matching direction metadata. SegmentId: {leg.SegmentId}. ActorId: {leg.ActorId}. EdgeId: {edgeId}. FromNodeId: {fromNodeId}. ToNodeId: {toNodeId}.");
    }

    private static long ConvertCoordinateToMillimeters(double coordinate)
    {
        if (double.IsNaN(coordinate) || double.IsInfinity(coordinate))
        {
            throw new DomainRuleViolationException(
                $"Movement graph coordinate must be finite. Coordinate: {coordinate}.");
        }

        return checked((long)Math.Round(coordinate, MidpointRounding.AwayFromZero));
    }

    private static long ConvertDistanceToMillimeters(double distanceM, string name)
    {
        if (double.IsNaN(distanceM) || double.IsInfinity(distanceM))
        {
            throw new DomainRuleViolationException($"{name} must be finite.");
        }

        var distanceMm = checked((long)Math.Round(
            distanceM * 1000,
            MidpointRounding.AwayFromZero));
        if (distanceMm <= 0)
        {
            throw new DomainRuleViolationException(
                $"{name} must be greater than zero after conversion to millimeters. DistanceMm: {distanceMm}.");
        }

        return distanceMm;
    }

    private static double ConvertMillimetersToMeters(long distanceMm)
    {
        return distanceMm / 1000.0;
    }

    private static long ScaleDuration(
        long durationMs,
        long cumulativeDistanceMm,
        long totalDistanceMm)
    {
        return (long)((decimal)durationMs * cumulativeDistanceMm / totalDistanceMm);
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

    private sealed record MovementRouteSegment(
        string SegmentId,
        string ActorId,
        string OperationId,
        string FromNodeId,
        string ToNodeId,
        long StartMs,
        long EndMs,
        double DistanceM,
        IReadOnlyList<string> PathNodeIds,
        IReadOnlyList<string> EdgeIds,
        long TravelTimeMs);

    private sealed record DirectedMovementEdge(
        string EdgeId,
        string FromNodeId,
        string ToNodeId,
        long DistanceMm);

    private readonly record struct DirectedEdgeKey(
        string EdgeId,
        string FromNodeId,
        string ToNodeId);
}
