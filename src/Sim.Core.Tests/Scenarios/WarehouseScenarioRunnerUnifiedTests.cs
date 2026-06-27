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
}
