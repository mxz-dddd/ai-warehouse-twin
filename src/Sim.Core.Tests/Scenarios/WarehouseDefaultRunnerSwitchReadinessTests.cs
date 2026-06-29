using Sim.Core.Resources;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Samples;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseDefaultRunnerSwitchReadinessTests
{
    [Fact]
    public void WarehouseDefaultRunnerSwitchReadiness_DefaultRunStillUsesLegacyPath()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var runner = new WarehouseScenarioRunner();

        var legacy = runner.Run(scenario);
        var unified = runner.RunWithUnifiedAdapter(scenario);

        Assert.Equal(220, legacy.FinishedAtMs);
        Assert.Equal(410, unified.FinishedAtMs);
        Assert.Equal(10, legacy.KpiSummary.EventLogLineCount);
        Assert.Equal(13, unified.KpiSummary.EventLogLineCount);
        Assert.Empty(legacy.FinalInventorySnapshot);
        Assert.NotEmpty(unified.FinalInventorySnapshot);
        Assert.Contains("inbound|", legacy.EventLogText);
        Assert.DoesNotContain("resource.acquired", legacy.EventLogText);
    }

    [Fact]
    public void WarehouseDefaultRunnerSwitchReadiness_RunWithTraceStillUsesLegacyPath()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var runner = new WarehouseScenarioRunner();

        var legacy = runner.Run(scenario);
        var traced = runner.RunWithTrace(scenario);

        Assert.Equal(legacy.EventLogText, traced.RunResult.EventLogText);
        Assert.Equal(legacy.KpiSummary, traced.RunResult.KpiSummary);
        Assert.Empty(traced.RunResult.FinalInventorySnapshot);
        AssertStages(traced.ResourceLeaseTimeline, "inbound", "dock", "forklift");
        AssertStages(traced.ResourceLeaseTimeline, "outbound", "dock", "worker");
        AssertStages(traced.ResourceLeaseTimeline, "each_pick", "station", "worker");
    }

    [Fact]
    public void WarehouseDefaultRunnerSwitchReadiness_ExplicitUnifiedPathRunsSample()
    {
        var result = new WarehouseScenarioRunner().RunWithUnifiedAdapter(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse());

        Assert.Equal("sample-small-warehouse", result.ScenarioId);
        Assert.Equal(20240627, result.Seed);
        Assert.Equal(1, result.CompletedReceipts);
        Assert.Equal(1, result.CompletedOutboundOrders);
        Assert.Equal(1, result.CompletedEachPickOrders);
        Assert.Equal(7m, result.TotalQuantityAvailable);
        Assert.Equal(8m, result.TotalQuantityShipped);
        Assert.Equal(4m, result.TotalQuantityPicked);
        Assert.Equal(3, result.FinalInventorySnapshot.Count);
        Assert.Contains("resource.acquired", result.EventLogText);
    }

    [Fact]
    public void WarehouseDefaultRunnerSwitchReadiness_LegacyAndUnifiedComparableFieldsRemainAligned()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var runner = new WarehouseScenarioRunner();

        var legacy = runner.Run(scenario);
        var unified = runner.RunWithUnifiedAdapter(scenario);

        Assert.Equal(legacy.ScenarioId, unified.ScenarioId);
        Assert.Equal(legacy.Seed, unified.Seed);
        Assert.Equal(legacy.StartedAtMs, unified.StartedAtMs);
        Assert.Equal(legacy.CompletedReceipts, unified.CompletedReceipts);
        Assert.Equal(legacy.CompletedOutboundOrders, unified.CompletedOutboundOrders);
        Assert.Equal(legacy.CompletedEachPickOrders, unified.CompletedEachPickOrders);
        Assert.Equal(legacy.TotalQuantityAvailable, unified.TotalQuantityAvailable);
        Assert.Equal(legacy.TotalQuantityShipped, unified.TotalQuantityShipped);
        Assert.Equal(legacy.TotalQuantityPicked, unified.TotalQuantityPicked);
    }

    private static void AssertStages(
        IEnumerable<ResourceLeaseTimelineEntry> timeline,
        string operationType,
        params string[] expectedStages)
    {
        Assert.Equal(
            expectedStages.Order(StringComparer.Ordinal),
            timeline
                .Where(entry => entry.OperationType == operationType)
                .Select(entry => entry.StageType)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
    }
}
