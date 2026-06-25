using Sim.Core.Domain;

namespace Sim.Core.Processes.Inbound;

public sealed record InboundInventoryItem
{
    public InboundInventoryItem(
        string inventoryUnitId,
        string skuId,
        decimal quantity,
        string locationId,
        InventoryStatus status)
    {
        if (string.IsNullOrWhiteSpace(inventoryUnitId))
        {
            throw new DomainRuleViolationException("InboundInventoryItem InventoryUnitId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(skuId))
        {
            throw new DomainRuleViolationException("InboundInventoryItem SkuId cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleViolationException(
                $"InboundInventoryItem Quantity must be greater than zero. Quantity: {quantity}.");
        }

        if (string.IsNullOrWhiteSpace(locationId))
        {
            throw new DomainRuleViolationException("InboundInventoryItem LocationId cannot be empty.");
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
