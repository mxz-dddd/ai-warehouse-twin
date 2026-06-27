namespace Sim.Core.Scenarios.Unified;

public sealed record WarehouseUnifiedOperationInterval(
    string OperationId,
    WarehouseUnifiedOperationType OperationType,
    string ResourceId,
    long StartedAtMs,
    long FinishedAtMs);

public sealed record WarehouseUnifiedOperationResult(
    IReadOnlyList<WarehouseUnifiedOperationInterval> OperationIntervals,
    IReadOnlyDictionary<string, decimal> FinalInventorySnapshot,
    string EventLogText);
