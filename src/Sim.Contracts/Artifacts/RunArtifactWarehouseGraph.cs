namespace Sim.Contracts.Artifacts;

public sealed record RunArtifactWarehouseGraph
{
    public IReadOnlyList<RunArtifactWarehouseGraphNode> Nodes { get; init; } =
        Array.Empty<RunArtifactWarehouseGraphNode>();

    public IReadOnlyList<RunArtifactWarehouseGraphEdge> Edges { get; init; } =
        Array.Empty<RunArtifactWarehouseGraphEdge>();
}

public sealed record RunArtifactWarehouseGraphNode
{
    public required string NodeId { get; init; }

    public required string NodeType { get; init; }

    public decimal X { get; init; }

    public decimal Y { get; init; }
}

public sealed record RunArtifactWarehouseGraphEdge
{
    public required string EdgeId { get; init; }

    public required string FromNodeId { get; init; }

    public required string ToNodeId { get; init; }

    public decimal DistanceM { get; init; }

    public long TravelTimeMs { get; init; }

    public bool Bidirectional { get; init; }
}
