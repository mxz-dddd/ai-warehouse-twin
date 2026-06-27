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
        Assert.Equal(
            first.OperationTelemetry.ToArray(),
            second.OperationTelemetry.ToArray());
        Assert.Equal(first.EventLogText, second.EventLogText);
    }

    [Fact]
    public void CombinedRunner_OperationTelemetry_CapturesCustomerKpiInputs()
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

        var inbound = result.OperationTelemetry
            .Single(telemetry => telemetry.OperationId == "inbound-a");
        var outbound = result.OperationTelemetry
            .Single(telemetry => telemetry.OperationId == "outbound-b");

        Assert.Equal(WarehouseUnifiedOperationType.Inbound, inbound.OperationType);
        Assert.Equal("dock-1", inbound.ResourceId);
        Assert.Equal(0, inbound.RequestedAtMs);
        Assert.Equal(0, inbound.StartedAtMs);
        Assert.Equal(100, inbound.FinishedAtMs);
        Assert.Equal(0, inbound.WaitingTimeMs);
        Assert.Equal(100, inbound.DurationMs);
        Assert.Equal("SKU-A", inbound.SkuId);
        Assert.Equal(5m, inbound.InventoryDelta);

        Assert.Equal(WarehouseUnifiedOperationType.Outbound, outbound.OperationType);
        Assert.Equal("dock-1", outbound.ResourceId);
        Assert.Equal(0, outbound.RequestedAtMs);
        Assert.Equal(100, outbound.StartedAtMs);
        Assert.Equal(150, outbound.FinishedAtMs);
        Assert.Equal(100, outbound.WaitingTimeMs);
        Assert.Equal(50, outbound.DurationMs);
        Assert.Equal("SKU-A", outbound.SkuId);
        Assert.Equal(-4m, outbound.InventoryDelta);
    }

    [Fact]
    public void CombinedRunner_CustomerKpiSummary_ComputesWaitingAndCycleStats()
    {
        var result = new WarehouseUnifiedOperationRunner().Run(
            InitialInventory(100m),
            [
                Operation(
                    "outbound-a",
                    WarehouseUnifiedOperationType.Outbound,
                    requestedAtMs: 0,
                    durationMs: 10,
                    inventoryDelta: -1m),
                Operation(
                    "outbound-b",
                    WarehouseUnifiedOperationType.Outbound,
                    requestedAtMs: 0,
                    durationMs: 20,
                    inventoryDelta: -1m),
                Operation(
                    "each-pick-c",
                    WarehouseUnifiedOperationType.EachPick,
                    requestedAtMs: 0,
                    durationMs: 30,
                    inventoryDelta: -1m),
                Operation(
                    "outbound-d",
                    WarehouseUnifiedOperationType.Outbound,
                    requestedAtMs: 0,
                    durationMs: 40,
                    inventoryDelta: -1m)
            ]);

        var summary = result.CustomerKpiSummary;

        Assert.Equal(4, summary.OperationCount);

        Assert.Equal(130, summary.TotalWaitingTimeMs);
        Assert.Equal(32.5m, summary.AverageWaitingTimeMs);
        Assert.Equal(60, summary.MaxWaitingTimeMs);
        Assert.Equal(30, summary.P50WaitingTimeMs);
        Assert.Equal(60, summary.P90WaitingTimeMs);
        Assert.Equal(60, summary.P95WaitingTimeMs);

        Assert.Equal(230, summary.TotalCycleTimeMs);
        Assert.Equal(57.5m, summary.AverageCycleTimeMs);
        Assert.Equal(100, summary.MaxCycleTimeMs);
        Assert.Equal(40, summary.P50CycleTimeMs);
        Assert.Equal(100, summary.P90CycleTimeMs);
        Assert.Equal(100, summary.P95CycleTimeMs);

        Assert.Equal(100, summary.TotalServiceDurationMs);
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
