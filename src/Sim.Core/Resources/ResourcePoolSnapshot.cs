namespace Sim.Core.Resources;

public sealed record ResourcePoolSnapshot(
    string PoolId,
    ResourceType ResourceType,
    int Capacity,
    int AvailableCount,
    int BusyCount,
    int WaitingCount,
    decimal Utilization,
    long TotalBusyTimeMs);
