using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Xunit;

namespace Sim.Core.Tests.Processes.EachPick;

public sealed class EachPickOrderTests
{
    [Fact]
    public void Constructor_CreatesEachPickOrder()
    {
        var order = Order();

        Assert.Equal("order-1", order.OrderId);
        Assert.Equal("warehouse-1", order.WarehouseId);
        Assert.Equal("sku-1", order.SkuId);
        Assert.Equal(5m, order.Quantity);
        Assert.Equal("pick-face-1", order.SourceLocationId);
        Assert.Equal("station-1", order.PickStationId);
        Assert.Equal("stage-1", order.StagingLocationId);
        Assert.Equal(10, order.ReleasedAtMs);
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
    public void Constructor_Throws_ForEmptyPickStationId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(pickStationId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyStagingLocationId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(stagingLocationId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForNegativeReleasedAtMs()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order(releasedAtMs: -1));
    }

    private static EachPickOrder Order(
        string orderId = "order-1",
        string warehouseId = "warehouse-1",
        string skuId = "sku-1",
        decimal quantity = 5m,
        string sourceLocationId = "pick-face-1",
        string pickStationId = "station-1",
        string stagingLocationId = "stage-1",
        long releasedAtMs = 10)
    {
        return new EachPickOrder(
            orderId,
            warehouseId,
            skuId,
            quantity,
            sourceLocationId,
            pickStationId,
            stagingLocationId,
            releasedAtMs);
    }
}
