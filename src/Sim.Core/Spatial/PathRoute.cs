namespace Sim.Core.Spatial;

public sealed record PathRoute(
    IReadOnlyList<string> PathNodeIds,
    IReadOnlyList<string> EdgeIds,
    long TotalDistanceMm);
