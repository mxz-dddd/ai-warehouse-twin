using Sim.Core.Scenarios;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseSharedResourceTests
{
    [Fact]
    public void SharedResource_CapacityOne_DoesNotDoubleBook()
    {
        var result = RunContentionScenario();

        Assert.Equal(2, result.Allocations.Count);
        Assert.True(
            result.Allocations[0].FinishedAtMs <= result.Allocations[1].StartedAtMs);
    }

    [Fact]
    public void SharedResource_CapacityOne_SecondWorkItemStartsAfterFirstFinishes()
    {
        var result = RunContentionScenario();

        Assert.Equal(
            new WarehouseSharedResourceAllocation(
                "dock-1",
                "inbound:receipt-a",
                StartedAtMs: 0,
                FinishedAtMs: 100),
            result.Allocations[0]);

        Assert.Equal(
            new WarehouseSharedResourceAllocation(
                "dock-1",
                "outbound:order-b",
                StartedAtMs: 100,
                FinishedAtMs: 150),
            result.Allocations[1]);

        Assert.Contains(
            "0|resource.queued|resource_id=dock-1|owner=outbound:order-b",
            result.EventLogText);
        Assert.Contains(
            "100|resource.acquired|resource_id=dock-1|owner=outbound:order-b",
            result.EventLogText);
    }

    [Fact]
    public void WarehouseTimeline_IsDeterministicAcrossRuns()
    {
        var first = RunContentionScenario();
        var second = RunContentionScenario();

        Assert.Equal(first.Allocations.ToArray(), second.Allocations.ToArray());
        Assert.Equal(first.EventLogText, second.EventLogText);
    }

    [Fact]
    public void EventLogText_UsesLfOnly()
    {
        var result = RunContentionScenario();

        Assert.Contains('\n', result.EventLogText);
        Assert.DoesNotContain('\r', result.EventLogText);
    }

    private static WarehouseSharedResourceTimelineResult RunContentionScenario()
    {
        return new WarehouseSharedResourceTimelineRunner().RunCapacityOne(
            "dock-1",
            [
                new WarehouseSharedResourceWorkItem(
                    "inbound",
                    "receipt-a",
                    "dock-1",
                    requestedAtMs: 0,
                    serviceDurationMs: 100,
                    sequence: 0),
                new WarehouseSharedResourceWorkItem(
                    "outbound",
                    "order-b",
                    "dock-1",
                    requestedAtMs: 0,
                    serviceDurationMs: 50,
                    sequence: 1)
            ]);
    }
}
