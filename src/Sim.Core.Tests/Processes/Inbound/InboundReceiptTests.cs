using Sim.Core.Domain;
using Sim.Core.Processes.Inbound;
using Xunit;

namespace Sim.Core.Tests.Processes.Inbound;

public sealed class InboundReceiptTests
{
    [Fact]
    public void Constructor_CreatesInboundReceipt()
    {
        var receipt = Receipt();

        Assert.Equal("receipt-1", receipt.ReceiptId);
        Assert.Equal("warehouse-1", receipt.WarehouseId);
        Assert.Equal("sku-1", receipt.SkuId);
        Assert.Equal(5m, receipt.Quantity);
    }

    [Fact]
    public void Constructor_Throws_ForEmptyReceiptId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Receipt(receiptId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyWarehouseId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Receipt(warehouseId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForEmptySkuId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Receipt(skuId: ""));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Throws_WhenQuantityIsNotPositive(decimal quantity)
    {
        Assert.Throws<DomainRuleViolationException>(() => Receipt(quantity: quantity));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyStagingLocationId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Receipt(stagingLocationId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyTargetLocationId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Receipt(targetLocationId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForNegativeArrivesAtMs()
    {
        Assert.Throws<DomainRuleViolationException>(() => Receipt(arrivesAtMs: -1));
    }

    private static InboundReceipt Receipt(
        string receiptId = "receipt-1",
        string warehouseId = "warehouse-1",
        string skuId = "sku-1",
        decimal quantity = 5m,
        string stagingLocationId = "stage-1",
        string targetLocationId = "loc-1",
        long arrivesAtMs = 10)
    {
        return new InboundReceipt(
            receiptId,
            warehouseId,
            skuId,
            quantity,
            stagingLocationId,
            targetLocationId,
            arrivesAtMs);
    }
}
