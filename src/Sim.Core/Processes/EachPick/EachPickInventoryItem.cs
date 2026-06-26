using Sim.Core.Domain;

namespace Sim.Core.Processes.EachPick;

public sealed record EachPickInventoryItem
{
    public EachPickInventoryItem(
        string inventoryUnitId,
        string skuId,
        decimal quantity,
        string locationId,
        InventoryStatus status)
    {
        if (string.IsNullOrWhiteSpace(inventoryUnitId))
        {
            throw new DomainRuleViolationException("EachPickInventoryItem InventoryUnitId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(skuId))
        {
            throw new DomainRuleViolationException("EachPickInventoryItem SkuId cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleViolationException(
                $"EachPickInventoryItem Quantity must be greater than zero. Quantity: {quantity}.");
        }

        if (string.IsNullOrWhiteSpace(locationId))
        {
            throw new DomainRuleViolationException("EachPickInventoryItem LocationId cannot be empty.");
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
