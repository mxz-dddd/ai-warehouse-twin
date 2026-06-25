using Sim.Core.Domain;

namespace Sim.Core.Processes.Inbound;

public sealed record InboundReceipt
{
    public InboundReceipt(
        string receiptId,
        string warehouseId,
        string skuId,
        decimal quantity,
        string stagingLocationId,
        string targetLocationId,
        long arrivesAtMs)
    {
        if (string.IsNullOrWhiteSpace(receiptId))
        {
            throw new DomainRuleViolationException("InboundReceipt ReceiptId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            throw new DomainRuleViolationException("InboundReceipt WarehouseId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(skuId))
        {
            throw new DomainRuleViolationException("InboundReceipt SkuId cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleViolationException(
                $"InboundReceipt Quantity must be greater than zero. Quantity: {quantity}.");
        }

        if (string.IsNullOrWhiteSpace(stagingLocationId))
        {
            throw new DomainRuleViolationException("InboundReceipt StagingLocationId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(targetLocationId))
        {
            throw new DomainRuleViolationException("InboundReceipt TargetLocationId cannot be empty.");
        }

        if (arrivesAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"InboundReceipt ArrivesAtMs cannot be negative. ArrivesAtMs: {arrivesAtMs}.");
        }

        ReceiptId = receiptId;
        WarehouseId = warehouseId;
        SkuId = skuId;
        Quantity = quantity;
        StagingLocationId = stagingLocationId;
        TargetLocationId = targetLocationId;
        ArrivesAtMs = arrivesAtMs;
    }

    public string ReceiptId { get; }

    public string WarehouseId { get; }

    public string SkuId { get; }

    public decimal Quantity { get; }

    public string StagingLocationId { get; }

    public string TargetLocationId { get; }

    public long ArrivesAtMs { get; }
}
