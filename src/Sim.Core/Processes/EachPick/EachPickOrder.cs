using Sim.Core.Domain;

namespace Sim.Core.Processes.EachPick;

public sealed record EachPickOrder
{
    public EachPickOrder(
        string orderId,
        string warehouseId,
        string skuId,
        decimal quantity,
        string sourceLocationId,
        string pickStationId,
        string stagingLocationId,
        long releasedAtMs)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new DomainRuleViolationException("EachPickOrder OrderId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            throw new DomainRuleViolationException("EachPickOrder WarehouseId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(skuId))
        {
            throw new DomainRuleViolationException("EachPickOrder SkuId cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleViolationException(
                $"EachPickOrder Quantity must be greater than zero. Quantity: {quantity}.");
        }

        if (string.IsNullOrWhiteSpace(sourceLocationId))
        {
            throw new DomainRuleViolationException("EachPickOrder SourceLocationId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(pickStationId))
        {
            throw new DomainRuleViolationException("EachPickOrder PickStationId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(stagingLocationId))
        {
            throw new DomainRuleViolationException("EachPickOrder StagingLocationId cannot be empty.");
        }

        if (releasedAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"EachPickOrder ReleasedAtMs cannot be negative. ReleasedAtMs: {releasedAtMs}.");
        }

        OrderId = orderId;
        WarehouseId = warehouseId;
        SkuId = skuId;
        Quantity = quantity;
        SourceLocationId = sourceLocationId;
        PickStationId = pickStationId;
        StagingLocationId = stagingLocationId;
        ReleasedAtMs = releasedAtMs;
    }

    public string OrderId { get; }

    public string WarehouseId { get; }

    public string SkuId { get; }

    public decimal Quantity { get; }

    public string SourceLocationId { get; }

    public string PickStationId { get; }

    public string StagingLocationId { get; }

    public long ReleasedAtMs { get; }
}
