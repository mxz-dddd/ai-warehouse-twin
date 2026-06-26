using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Xunit;

namespace Sim.Core.Tests.Processes.EachPick;

public sealed class EachPickProcessParametersTests
{
    [Fact]
    public void Constructor_CreatesParameters()
    {
        var parameters = new EachPickProcessParameters(10, 20, 30, 40);

        Assert.Equal(10, parameters.ToteBindDurationMs);
        Assert.Equal(20, parameters.TravelToStationDurationMs);
        Assert.Equal(30, parameters.PickServiceDurationMs);
        Assert.Equal(40, parameters.MoveToStagingDurationMs);
    }

    [Fact]
    public void Constructor_AllowsZeroDurations()
    {
        var parameters = new EachPickProcessParameters(0, 0, 0, 0);

        Assert.Equal(0, parameters.ToteBindDurationMs);
        Assert.Equal(0, parameters.TravelToStationDurationMs);
        Assert.Equal(0, parameters.PickServiceDurationMs);
        Assert.Equal(0, parameters.MoveToStagingDurationMs);
    }

    [Fact]
    public void Constructor_Throws_ForNegativeToteBindDuration()
    {
        Assert.Throws<DomainRuleViolationException>(() => new EachPickProcessParameters(-1, 0, 0, 0));
    }

    [Fact]
    public void Constructor_Throws_ForNegativeTravelToStationDuration()
    {
        Assert.Throws<DomainRuleViolationException>(() => new EachPickProcessParameters(0, -1, 0, 0));
    }

    [Fact]
    public void Constructor_Throws_ForNegativePickServiceDuration()
    {
        Assert.Throws<DomainRuleViolationException>(() => new EachPickProcessParameters(0, 0, -1, 0));
    }

    [Fact]
    public void Constructor_Throws_ForNegativeMoveToStagingDuration()
    {
        Assert.Throws<DomainRuleViolationException>(() => new EachPickProcessParameters(0, 0, 0, -1));
    }
}
