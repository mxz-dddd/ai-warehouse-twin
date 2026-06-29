using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Samples;
using Sim.Core.Scenarios.Unified;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseScenarioRunnerUnifiedTests
{
    [Fact]
    public void WarehouseScenarioRunner_UnifiedPath_UsesSharedResourceAndInventory()
    {
        var result = new WarehouseScenarioRunner().RunUnified(UnifiedScenario());

        Assert.Equal("test-unified-warehouse", result.ScenarioId);
        Assert.Equal(20240627, result.Seed);
        Assert.Equal(0, result.StartedAtMs);
        Assert.Equal(180, result.FinishedAtMs);
        Assert.Equal(180, result.FinalWorldState.TimeMs);
        Assert.Equal(1, result.CompletedReceipts);
        Assert.Equal(1, result.CompletedOutboundOrders);
        Assert.Equal(1, result.CompletedEachPickOrders);
        Assert.Equal(7m, result.TotalQuantityAvailable);
        Assert.Equal(8m, result.TotalQuantityShipped);
        Assert.Equal(4m, result.TotalQuantityPicked);
        Assert.Equal(0m, result.FinalInventorySnapshot["SKU-A"]);

        Assert.Contains(
            "0|resource.acquired|resource_id=dock-1|owner=inbound:inbound-1",
            result.EventLogText);
        Assert.Contains(
            "100|resource.acquired|resource_id=dock-1|owner=outbound:outbound-1",
            result.EventLogText);
        Assert.Contains(
            "150|resource.acquired|resource_id=dock-1|owner=each_pick:each-pick-1",
            result.EventLogText);
        Assert.Contains("100|inventory.added|sku_id=SKU-A", result.EventLogText);
        Assert.Contains("150|inventory.removed|sku_id=SKU-A", result.EventLogText);
        Assert.DoesNotContain('\r', result.EventLogText);
    }

    [Fact]
    public void WarehouseScenarioRunner_UnifiedPath_IsDeterministicAcrossRuns()
    {
        var runner = new WarehouseScenarioRunner();
        var scenario = UnifiedScenario();

        var first = runner.RunUnified(scenario);
        var second = runner.RunUnified(scenario);

        Assert.Equal(first.EventLogText, second.EventLogText);
        Assert.Equal(first.FinishedAtMs, second.FinishedAtMs);
        Assert.Equal(first.KpiSummary, second.KpiSummary);
        Assert.Equal(
            first.FinalInventorySnapshot.ToArray(),
            second.FinalInventorySnapshot.ToArray());
    }

    [Fact]
    public void WarehouseScenarioRunner_LegacySample_RemainsStable()
    {
        var result = new WarehouseScenarioRunner().Run(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse());

        Assert.Equal("sample-small-warehouse", result.ScenarioId);
        Assert.Equal(20240627, result.Seed);
        Assert.Equal(10, result.StartedAtMs);
        Assert.Equal(220, result.FinishedAtMs);
        Assert.Equal(1, result.CompletedReceipts);
        Assert.Equal(1, result.CompletedOutboundOrders);
        Assert.Equal(1, result.CompletedEachPickOrders);
        Assert.Equal(10, result.KpiSummary.EventLogLineCount);
        Assert.Empty(result.FinalInventorySnapshot);
    }

    [Fact]
    public void WarehouseScenarioRunner_RunWithUnifiedAdapter_RunsSampleScenario()
    {
        var result = new WarehouseScenarioRunner().RunWithUnifiedAdapter(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse());

        Assert.Equal("sample-small-warehouse", result.ScenarioId);
        Assert.Equal(20240627, result.Seed);
        Assert.Equal(10, result.StartedAtMs);
        Assert.Equal(410, result.FinishedAtMs);
        Assert.Equal(1, result.CompletedReceipts);
        Assert.Equal(1, result.CompletedOutboundOrders);
        Assert.Equal(1, result.CompletedEachPickOrders);
        Assert.DoesNotContain('\r', result.EventLogText);
    }

    [Fact]
    public void WarehouseScenarioRunner_RunWithUnifiedAdapter_MatchesLegacyCoreCountsAndQuantities()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var runner = new WarehouseScenarioRunner();

        var legacy = runner.Run(scenario);
        var unified = runner.RunWithUnifiedAdapter(scenario);

        Assert.Equal(legacy.CompletedReceipts, unified.CompletedReceipts);
        Assert.Equal(legacy.CompletedOutboundOrders, unified.CompletedOutboundOrders);
        Assert.Equal(legacy.CompletedEachPickOrders, unified.CompletedEachPickOrders);
        Assert.Equal(legacy.TotalQuantityAvailable, unified.TotalQuantityAvailable);
        Assert.Equal(legacy.TotalQuantityShipped, unified.TotalQuantityShipped);
        Assert.Equal(legacy.TotalQuantityPicked, unified.TotalQuantityPicked);
    }

    [Fact]
    public void WarehouseScenarioRunner_RunWithUnifiedAdapter_ProducesFinalInventorySnapshot()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var runner = new WarehouseScenarioRunner();

        var legacy = runner.Run(scenario);
        var unified = runner.RunWithUnifiedAdapter(scenario);

        Assert.Empty(legacy.FinalInventorySnapshot);
        Assert.Equal(3, unified.FinalInventorySnapshot.Count);
        Assert.Equal(7m, unified.FinalInventorySnapshot["sku-inbound-1"]);
        Assert.Equal(0m, unified.FinalInventorySnapshot["sku-outbound-1"]);
        Assert.Equal(0m, unified.FinalInventorySnapshot["sku-each-1"]);
    }

    [Fact]
    public void WarehouseScenarioRunner_Run_DefaultPath_RemainsLegacyAfterUnifiedAdapterPathAdded()
    {
        var result = new WarehouseScenarioRunner().Run(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse());

        Assert.Equal(220, result.FinishedAtMs);
        Assert.Equal(10, result.KpiSummary.EventLogLineCount);
        Assert.Contains("inbound|", result.EventLogText);
        Assert.Contains("outbound|", result.EventLogText);
        Assert.Contains("each_pick|", result.EventLogText);
        Assert.Empty(result.FinalInventorySnapshot);
    }

    [Fact]
    public void WarehouseScenarioRunner_RunWithTrace_RemainsLegacyTracePathAfterUnifiedAdapterPathAdded()
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
    public void WarehouseScenarioRunner_RunWithUnifiedAdapter_DocumentsCoarseOperationMappingGap()
    {
        var result = new WarehouseScenarioRunner().RunWithUnifiedAdapter(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse());

        Assert.Equal(13, result.KpiSummary.EventLogLineCount);
        Assert.Contains("owner=inbound:inbound:receipt-1", result.EventLogText);
        Assert.Contains("owner=outbound:outbound:order-1", result.EventLogText);
        Assert.Contains("owner=each_pick:each_pick:each-order-1", result.EventLogText);
        Assert.DoesNotContain("forklift", result.EventLogText);
        Assert.DoesNotContain("worker", result.EventLogText);
    }

    private static WarehouseUnifiedScenario UnifiedScenario()
    {
        return new WarehouseUnifiedScenario(
            "test-unified-warehouse",
            seed: 20240627,
            new Dictionary<string, decimal>
            {
                ["SKU-A"] = 5m
            },
            [
                Operation(
                    "inbound-1",
                    WarehouseUnifiedOperationType.Inbound,
                    requestedAtMs: 0,
                    durationMs: 100,
                    inventoryDelta: 7m),
                Operation(
                    "outbound-1",
                    WarehouseUnifiedOperationType.Outbound,
                    requestedAtMs: 100,
                    durationMs: 50,
                    inventoryDelta: -8m),
                Operation(
                    "each-pick-1",
                    WarehouseUnifiedOperationType.EachPick,
                    requestedAtMs: 150,
                    durationMs: 30,
                    inventoryDelta: -4m)
            ]);
    }

    private static WarehouseUnifiedOperation Operation(
        string operationId,
        WarehouseUnifiedOperationType operationType,
        long requestedAtMs,
        long durationMs,
        decimal inventoryDelta)
    {
        return new WarehouseUnifiedOperation(
            operationId,
            operationType,
            requestedAtMs,
            resourceId: "dock-1",
            durationMs,
            skuId: "SKU-A",
            inventoryDelta);
    }

    private static void AssertStages(
        IEnumerable<Sim.Core.Resources.ResourceLeaseTimelineEntry> timeline,
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
