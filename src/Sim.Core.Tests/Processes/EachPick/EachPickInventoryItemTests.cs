using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Xunit;

namespace Sim.Core.Tests.Processes.EachPick;

public sealed class EachPickInventoryItemTests
{
    [Fact]
    public void Constructor_CreatesInventoryItem()
    {
        var item = Item();

        Assert.Equal("inv-1", item.InventoryUnitId);
        Assert.Equal("sku-1", item.SkuId);
        Assert.Equal(5m, item.Quantity);
        Assert.Equal("pick-face-1", item.LocationId);
        Assert.Equal(InventoryStatus.Available, item.Status);
    }

    [Fact]
    public void Constructor_Throws_ForEmptyInventoryUnitId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Item(inventoryUnitId: ""));
    }

    [Fact]
    public void Constructor_Throws_ForEmptySkuId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Item(skuId: ""));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Throws_WhenQuantityIsNotPositive(decimal quantity)
    {
        Assert.Throws<DomainRuleViolationException>(() => Item(quantity: quantity));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyLocationId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Item(locationId: ""));
    }

    [Fact]
    public void Constructor_AllowsAvailableStatus()
    {
        var item = Item(status: InventoryStatus.Available);

        Assert.Equal(InventoryStatus.Available, item.Status);
    }

    [Fact]
    public void Constructor_AllowsNonAvailableStatus()
    {
        var item = Item(status: InventoryStatus.Allocated);

        Assert.Equal(InventoryStatus.Allocated, item.Status);
    }

    private static EachPickInventoryItem Item(
        string inventoryUnitId = "inv-1",
        string skuId = "sku-1",
        decimal quantity = 5m,
        string locationId = "pick-face-1",
        InventoryStatus status = InventoryStatus.Available)
    {
        return new EachPickInventoryItem(
            inventoryUnitId,
            skuId,
            quantity,
            locationId,
            status);
    }
}
