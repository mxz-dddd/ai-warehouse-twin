using Sim.Core.Domain;
using Sim.Core.Resources;
using Xunit;

namespace Sim.Core.Tests.Resources;

public sealed class ResourceRequestTests
{
    [Fact]
    public void Constructor_CreatesResourceRequest()
    {
        var request = new ResourceRequest("req-1", "task-1", 10);

        Assert.Equal("req-1", request.RequestId);
        Assert.Equal("task-1", request.TaskId);
        Assert.Equal(10, request.RequestedAtMs);
    }

    [Fact]
    public void Constructor_Throws_ForEmptyRequestId()
    {
        Assert.Throws<DomainRuleViolationException>(() => new ResourceRequest("", "task-1", 10));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyTaskId()
    {
        Assert.Throws<DomainRuleViolationException>(() => new ResourceRequest("req-1", "", 10));
    }

    [Fact]
    public void Constructor_Throws_ForNegativeRequestedAtMs()
    {
        Assert.Throws<DomainRuleViolationException>(() => new ResourceRequest("req-1", "task-1", -1));
    }
}
