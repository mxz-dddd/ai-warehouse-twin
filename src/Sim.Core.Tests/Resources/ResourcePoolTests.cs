using Sim.Core.Domain;
using Sim.Core.Resources;
using Xunit;

namespace Sim.Core.Tests.Resources;

public sealed class ResourcePoolTests
{
    [Fact]
    public void Constructor_CreatesResourcePool()
    {
        var pool = CreatePool(capacity: 2);

        Assert.Equal("workers", pool.PoolId);
        Assert.Equal(ResourceType.Worker, pool.ResourceType);
        Assert.Equal(2, pool.Capacity);
        Assert.Equal(2, pool.AvailableCount);
        Assert.Equal(0, pool.BusyCount);
        Assert.Equal(0, pool.WaitingCount);
    }

    [Fact]
    public void Constructor_Throws_ForEmptyPoolId()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new ResourcePool("", ResourceType.Worker, [Worker("worker-1")]));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyResources()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new ResourcePool("workers", ResourceType.Worker, []));
    }

    [Fact]
    public void Constructor_Throws_ForMismatchedResourceType()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new ResourcePool("workers", ResourceType.Worker, [new ResourceUnit("dock-1", ResourceType.Dock, "Dock 1")]));
    }

    [Fact]
    public void Constructor_Throws_ForDuplicateResourceId()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new ResourcePool("workers", ResourceType.Worker, [Worker("worker-1"), Worker("worker-1")]));
    }

    [Fact]
    public void TryAcquire_ReturnsLease_WhenResourceAvailable()
    {
        var pool = CreatePool(capacity: 1);

        var lease = pool.TryAcquire(Request("req-1"), nowMs: 10);

        Assert.NotNull(lease);
        Assert.Equal("req-1", lease!.RequestId);
        Assert.Equal(0, pool.AvailableCount);
        Assert.Equal(1, pool.BusyCount);
    }

    [Fact]
    public void TryAcquire_ReturnsNull_WhenNoResourceAvailable_AndDoesNotQueue()
    {
        var pool = CreatePool(capacity: 1);
        pool.TryAcquire(Request("req-1"), nowMs: 10);

        var lease = pool.TryAcquire(Request("req-2"), nowMs: 11);

        Assert.Null(lease);
        Assert.Equal(0, pool.AvailableCount);
        Assert.Equal(1, pool.BusyCount);
        Assert.Equal(0, pool.WaitingCount);
    }

    [Fact]
    public void TryAcquire_Throws_ForNullRequest()
    {
        var pool = CreatePool(capacity: 1);

        Assert.Throws<ArgumentNullException>(() => pool.TryAcquire(null!, nowMs: 10));
    }

    [Fact]
    public void TryAcquire_Throws_ForNegativeNowMs()
    {
        var pool = CreatePool(capacity: 1);

        Assert.Throws<DomainRuleViolationException>(() => pool.TryAcquire(Request("req-1"), nowMs: -1));
    }

    [Fact]
    public void AcquireOrQueue_ReturnsLease_WhenResourceAvailable()
    {
        var pool = CreatePool(capacity: 1);

        var lease = pool.AcquireOrQueue(Request("req-1"), nowMs: 10);

        Assert.NotNull(lease);
        Assert.Equal(0, pool.WaitingCount);
    }

    [Fact]
    public void AcquireOrQueue_QueuesRequest_WhenNoResourceAvailable()
    {
        var pool = CreatePool(capacity: 1);
        pool.AcquireOrQueue(Request("req-1"), nowMs: 10);

        var lease = pool.AcquireOrQueue(Request("req-2"), nowMs: 11);

        Assert.Null(lease);
        Assert.Equal(1, pool.WaitingCount);
    }

    [Fact]
    public void AcquireOrQueue_Throws_ForDuplicateWaitingRequestId()
    {
        var pool = CreatePool(capacity: 1);
        pool.AcquireOrQueue(Request("req-1"), nowMs: 10);
        pool.AcquireOrQueue(Request("req-2"), nowMs: 11);

        Assert.Throws<DomainRuleViolationException>(() => pool.AcquireOrQueue(Request("req-2"), nowMs: 12));
    }

    [Fact]
    public void Release_ReturnsResourceToAvailable_WhenNoRequestWaiting()
    {
        var pool = CreatePool(capacity: 1);
        var lease = pool.AcquireOrQueue(Request("req-1"), nowMs: 10)!;

        var nextLease = pool.Release(lease, nowMs: 20);

        Assert.Null(nextLease);
        Assert.Equal(1, pool.AvailableCount);
        Assert.Equal(0, pool.BusyCount);
    }

    [Fact]
    public void Release_AssignsReleasedResourceToFirstWaitingRequest()
    {
        var pool = CreatePool(capacity: 1);
        var lease = pool.AcquireOrQueue(Request("req-1"), nowMs: 10)!;
        pool.AcquireOrQueue(Request("req-2"), nowMs: 11);

        var nextLease = pool.Release(lease, nowMs: 20);

        Assert.NotNull(nextLease);
        Assert.Equal("req-2", nextLease!.RequestId);
        Assert.Equal(0, pool.AvailableCount);
        Assert.Equal(1, pool.BusyCount);
        Assert.Equal(0, pool.WaitingCount);
    }

    [Fact]
    public void Release_PreservesFifoWaitingOrder()
    {
        var pool = CreatePool(capacity: 1);
        var firstLease = pool.AcquireOrQueue(Request("req-1"), nowMs: 10)!;
        pool.AcquireOrQueue(Request("req-2"), nowMs: 11);
        pool.AcquireOrQueue(Request("req-3"), nowMs: 12);

        var secondLease = pool.Release(firstLease, nowMs: 20);
        var thirdLease = pool.Release(secondLease!, nowMs: 30);

        Assert.Equal("req-2", secondLease!.RequestId);
        Assert.Equal("req-3", thirdLease!.RequestId);
    }

    [Fact]
    public void Release_Throws_ForLeaseFromOtherPool()
    {
        var pool = CreatePool(capacity: 1);
        var foreignLease = new ResourceLease(
            new ResourceUnit("other-worker", ResourceType.Worker, "Other Worker"),
            "req-1",
            "task-1",
            10);

        Assert.Throws<DomainRuleViolationException>(() => pool.Release(foreignLease, nowMs: 20));
    }

    [Fact]
    public void Release_Throws_ForResourceThatIsNotBusy()
    {
        var pool = CreatePool(capacity: 1);
        var lease = new ResourceLease(Worker("worker-1"), "req-1", "task-1", 10);

        Assert.Throws<DomainRuleViolationException>(() => pool.Release(lease, nowMs: 20));
    }

    [Fact]
    public void Release_Throws_WhenNowIsBeforeAcquiredAt()
    {
        var pool = CreatePool(capacity: 1);
        var lease = pool.AcquireOrQueue(Request("req-1"), nowMs: 10)!;

        Assert.Throws<DomainRuleViolationException>(() => pool.Release(lease, nowMs: 9));
    }

    [Fact]
    public void Release_AccumulatesBusyTime()
    {
        var pool = CreatePool(capacity: 1);
        var lease = pool.AcquireOrQueue(Request("req-1"), nowMs: 10)!;

        pool.Release(lease, nowMs: 30);

        Assert.Equal(20, pool.Snapshot(nowMs: 30).TotalBusyTimeMs);
    }

    private static ResourcePool CreatePool(int capacity)
    {
        return new ResourcePool(
            "workers",
            ResourceType.Worker,
            Enumerable.Range(1, capacity).Select(index => Worker($"worker-{index}")));
    }

    private static ResourceUnit Worker(string resourceId)
    {
        return new ResourceUnit(resourceId, ResourceType.Worker, resourceId);
    }

    private static ResourceRequest Request(string requestId)
    {
        return new ResourceRequest(requestId, $"task-{requestId}", 0);
    }
}
