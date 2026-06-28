namespace Sim.Core.Scenarios.Unified;

public sealed record WarehouseUnifiedOperationInterval(
    string OperationId,
    WarehouseUnifiedOperationType OperationType,
    string ResourceId,
    long StartedAtMs,
    long FinishedAtMs);

public sealed record WarehouseUnifiedOperationTelemetry(
    string OperationId,
    WarehouseUnifiedOperationType OperationType,
    string ResourceId,
    long RequestedAtMs,
    long StartedAtMs,
    long FinishedAtMs,
    long WaitingTimeMs,
    long DurationMs,
    string SkuId,
    decimal InventoryDelta);

public sealed record WarehouseUnifiedOperationResult(
    IReadOnlyList<WarehouseUnifiedOperationInterval> OperationIntervals,
    IReadOnlyList<WarehouseUnifiedOperationTelemetry> OperationTelemetry,
    WarehouseUnifiedCustomerKpiSummary CustomerKpiSummary,
    IReadOnlyDictionary<WarehouseUnifiedOperationType, WarehouseUnifiedCustomerKpiSummary> CustomerKpiSummaryByOperationType,
    IReadOnlyDictionary<string, WarehouseUnifiedResourceKpiSummary> ResourceKpiSummaryByResourceId,
    IReadOnlyDictionary<string, decimal> FinalInventorySnapshot,
    string EventLogText);
