namespace Sim.Core.Spatial;

public sealed record PathGraphNode(
    string NodeId,
    string NodeType,
    long XMm,
    long YMm);
