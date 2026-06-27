using Xunit;
using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Processes.Inbound;
using Sim.Core.Processes.Outbound;
using Sim.Core.Scenarios;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseScenarioRunnerTests
{
    [Fact]
    public void Run_CombinesAllChildScenarioResults()
    {
        var scenario = Scenario(
            inboundScenario: InboundScenario(),
            outboundScenario: OutboundScenario(),
            eachPickScenario: EachPickScenario());

        var result = new WarehouseScenarioRunner().Run(scenario);

        Assert.Equal("warehouse-1", result.ScenarioId);
        Assert.Equal(999, result.Seed);

        Assert.Equal(1, result.CompletedReceipts);
        Assert.Equal(1, result.CompletedOutboundOrders);
        Assert.Equal(1, result.CompletedEachPickOrders);

        Assert.Equal(7m, result.TotalQuantityAvailable);
        Assert.Equal(8m, result.TotalQuantityShipped);
        Assert.Equal(9m, result.TotalQuantityPicked);

        Assert.Equal(10, result.StartedAtMs);
        Assert.Equal(220, result.FinishedAtMs);
        Assert.Equal(220, result.FinalWorldState.TimeMs);
    }

    [Fact]
    public void Run_AllowsInboundOnlyWarehouseScenario()
    {
        var scenario = Scenario(
            inboundScenario: InboundScenario(),
            outboundScenario: null,
            eachPickScenario: null);

        var result = new WarehouseScenarioRunner().Run(scenario);

        Assert.Equal(1, result.CompletedReceipts);
        Assert.Equal(0, result.CompletedOutboundOrders);
        Assert.Equal(0, result.CompletedEachPickOrders);

        Assert.Equal(7m, result.TotalQuantityAvailable);
        Assert.Equal(0m, result.TotalQuantityShipped);
        Assert.Equal(0m, result.TotalQuantityPicked);

        Assert.NotNull(result.InboundResult);
        Assert.Null(result.OutboundResult);
        Assert.Null(result.EachPickResult);
    }

    [Fact]
    public void Run_PrefixesChildEventLogLines()
    {
        var scenario = Scenario(
            inboundScenario: InboundScenario(),
            outboundScenario: OutboundScenario(),
            eachPickScenario: EachPickScenario());

        var result = new WarehouseScenarioRunner().Run(scenario);

        Assert.Contains("inbound|", result.EventLogText);
        Assert.Contains("outbound|", result.EventLogText);
        Assert.Contains("each_pick|", result.EventLogText);

        Assert.Contains("inbound|0|10|inbound.receipt_arrived.receipt-1|InboundReceiptArrived", result.EventLogText);
        Assert.Contains("outbound|0|20|outbound.order_released.order-1|OutboundOrderReleased", result.EventLogText);
        Assert.Contains("each_pick|0|30|each_pick.order_released.each-order-1|EachPickOrderReleased", result.EventLogText);
        Assert.DoesNotContain('\r', result.EventLogText);
    }

    [Fact]
    public void Run_ProducesDeterministicWarehouseEventLog()
    {
        var scenario = Scenario(
            inboundScenario: InboundScenario(),
            outboundScenario: OutboundScenario(),
            eachPickScenario: EachPickScenario());

        var first = new WarehouseScenarioRunner().Run(scenario);
        var second = new WarehouseScenarioRunner().Run(scenario);

        Assert.Equal(first.EventLogText, second.EventLogText);
        Assert.Equal(first.StartedAtMs, second.StartedAtMs);
        Assert.Equal(first.FinishedAtMs, second.FinishedAtMs);
    }

    [Fact]
    public void Run_MergesChildWorldStateEntities()
    {
        var scenario = Scenario(
            inboundScenario: InboundScenario(),
            outboundScenario: OutboundScenario(),
            eachPickScenario: EachPickScenario());

        var result = new WarehouseScenarioRunner().Run(scenario);

        Assert.Equal("AVAILABLE", result.FinalWorldState.Entities["receipt:receipt-1"].Status);
        Assert.Equal("SHIPPED", result.FinalWorldState.Entities["order:order-1"].Status);
    }

    private static WarehouseScenario Scenario(
        InboundScenario? inboundScenario,
        OutboundScenario? outboundScenario,
        EachPickScenario? eachPickScenario)
    {
        return new WarehouseScenario(
            "warehouse-1",
            999,
            inboundScenario,
            outboundScenario,
            eachPickScenario);
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
            456,
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
            789,
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
}
