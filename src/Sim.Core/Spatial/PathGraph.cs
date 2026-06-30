using Sim.Core.Domain;

namespace Sim.Core.Spatial;

public sealed class PathGraph
{
    private static readonly StringComparer IdComparer = StringComparer.Ordinal;

    private readonly IReadOnlyDictionary<string, PathGraphNode> nodesById;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<AdjacentEdge>> adjacencyByNodeId;

    public PathGraph(IReadOnlyList<PathGraphNode> nodes, IReadOnlyList<PathGraphEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        var orderedNodes = nodes
            .OrderBy(node => node.NodeId, IdComparer)
            .ToArray();

        var mutableNodesById = new Dictionary<string, PathGraphNode>(IdComparer);
        foreach (var node in orderedNodes)
        {
            ValidateNode(node);

            if (!mutableNodesById.TryAdd(node.NodeId, node))
            {
                throw new DomainRuleViolationException(
                    $"PathGraph node id must be unique. NodeId: {node.NodeId}.");
            }
        }

        var mutableAdjacency = mutableNodesById.Keys.ToDictionary(
            nodeId => nodeId,
            _ => new List<AdjacentEdge>(),
            IdComparer);

        var edgeIds = new HashSet<string>(IdComparer);
        foreach (var edge in edges.OrderBy(edge => edge.EdgeId, IdComparer))
        {
            ValidateEdge(edge);

            if (!edgeIds.Add(edge.EdgeId))
            {
                throw new DomainRuleViolationException(
                    $"PathGraph edge id must be unique. EdgeId: {edge.EdgeId}.");
            }

            if (!mutableNodesById.ContainsKey(edge.FromNodeId))
            {
                throw new DomainRuleViolationException(
                    $"PathGraph edge references unknown from node. EdgeId: {edge.EdgeId}. FromNodeId: {edge.FromNodeId}.");
            }

            if (!mutableNodesById.ContainsKey(edge.ToNodeId))
            {
                throw new DomainRuleViolationException(
                    $"PathGraph edge references unknown to node. EdgeId: {edge.EdgeId}. ToNodeId: {edge.ToNodeId}.");
            }

            mutableAdjacency[edge.FromNodeId].Add(new AdjacentEdge(
                edge.EdgeId,
                edge.FromNodeId,
                edge.ToNodeId,
                edge.DistanceMm));

            if (edge.Bidirectional)
            {
                mutableAdjacency[edge.ToNodeId].Add(new AdjacentEdge(
                    edge.EdgeId,
                    edge.ToNodeId,
                    edge.FromNodeId,
                    edge.DistanceMm));
            }
        }

        nodesById = mutableNodesById;
        adjacencyByNodeId = mutableAdjacency.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<AdjacentEdge>)pair.Value
                .OrderBy(edge => edge.DistanceMm)
                .ThenBy(edge => edge.ToNodeId, IdComparer)
                .ThenBy(edge => edge.EdgeId, IdComparer)
                .ToArray(),
            IdComparer);
    }

    public bool TryGetRoute(string fromNodeId, string toNodeId, out PathRoute route)
    {
        if (!nodesById.ContainsKey(fromNodeId) || !nodesById.ContainsKey(toNodeId))
        {
            route = null!;
            return false;
        }

        if (IdComparer.Equals(fromNodeId, toNodeId))
        {
            route = new PathRoute([fromNodeId], [], 0);
            return true;
        }

        var distances = nodesById.Keys.ToDictionary(
            nodeId => nodeId,
            _ => long.MaxValue,
            IdComparer);
        var previousByNodeId = new Dictionary<string, PreviousStep>(IdComparer);
        var unvisited = new HashSet<string>(nodesById.Keys, IdComparer);

        distances[fromNodeId] = 0;

        while (unvisited.Count > 0)
        {
            var currentNodeId = SelectNextNode(unvisited, distances);
            if (currentNodeId is null)
            {
                break;
            }

            unvisited.Remove(currentNodeId);

            if (IdComparer.Equals(currentNodeId, toNodeId))
            {
                break;
            }

            foreach (var edge in adjacencyByNodeId[currentNodeId])
            {
                if (!unvisited.Contains(edge.ToNodeId))
                {
                    continue;
                }

                var currentDistance = distances[currentNodeId];
                if (currentDistance == long.MaxValue)
                {
                    continue;
                }

                var candidateDistance = checked(currentDistance + edge.DistanceMm);
                var previousStep = new PreviousStep(currentNodeId, edge.EdgeId);

                if (candidateDistance < distances[edge.ToNodeId] ||
                    (candidateDistance == distances[edge.ToNodeId] &&
                     IsDeterministicTieBreakWinner(previousStep, previousByNodeId.GetValueOrDefault(edge.ToNodeId))))
                {
                    distances[edge.ToNodeId] = candidateDistance;
                    previousByNodeId[edge.ToNodeId] = previousStep;
                }
            }
        }

        if (distances[toNodeId] == long.MaxValue)
        {
            route = null!;
            return false;
        }

        route = BuildRoute(fromNodeId, toNodeId, distances[toNodeId], previousByNodeId);
        return true;
    }

    public PathRoute GetRoute(string fromNodeId, string toNodeId)
    {
        if (!nodesById.ContainsKey(fromNodeId))
        {
            throw new DomainRuleViolationException(
                $"PathGraph route start node is unknown. FromNodeId: {fromNodeId}.");
        }

        if (!nodesById.ContainsKey(toNodeId))
        {
            throw new DomainRuleViolationException(
                $"PathGraph route end node is unknown. ToNodeId: {toNodeId}.");
        }

        if (TryGetRoute(fromNodeId, toNodeId, out var route))
        {
            return route;
        }

        throw new DomainRuleViolationException(
            $"PathGraph route is unreachable. FromNodeId: {fromNodeId}. ToNodeId: {toNodeId}.");
    }

    private static void ValidateNode(PathGraphNode node)
    {
        if (string.IsNullOrWhiteSpace(node.NodeId))
        {
            throw new DomainRuleViolationException("PathGraphNode NodeId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(node.NodeType))
        {
            throw new DomainRuleViolationException(
                $"PathGraphNode NodeType cannot be empty. NodeId: {node.NodeId}.");
        }
    }

    private static void ValidateEdge(PathGraphEdge edge)
    {
        if (string.IsNullOrWhiteSpace(edge.EdgeId))
        {
            throw new DomainRuleViolationException("PathGraphEdge EdgeId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(edge.FromNodeId))
        {
            throw new DomainRuleViolationException(
                $"PathGraphEdge FromNodeId cannot be empty. EdgeId: {edge.EdgeId}.");
        }

        if (string.IsNullOrWhiteSpace(edge.ToNodeId))
        {
            throw new DomainRuleViolationException(
                $"PathGraphEdge ToNodeId cannot be empty. EdgeId: {edge.EdgeId}.");
        }

        if (edge.DistanceMm <= 0)
        {
            throw new DomainRuleViolationException(
                $"PathGraphEdge DistanceMm must be greater than zero. EdgeId: {edge.EdgeId}. DistanceMm: {edge.DistanceMm}.");
        }
    }

    private static string? SelectNextNode(
        HashSet<string> unvisited,
        IReadOnlyDictionary<string, long> distances)
    {
        string? selectedNodeId = null;
        var selectedDistance = long.MaxValue;

        foreach (var nodeId in unvisited.OrderBy(nodeId => nodeId, IdComparer))
        {
            var distance = distances[nodeId];
            if (distance < selectedDistance ||
                (distance == selectedDistance &&
                 selectedNodeId is not null &&
                 IdComparer.Compare(nodeId, selectedNodeId) < 0))
            {
                selectedNodeId = nodeId;
                selectedDistance = distance;
            }
        }

        return selectedDistance == long.MaxValue ? null : selectedNodeId;
    }

    private static bool IsDeterministicTieBreakWinner(
        PreviousStep candidate,
        PreviousStep? existing)
    {
        if (existing is null)
        {
            return true;
        }

        var edgeComparison = IdComparer.Compare(candidate.EdgeId, existing.EdgeId);
        if (edgeComparison != 0)
        {
            return edgeComparison < 0;
        }

        return IdComparer.Compare(candidate.PreviousNodeId, existing.PreviousNodeId) < 0;
    }

    private static PathRoute BuildRoute(
        string fromNodeId,
        string toNodeId,
        long totalDistanceMm,
        IReadOnlyDictionary<string, PreviousStep> previousByNodeId)
    {
        var pathNodeIds = new List<string> { toNodeId };
        var edgeIds = new List<string>();
        var currentNodeId = toNodeId;

        while (!IdComparer.Equals(currentNodeId, fromNodeId))
        {
            var previous = previousByNodeId[currentNodeId];
            edgeIds.Add(previous.EdgeId);
            currentNodeId = previous.PreviousNodeId;
            pathNodeIds.Add(currentNodeId);
        }

        pathNodeIds.Reverse();
        edgeIds.Reverse();

        return new PathRoute(pathNodeIds, edgeIds, totalDistanceMm);
    }

    private sealed record AdjacentEdge(
        string EdgeId,
        string FromNodeId,
        string ToNodeId,
        long DistanceMm);

    private sealed record PreviousStep(
        string PreviousNodeId,
        string EdgeId);
}
