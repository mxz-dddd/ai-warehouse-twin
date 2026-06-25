using Sim.Core.Domain;
using Sim.Core.Invariants;
using Xunit;

namespace Sim.Core.Tests.Invariants;

public sealed class LocationCapacityInvariantTests
{
    [Fact]
    public void EnsureWithinCapacity_AllowsValuesBelowCapacity()
    {
        LocationCapacityInvariant.EnsureWithinCapacity(90m, 80m, 100m, 100m);
    }

    [Fact]
    public void EnsureWithinCapacity_AllowsValuesEqualToCapacity()
    {
        LocationCapacityInvariant.EnsureWithinCapacity(100m, 100m, 100m, 100m);
    }

    [Fact]
    public void EnsureWithinCapacity_Throws_WhenWeightExceedsCapacity()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => LocationCapacityInvariant.EnsureWithinCapacity(101m, 50m, 100m, 100m));
    }

    [Fact]
    public void EnsureWithinCapacity_Throws_WhenVolumeExceedsCapacity()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => LocationCapacityInvariant.EnsureWithinCapacity(50m, 101m, 100m, 100m));
    }

    [Fact]
    public void EnsureWithinCapacity_Throws_WhenMaxWeightIsNegative()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => LocationCapacityInvariant.EnsureWithinCapacity(0m, 0m, -1m, 100m));
    }

    [Fact]
    public void EnsureWithinCapacity_Throws_WhenMaxVolumeIsNegative()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => LocationCapacityInvariant.EnsureWithinCapacity(0m, 0m, 100m, -1m));
    }
}
