using Sim.Core.Domain;
using Xunit;

namespace Sim.Core.Tests.Domain;

public sealed class InventoryQuantityTests
{
    [Fact]
    public void Constructor_AllowsZero()
    {
        var quantity = new InventoryQuantity(0m);

        Assert.Equal(0m, quantity.Value);
    }

    [Fact]
    public void Constructor_AllowsPositiveValue()
    {
        var quantity = new InventoryQuantity(12.5m);

        Assert.Equal(12.5m, quantity.Value);
    }

    [Fact]
    public void Constructor_Throws_ForNegativeValue()
    {
        Assert.Throws<DomainRuleViolationException>(() => new InventoryQuantity(-1m));
    }

    [Fact]
    public void Add_ReturnsSum()
    {
        var result = new InventoryQuantity(2m).Add(new InventoryQuantity(3.5m));

        Assert.Equal(5.5m, result.Value);
    }

    [Fact]
    public void Subtract_ReturnsDifference()
    {
        var result = new InventoryQuantity(7m).Subtract(new InventoryQuantity(2.25m));

        Assert.Equal(4.75m, result.Value);
    }

    [Fact]
    public void Subtract_Throws_WhenResultWouldBeNegative()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new InventoryQuantity(1m).Subtract(new InventoryQuantity(2m)));
    }
}
