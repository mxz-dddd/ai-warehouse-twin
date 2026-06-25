using Sim.Core.Domain;
using Sim.Core.Invariants;
using Xunit;

namespace Sim.Core.Tests.Invariants;

public sealed class InventoryInvariantsTests
{
    [Fact]
    public void EnsureNonNegative_AllowsNonNegativeQuantity()
    {
        InventoryInvariants.EnsureNonNegative(0m);
        InventoryInvariants.EnsureNonNegative(10m);
    }

    [Fact]
    public void EnsureNonNegative_Throws_ForNegativeQuantity()
    {
        Assert.Throws<DomainRuleViolationException>(() => InventoryInvariants.EnsureNonNegative(-0.1m));
    }

    [Fact]
    public void EnsureConserved_AllowsEqualTotals()
    {
        InventoryInvariants.EnsureConserved(10.25m, 10.25m);
    }

    [Fact]
    public void EnsureConserved_Throws_ForDifferentTotals()
    {
        Assert.Throws<DomainRuleViolationException>(() => InventoryInvariants.EnsureConserved(10m, 9.99m));
    }
}
