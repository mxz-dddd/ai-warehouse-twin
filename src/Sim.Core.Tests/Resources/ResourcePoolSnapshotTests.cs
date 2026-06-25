using Sim.Core.Resources;
using Xunit;

namespace Sim.Core.Tests.Resources;

public sealed class ResourcePoolSnapshotTests
{
    [Fact]
    public void Snapshot_ReturnsZeroUtilization_WhenNowMsIsZero()
    {
        var pool = CreatePool(capacity: 1);

        var snapshot = pool.Snapshot(nowMs: 0);

        Assert.Equal(0m, snapshot.Utilization);
        Assert.Equal(0, snapshot.TotalBusyTimeMs);
    }

    [Fact]
    public void Snapshot_ReturnsZeroUtilization_WhenNoBusyTimeExists()
    {
        var pool = CreatePool(capacity: 1);

        var snapshot = pool.Snapshot(nowMs: 100);

        Assert.Equal(0m, snapshot.Utilization);
        Assert.Equal(0, snapshot.TotalBusyTimeMs);
    }

    [Fact]
    public void Snapshot_IncludesCompletedBusyTime()
    {
        var pool = CreatePool(capacity: 2);
        var lease = pool.AcquireOrQueue(Request("req-1"), nowMs: 10)!;
        pool.Release(lease, nowMs: 30);

        var snapshot = pool.Snapshot(nowMs: 40);

        Assert.Equal(20, snapshot.TotalBusyTimeMs);
        Assert.Equal(0.25m, snapshot.Utilization);
    }

    [Fact]
    public void Snapshot_IncludesCurrentlyBusyResources()
    {
        var pool = CreatePool(capacity: 2);
        pool.AcquireOrQueue(Request("req-1"), nowMs: 10);

        var snapshot = pool.Snapshot(nowMs: 30);

        Assert.Equal(20, snapshot.TotalBusyTimeMs);
        Assert.Equal(20m / 60m, snapshot.Utilization);
    }

    [Fact]
    public void Snapshot_UtilizationDoesNotExceedOne()
    {
        var pool = CreatePool(capacity: 1);
        var lease = pool.AcquireOrQueue(Request("req-1"), nowMs: 0)!;

        pool.Release(lease, nowMs: 100);

        Assert.Equal(1m, pool.Snapshot(nowMs: 100).Utilization);
    }

    [Fact]
    public void Snapshot_ReturnsCounts()
    {
        var pool = CreatePool(capacity: 1);
        pool.AcquireOrQueue(Request("req-1"), nowMs: 0);
        pool.AcquireOrQueue(Request("req-2"), nowMs: 1);

        var snapshot = pool.Snapshot(nowMs: 10);

        Assert.Equal("workers", snapshot.PoolId);
        Assert.Equal(ResourceType.Worker, snapshot.ResourceType);
        Assert.Equal(1, snapshot.Capacity);
        Assert.Equal(0, snapshot.AvailableCount);
        Assert.Equal(1, snapshot.BusyCount);
        Assert.Equal(1, snapshot.WaitingCount);
    }

    private static ResourcePool CreatePool(int capacity)
    {
        return new ResourcePool(
            "workers",
            ResourceType.Worker,
            Enumerable.Range(1, capacity).Select(index => new ResourceUnit($"worker-{index}", ResourceType.Worker, $"Worker {index}")));
    }

    private static ResourceRequest Request(string requestId)
    {
        return new ResourceRequest(requestId, $"task-{requestId}", 0);
    }
}
