using System.Text.Json;
using System.Text.Json.Serialization;
using Sim.Core.Domain;

namespace Sim.Core.Spatial;

public static class LayoutGraphLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly StringComparer IdComparer = StringComparer.Ordinal;

    public static PathGraph Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new DomainRuleViolationException("LayoutGraph document JSON cannot be empty.");
        }

        var document = JsonSerializer.Deserialize<LayoutGraphDocument>(json, Options);
        if (document is null)
        {
            throw new DomainRuleViolationException("LayoutGraph document cannot be null.");
        }

        return Load(document);
    }

    private static PathGraph Load(LayoutGraphDocument document)
    {
        var nodes = ToCanonicalNodes(document);
        var edges = ToCanonicalEdges(document);

        if (nodes.Count == 0)
        {
            throw new DomainRuleViolationException("LayoutGraph document requires at least one node.");
        }

        if (edges.Count == 0)
        {
            throw new DomainRuleViolationException("LayoutGraph document requires at least one edge.");
        }

        var nodeIds = new HashSet<string>(IdComparer);
        var pathNodes = nodes
            .OrderBy(node => node.NodeId, IdComparer)
            .Select(node =>
            {
                ValidateNode(node);
                if (!nodeIds.Add(node.NodeId!))
                {
                    throw new DomainRuleViolationException(
                        $"LayoutGraph node id must be unique. NodeId: {node.NodeId}.");
                }

                return new PathGraphNode(
                    node.NodeId!,
                    node.NodeType!,
                    node.XMm!.Value,
                    node.YMm!.Value);
            })
            .ToArray();

        var edgeIds = new HashSet<string>(IdComparer);
        var pathEdges = edges
            .OrderBy(edge => edge.EdgeId, IdComparer)
            .Select(edge =>
            {
                ValidateEdge(edge);
                if (!edgeIds.Add(edge.EdgeId!))
                {
                    throw new DomainRuleViolationException(
                        $"LayoutGraph edge id must be unique. EdgeId: {edge.EdgeId}.");
                }

                EnsureEndpointExists(edge.EdgeId!, "FromNodeId", edge.FromNodeId!, nodeIds);
                EnsureEndpointExists(edge.EdgeId!, "ToNodeId", edge.ToNodeId!, nodeIds);

                return new PathGraphEdge(
                    edge.EdgeId!,
                    edge.FromNodeId!,
                    edge.ToNodeId!,
                    edge.DistanceMm!.Value,
                    edge.Bidirectional!.Value);
            })
            .ToArray();

        return new PathGraph(pathNodes, pathEdges);
    }

    private static IReadOnlyList<LayoutGraphNodeDocument> ToCanonicalNodes(
        LayoutGraphDocument document)
    {
        if (document.Nodes is not null)
        {
            return document.Nodes;
        }

        return document.PathNodes?
            .Select(node => new LayoutGraphNodeDocument
            {
                NodeId = node.NodeId,
                NodeType = node.NodeType,
                XMm = node.X,
                YMm = node.Y,
            })
            .ToArray() ?? [];
    }

    private static IReadOnlyList<LayoutGraphEdgeDocument> ToCanonicalEdges(
        LayoutGraphDocument document)
    {
        if (document.Edges is not null)
        {
            return document.Edges;
        }

        return document.PathEdges?
            .Select(edge => new LayoutGraphEdgeDocument
            {
                EdgeId = edge.EdgeId,
                FromNodeId = edge.FromNodeId,
                ToNodeId = edge.ToNodeId,
                DistanceMm = edge.DistanceMm,
                Bidirectional = edge.Bidirectional,
            })
            .ToArray() ?? [];
    }

    private static void ValidateNode(LayoutGraphNodeDocument node)
    {
        if (string.IsNullOrWhiteSpace(node.NodeId))
        {
            throw new DomainRuleViolationException("LayoutGraph node nodeId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(node.NodeType))
        {
            throw new DomainRuleViolationException(
                $"LayoutGraph node nodeType cannot be empty. NodeId: {node.NodeId}.");
        }

        if (node.XMm is null)
        {
            throw new DomainRuleViolationException(
                $"LayoutGraph node xMm is required. NodeId: {node.NodeId}.");
        }

        if (node.YMm is null)
        {
            throw new DomainRuleViolationException(
                $"LayoutGraph node yMm is required. NodeId: {node.NodeId}.");
        }
    }

    private static void ValidateEdge(LayoutGraphEdgeDocument edge)
    {
        if (string.IsNullOrWhiteSpace(edge.EdgeId))
        {
            throw new DomainRuleViolationException("LayoutGraph edge edgeId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(edge.FromNodeId))
        {
            throw new DomainRuleViolationException(
                $"LayoutGraph edge fromNodeId cannot be empty. EdgeId: {edge.EdgeId}.");
        }

        if (string.IsNullOrWhiteSpace(edge.ToNodeId))
        {
            throw new DomainRuleViolationException(
                $"LayoutGraph edge toNodeId cannot be empty. EdgeId: {edge.EdgeId}.");
        }

        if (edge.DistanceMm is null || edge.DistanceMm <= 0)
        {
            throw new DomainRuleViolationException(
                $"LayoutGraph edge distanceMm must be greater than zero. EdgeId: {edge.EdgeId}. DistanceMm: {edge.DistanceMm?.ToString() ?? "null"}.");
        }

        if (edge.Bidirectional is null)
        {
            throw new DomainRuleViolationException(
                $"LayoutGraph edge bidirectional is required. EdgeId: {edge.EdgeId}.");
        }
    }

    private static void EnsureEndpointExists(
        string edgeId,
        string endpointName,
        string nodeId,
        IReadOnlySet<string> nodeIds)
    {
        if (!nodeIds.Contains(nodeId))
        {
            throw new DomainRuleViolationException(
                $"LayoutGraph edge references unknown {endpointName}. EdgeId: {edgeId}. NodeId: {nodeId}.");
        }
    }

    private sealed record LayoutGraphDocument
    {
        public IReadOnlyList<LayoutGraphNodeDocument>? Nodes { get; init; }

        public IReadOnlyList<LayoutGraphEdgeDocument>? Edges { get; init; }

        [JsonPropertyName("path_nodes")]
        public IReadOnlyList<WarehousePathNodeDocument>? PathNodes { get; init; }

        [JsonPropertyName("path_edges")]
        public IReadOnlyList<WarehousePathEdgeDocument>? PathEdges { get; init; }
    }

    private sealed record LayoutGraphNodeDocument
    {
        public string? NodeId { get; init; }

        public string? NodeType { get; init; }

        public long? XMm { get; init; }

        public long? YMm { get; init; }
    }

    private sealed record LayoutGraphEdgeDocument
    {
        public string? EdgeId { get; init; }

        public string? FromNodeId { get; init; }

        public string? ToNodeId { get; init; }

        public long? DistanceMm { get; init; }

        public bool? Bidirectional { get; init; }
    }

    private sealed record WarehousePathNodeDocument
    {
        [JsonPropertyName("node_id")]
        public string? NodeId { get; init; }

        [JsonPropertyName("node_type")]
        public string? NodeType { get; init; }

        [JsonPropertyName("x")]
        public long? X { get; init; }

        [JsonPropertyName("y")]
        public long? Y { get; init; }
    }

    private sealed record WarehousePathEdgeDocument
    {
        [JsonPropertyName("edge_id")]
        public string? EdgeId { get; init; }

        [JsonPropertyName("from_node_id")]
        public string? FromNodeId { get; init; }

        [JsonPropertyName("to_node_id")]
        public string? ToNodeId { get; init; }

        [JsonPropertyName("distance_mm")]
        public long? DistanceMm { get; init; }

        [JsonPropertyName("bidirectional")]
        public bool? Bidirectional { get; init; }
    }
}
