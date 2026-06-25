using Sim.Core.Domain;
using Sim.Core.Resources;
using Xunit;

namespace Sim.Core.Tests.Resources;

public sealed class ResourceUnitTests
{
    [Fact]
    public void Constructor_CreatesResourceUnit()
    {
        var unit = new ResourceUnit("worker-1", ResourceType.Worker, "Worker 1");

        Assert.Equal("worker-1", unit.ResourceId);
        Assert.Equal(ResourceType.Worker, unit.ResourceType);
        Assert.Equal("Worker 1", unit.Name);
    }

    [Fact]
    public void Constructor_Throws_ForEmptyResourceId()
    {
        Assert.Throws<DomainRuleViolationException>(() => new ResourceUnit("", ResourceType.Worker, "Worker 1"));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyName()
    {
        Assert.Throws<DomainRuleViolationException>(() => new ResourceUnit("worker-1", ResourceType.Worker, ""));
    }
}
