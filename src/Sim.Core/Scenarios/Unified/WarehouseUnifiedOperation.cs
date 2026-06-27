using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Unified;

public enum WarehouseUnifiedOperationType
{
    Inbound,
    Outbound,
    EachPick
}

public sealed record WarehouseUnifiedOperation
{
    public WarehouseUnifiedOperation(
        string operationId,
        WarehouseUnifiedOperationType operationType,
        long requestedAtMs,
        string resourceId,
        long durationMs,
        string skuId,
        decimal inventoryDelta)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            throw new DomainRuleViolationException(
                "Warehouse unified operation OperationId cannot be empty.");
        }

        if (requestedAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Warehouse unified operation request time cannot be negative. RequestedAtMs: {requestedAtMs}.");
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Warehouse unified operation ResourceId cannot be empty.");
        }

        if (durationMs <= 0)
        {
            throw new DomainRuleViolationException(
                $"Warehouse unified operation duration must be positive. DurationMs: {durationMs}.");
        }

        if (string.IsNullOrWhiteSpace(skuId))
        {
            throw new DomainRuleViolationException(
                "Warehouse unified operation SkuId cannot be empty.");
        }

        if (operationType == WarehouseUnifiedOperationType.Inbound &&
            inventoryDelta <= 0m)
        {
            throw new DomainRuleViolationException(
                $"Inbound operation inventory delta must be positive. InventoryDelta: {inventoryDelta}.");
        }

        if (operationType != WarehouseUnifiedOperationType.Inbound &&
            inventoryDelta >= 0m)
        {
            throw new DomainRuleViolationException(
                $"Outbound and each-pick operation inventory delta must be negative. InventoryDelta: {inventoryDelta}.");
        }

        OperationId = operationId;
        OperationType = operationType;
        RequestedAtMs = requestedAtMs;
        ResourceId = resourceId;
        DurationMs = durationMs;
        SkuId = skuId;
        InventoryDelta = inventoryDelta;
    }

    public string OperationId { get; }

    public WarehouseUnifiedOperationType OperationType { get; }

    public long RequestedAtMs { get; }

    public string ResourceId { get; }

    public long DurationMs { get; }

    public string SkuId { get; }

    public decimal InventoryDelta { get; }
}
