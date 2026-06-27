using Sim.Core.Domain;
using Sim.Core.Scenarios.Unified;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseUnifiedOperationRunnerTests
{
    [Fact]
    public void CombinedRunner_ResourceCapacityOne_DoesNotDoubleBook()
    {
        var result = new WarehouseUnifiedOperationRunner().Run(
            InitialInventory(10m),
            [
                Operation(
                    "inbound-a",
                    WarehouseUnifiedOperationType.Inbound,
                    requestedAtMs: 0,
                    durationMs: 100,
                    inventoryDelta: 5m),
                Operation(
                    "outbound-b",
                    WarehouseUnifiedOperationType.Outbound,
                    requestedAtMs: 0,
                    durationMs: 50,
                    inventoryDelta: -4m)
            ]);

        Assert.Equal(2, result.OperationIntervals.Count);
        Assert.True(
            result.OperationIntervals[0].FinishedAtMs <=
            result.OperationIntervals[1].StartedAtMs);
        Assert.Equal(11m, result.FinalInventorySnapshot["SKU-A"]);
    }

    [Fact]
    public void CombinedRunner_InventoryCannotGoNegative()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new WarehouseUnifiedOperationRunner().Run(
                InitialInventory(2m),
                [
                    Operation(
                        "outbound-a",
                        WarehouseUnifiedOperationType.Outbound,
                        requestedAtMs: 0,
                        durationMs: 50,
                        inventoryDelta: -3m)
                ]));
    }

    [Fact]
    public void CombinedRunner_InboundOutboundEachPick_ConservesInventory()
    {
        var result = RunConservationScenario();

        Assert.Equal(0m, result.FinalInventorySnapshot["SKU-A"]);
        Assert.Equal(
            5m + 7m - 8m - 4m,
            result.FinalInventorySnapshot["SKU-A"]);
        Assert.All(
            result.FinalInventorySnapshot.Values,
            quantity => Assert.True(quantity >= 0m));
    }

    [Fact]
    public void CombinedRunner_IsDeterministicAcrossRuns()
    {
        var first = RunConservationScenario();
        var second = RunConservationScenario();

        Assert.Equal(
            first.OperationIntervals.ToArray(),
            second.OperationIntervals.ToArray());
        Assert.Equal(
            first.FinalInventorySnapshot.ToArray(),
            second.FinalInventorySnapshot.ToArray());
        Assert.Equal(first.EventLogText, second.EventLogText);
    }

    [Fact]
    public void CombinedRunner_EventLog_UsesLfOnly()
    {
        var result = RunConservationScenario();

        Assert.Contains('\n', result.EventLogText);
        Assert.DoesNotContain('\r', result.EventLogText);
    }

    private static WarehouseUnifiedOperationResult RunConservationScenario()
    {
        return new WarehouseUnifiedOperationRunner().Run(
            InitialInventory(5m),
            [
                Operation(
                    "inbound-a",
                    WarehouseUnifiedOperationType.Inbound,
                    requestedAtMs: 0,
                    durationMs: 10,
                    inventoryDelta: 7m),
                Operation(
                    "outbound-b",
                    WarehouseUnifiedOperationType.Outbound,
                    requestedAtMs: 10,
                    durationMs: 10,
                    inventoryDelta: -8m),
                Operation(
                    "each-pick-c",
                    WarehouseUnifiedOperationType.EachPick,
                    requestedAtMs: 20,
                    durationMs: 10,
                    inventoryDelta: -4m)
            ]);
    }

    private static IReadOnlyDictionary<string, decimal> InitialInventory(
        decimal quantity)
    {
        return new Dictionary<string, decimal>
        {
            ["SKU-A"] = quantity
        };
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
