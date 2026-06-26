using Xunit;
using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Processes.Inbound;
using Sim.Core.Processes.Outbound;
using Sim.Core.Scenarios;
using Sim.Core.World;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseScenarioModelsTests
{
    [Fact]
    public void WarehouseScenario_AllowsSingleChildScenario()
    {
        var scenario = new WarehouseScenario(
            "warehouse-1",
            123,
            inboundScenario: InboundScenario(),
            outboundScenario: null,
            eachPickScenario: null);

        Assert.Equal("warehouse-1", scenario.ScenarioId);
        Assert.Equal(123, scenario.Seed);
        Assert.NotNull(scenario.InboundScenario);
        Assert.Null(scenario.OutboundScenario);
        Assert.Null(scenario.EachPickScenario);
    }

    [Fact]
    public void WarehouseScenario_StoresAllChildScenarios()
    {
        var scenario = new WarehouseScenario(
            "warehouse-all",
            456,
            InboundScenario(),
            OutboundScenario(),
            EachPickScenario());

        Assert.NotNull(scenario.InboundScenario);
        Assert.NotNull(scenario.OutboundScenario);
        Assert.NotNull(scenario.EachPickScenario);
    }

    [Fact]
    public void WarehouseScenario_Throws_ForEmptyScenarioId()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            new WarehouseScenario(
                "",
                123,
                InboundScenario(),
                null,
                null));
    }

    [Fact]
    public void WarehouseScenario_Throws_WhenNoChildScenarioExists()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            new WarehouseScenario(
                "warehouse-empty",
                123,
                null,
                null,
                null));
    }

    [Fact]
    public void WarehouseRunResult_AggregatesChildMetrics()
    {
        var result = new WarehouseRunResult(
            "warehouse-result",
            123,
            InboundResult(),
            OutboundResult(),
            EachPickResult(),
            startedAtMs: 10,
            finishedAtMs: 300,
            eventLogText: "warehouse-log",
            finalWorldState: new WorldState(300));

        Assert.Equal("warehouse-result", result.ScenarioId);
        Assert.Equal(123, result.Seed);
        Assert.Equal(1, result.CompletedReceipts);
        Assert.Equal(2, result.CompletedOutboundOrders);
        Assert.Equal(3, result.CompletedEachPickOrders);
        Assert.Equal(7m, result.TotalQuantityAvailable);
        Assert.Equal(8m, result.TotalQuantityShipped);
        Assert.Equal(9m, result.TotalQuantityPicked);
        Assert.Equal(10, result.StartedAtMs);
        Assert.Equal(300, result.FinishedAtMs);
        Assert.Equal("warehouse-log", result.EventLogText);
        Assert.Equal(300, result.FinalWorldState.TimeMs);
    }

    [Fact]
    public void WarehouseRunResult_UsesZeroMetricsForMissingChildResults()
    {
        var result = new WarehouseRunResult(
            "warehouse-inbound-only",
            123,
            InboundResult(),
            outboundResult: null,
            eachPickResult: null,
            startedAtMs: 10,
            finishedAtMs: 210,
            eventLogText: "",
            finalWorldState: new WorldState(210));

        Assert.Equal(1, result.CompletedReceipts);
        Assert.Equal(0, result.CompletedOutboundOrders);
        Assert.Equal(0, result.CompletedEachPickOrders);
        Assert.Equal(7m, result.TotalQuantityAvailable);
        Assert.Equal(0m, result.TotalQuantityShipped);
        Assert.Equal(0m, result.TotalQuantityPicked);
    }

    [Fact]
    public void WarehouseRunResult_Throws_ForEmptyScenarioId()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            new WarehouseRunResult(
                "",
                123,
                InboundResult(),
                null,
                null,
                10,
                210,
                "",
                new WorldState(210)));
    }

    [Fact]
    public void WarehouseRunResult_Throws_WhenNoChildResultExists()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            new WarehouseRunResult(
                "warehouse-empty-result",
                123,
                null,
                null,
                null,
                10,
                210,
                "",
                new WorldState(210)));
    }

    [Fact]
    public void WarehouseRunResult_Throws_ForNegativeStartedAtMs()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            new WarehouseRunResult(
                "warehouse-negative-start",
                123,
                InboundResult(),
                null,
                null,
                -1,
                210,
                "",
                new WorldState(210)));
    }

    [Fact]
    public void WarehouseRunResult_Throws_WhenFinishedBeforeStarted()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            new WarehouseRunResult(
                "warehouse-invalid-time",
                123,
                InboundResult(),
                null,
                null,
                210,
                10,
                "",
                new WorldState(210)));
    }

    private static InboundScenario InboundScenario()
    {
        return new InboundScenario(
            "inbound-1",
            123,
            [
                new InboundReceipt(
                    "receipt-1",
                    "warehouse-1",
                    "sku-inbound-1",
                    7m,
                    "stage-1",
                    "loc-1",
                    10)
            ],
            new InboundProcessParameters(100, 50, 50),
            dockCount: 1,
            forkliftCount: 1);
    }

    private static OutboundScenario OutboundScenario()
    {
        return new OutboundScenario(
            "outbound-1",
            123,
            [
                new OutboundOrder(
                    "order-1",
                    "warehouse-1",
                    "sku-order-1",
                    8m,
                    "pick-1",
                    "stage-1",
                    "dock-1",
                    20)
            ],
            [
                new OutboundInventoryItem(
                    "inv-outbound-1",
                    "sku-order-1",
                    8m,
                    "pick-1",
                    InventoryStatus.Available)
            ],
            new OutboundProcessParameters(50, 50, 25, 75),
            workerCount: 1,
            dockCount: 1);
    }

    private static EachPickScenario EachPickScenario()
    {
        return new EachPickScenario(
            "each-pick-1",
            123,
            [
                new EachPickOrder(
                    "each-order-1",
                    "warehouse-1",
                    "sku-each-1",
                    9m,
                    "pick-face-1",
                    "station-1",
                    "stage-1",
                    30)
            ],
            [
                new EachPickInventoryItem(
                    "inv-each-1",
                    "sku-each-1",
                    9m,
                    "pick-face-1",
                    InventoryStatus.Available)
            ],
            new EachPickProcessParameters(10, 20, 30, 40),
            stationCount: 1,
            workerCount: 1);
    }

    private static InboundRunResult InboundResult()
    {
        return new InboundRunResult(
            "inbound-result",
            123,
            1,
            7m,
            10,
            210,
            "inbound-log",
            new WorldState(210));
    }

    private static OutboundRunResult OutboundResult()
    {
        return new OutboundRunResult(
            "outbound-result",
            123,
            2,
            8m,
            20,
            220,
            "outbound-log",
            new WorldState(220));
    }

    private static EachPickRunResult EachPickResult()
    {
        return new EachPickRunResult(
            "each-pick-result",
            123,
            3,
            9m,
            30,
            300,
            "each-pick-log",
            new WorldState(300));
    }
}
