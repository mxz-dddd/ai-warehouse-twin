using Sim.Core.Des;
using Sim.Core.Domain;
using Xunit;

namespace Sim.Core.Tests.Des;

public sealed class SimClockTests
{
    [Fact]
    public void Constructor_DefaultsToZero()
    {
        var clock = new SimClock();

        Assert.Equal(0, clock.NowMs);
    }

    [Fact]
    public void Constructor_AllowsPositiveInitialTime()
    {
        var clock = new SimClock(42);

        Assert.Equal(42, clock.NowMs);
    }

    [Fact]
    public void Constructor_Throws_ForNegativeInitialTime()
    {
        Assert.Throws<DomainRuleViolationException>(() => new SimClock(-1));
    }

    [Fact]
    public void AdvanceTo_AllowsLargerTime()
    {
        var advanced = new SimClock(10).AdvanceTo(25);

        Assert.Equal(25, advanced.NowMs);
    }

    [Fact]
    public void AdvanceTo_AllowsSameTime()
    {
        var advanced = new SimClock(10).AdvanceTo(10);

        Assert.Equal(10, advanced.NowMs);
    }

    [Fact]
    public void AdvanceTo_Throws_WhenTargetIsEarlier()
    {
        Assert.Throws<DomainRuleViolationException>(() => new SimClock(10).AdvanceTo(9));
    }
}
