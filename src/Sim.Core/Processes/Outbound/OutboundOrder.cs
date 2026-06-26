using Sim.Core.Domain;

namespace Sim.Core.Processes.Outbound;

public sealed record OutboundOrder
{
    public OutboundOrder(
        string orderId,
        string warehouseId,
        string skuId,
        decimal quantity,
        string sourceLocationId,
        string stagingLocationId,
        string dockLocationId,
        long releasedAtMs)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new DomainRuleViolationException("OutboundOrder OrderId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            throw new DomainRuleViolationException("OutboundOrder WarehouseId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(skuId))
        {
            throw new DomainRuleViolationException("OutboundOrder SkuId cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleViolationException(
                $"OutboundOrder Quantity must be greater than zero. Quantity: {quantity}.");
        }

        if (string.IsNullOrWhiteSpace(sourceLocationId))
        {
            throw new DomainRuleViolationException("OutboundOrder SourceLocationId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(stagingLocationId))
        {
            throw new DomainRuleViolationException("OutboundOrder StagingLocationId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(dockLocationId))
        {
            throw new DomainRuleViolationException("OutboundOrder DockLocationId cannot be empty.");
        }

        if (releasedAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"OutboundOrder ReleasedAtMs cannot be negative. ReleasedAtMs: {releasedAtMs}.");
        }

        OrderId = orderId;
        WarehouseId = warehouseId;
        SkuId = skuId;
        Quantity = quantity;
        SourceLocationId = sourceLocationId;
        StagingLocationId = stagingLocationId;
        DockLocationId = dockLocationId;
        ReleasedAtMs = releasedAtMs;
    }

    public string OrderId { get; }

    public string WarehouseId { get; }

    public string SkuId { get; }

    public decimal Quantity { get; }

    public string SourceLocationId { get; }

    public string StagingLocationId { get; }

    public string DockLocationId { get; }

    public long ReleasedAtMs { get; }
}
