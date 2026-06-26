using Sim.Core.Domain;
using Sim.Core.Processes.Outbound;
using Xunit;

namespace Sim.Core.Tests.Processes.Outbound;

public sealed class OutboundOrderTests
{
    [Fact]
    public void Constructor_CreatesOutboundOrder()
    {
        var order = Order();

        Assert.Equal("order-1", order.OrderId);
        Assert.Equal("warehouse-1", order.WarehouseId);
        Assert.Equal("sku-1", order.SkuId);
        Assert.Equal(5m, order.Quantity);
    }

    [Fact]
    public void Constructor_Throws_ForEmptyOrderId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(orderId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyWarehouseId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(warehouseId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForEmptySkuId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(skuId: ""));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Throws_WhenQuantityIsNotPositive(decimal quantity)
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(quantity: quantity));
    }

    [Fact]
    public void Constructor_Throws_ForEmptySourceLocationId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(sourceLocationId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyStagingLocationId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(stagingLocationId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyDockLocationId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(dockLocationId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForNegativeReleasedAtMs()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(releasedAtMs: -1));
    }

    private static OutboundOrder Order(
        string orderId = "order-1",
        string warehouseId = "warehouse-1",
        string skuId = "sku-1",
        decimal quantity = 5m,
        string sourceLocationId = "pick-1",
        string stagingLocationId = "stage-1",
        string dockLocationId = "dock-1",
        long releasedAtMs = 10)
    {
        return new OutboundOrder(
            orderId,
            warehouseId,
            skuId,
            quantity,
            sourceLocationId,
            stagingLocationId,
            dockLocationId,
            releasedAtMs);
    }
}
