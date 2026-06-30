namespace Sim.Core.Spatial;

public sealed record PathGraphEdge(
    string EdgeId,
    string FromNodeId,
    string ToNodeId,
    long DistanceMm,
    bool Bidirectional);
