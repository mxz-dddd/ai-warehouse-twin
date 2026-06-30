namespace Sim.Core.Movement;

public sealed record MovementArtifactGenerationRequest(
    string ScenarioId,
    string? RunId,
    int Seed,
    string SourceRunArtifact,
    IReadOnlyList<MovementGraphNodeInput> Nodes,
    IReadOnlyList<MovementGraphEdgeInput> Edges,
    IReadOnlyList<MovementActorInput> Actors,
    IReadOnlyList<MovementLegInput> MovementLegs,
    string GraphSource,
    string GeneratorVersion);

public sealed record MovementGraphNodeInput(
    string NodeId,
    string NodeType,
    double X,
    double Y);

public sealed record MovementGraphEdgeInput(
    string EdgeId,
    string FromNodeId,
    string ToNodeId,
    double DistanceM,
    long TravelTimeMs,
    bool Bidirectional);

public sealed record MovementActorInput(
    string ActorId,
    string ActorType,
    string? ResourceId,
    string InitialNodeId,
    double? Capacity,
    string? LoadState);

public sealed record MovementLegInput(
    string SegmentId,
    string ActorId,
    string? OperationId,
    string FromNodeId,
    string ToNodeId,
    long StartMs,
    long EndMs,
    double DistanceM,
    IReadOnlyList<string> PathNodeIds,
    IReadOnlyList<string> EdgeIds,
    long TravelTimeMs);
