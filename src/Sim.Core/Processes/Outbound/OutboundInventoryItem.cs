using Sim.Core.Domain;

namespace Sim.Core.Processes.Outbound;

public sealed record OutboundInventoryItem
{
    public OutboundInventoryItem(
        string inventoryUnitId,
        string skuId,
        decimal quantity,
        string locationId,
        InventoryStatus status)
    {
        if (string.IsNullOrWhiteSpace(inventoryUnitId))
        {
            throw new DomainRuleViolationException("OutboundInventoryItem InventoryUnitId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(skuId))
        {
            throw new DomainRuleViolationException("OutboundInventoryItem SkuId cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleViolationException(
                $"OutboundInventoryItem Quantity must be greater than zero. Quantity: {quantity}.");
        }

        if (string.IsNullOrWhiteSpace(locationId))
        {
            throw new DomainRuleViolationException("OutboundInventoryItem LocationId cannot be empty.");
        }

        InventoryUnitId = inventoryUnitId;
        SkuId = skuId;
        Quantity = quantity;
        LocationId = locationId;
        Status = status;
    }

    public string InventoryUnitId { get; }

    public string SkuId { get; }

    public decimal Quantity { get; }

    public string LocationId { get; }

    public InventoryStatus Status { get; }
}
