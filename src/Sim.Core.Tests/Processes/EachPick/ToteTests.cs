using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Xunit;

namespace Sim.Core.Tests.Processes.EachPick;

public sealed class ToteTests
{
    [Fact]
    public void Constructor_CreatesTote()
    {
        var tote = new Tote("tote-1", "order-1", "EMPTY_BOUND");

        Assert.Equal("tote-1", tote.ToteId);
        Assert.Equal("order-1", tote.OrderId);
        Assert.Equal("EMPTY_BOUND", tote.Status);
    }

    [Fact]
    public void Constructor_Throws_ForEmptyToteId()
    {
        Assert.Throws<DomainRuleViolationException>(() => new Tote("", "order-1", "EMPTY_BOUND"));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyOrderId()
    {
        Assert.Throws<DomainRuleViolationException>(() => new Tote("tote-1", "", "EMPTY_BOUND"));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyStatus()
    {
        Assert.Throws<DomainRuleViolationException>(() => new Tote("tote-1", "order-1", ""));
    }
}
