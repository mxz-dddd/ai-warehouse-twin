using Xunit;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Samples;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseSampleScenarioFactoryTests
{
    [Fact]
    public void CreateSmallWarehouse_ReturnsCompleteWarehouseScenario()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();

        Assert.Equal("sample-small-warehouse", scenario.ScenarioId);
        Assert.Equal(20240627, scenario.Seed);

        Assert.NotNull(scenario.InboundScenario);
        Assert.NotNull(scenario.OutboundScenario);
        Assert.NotNull(scenario.EachPickScenario);

        Assert.Equal("sample-small-warehouse.inbound", scenario.InboundScenario!.ScenarioId);
        Assert.Equal("sample-small-warehouse.outbound", scenario.OutboundScenario!.ScenarioId);
        Assert.Equal("sample-small-warehouse.each-pick", scenario.EachPickScenario!.ScenarioId);
    }

    [Fact]
    public void SampleSmallWarehouse_RunsToStableSummary()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();

        var result = new WarehouseScenarioRunner().Run(scenario);

        Assert.Equal("sample-small-warehouse", result.ScenarioId);
        Assert.Equal(20240627, result.Seed);

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
    public void SampleSmallWarehouse_ProducesDeterministicEventLog()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();

        var first = new WarehouseScenarioRunner().Run(scenario);
        var second = new WarehouseScenarioRunner().Run(scenario);

        Assert.Equal(first.EventLogText, second.EventLogText);

        Assert.Contains("inbound|0|10|inbound.receipt_arrived.receipt-1|InboundReceiptArrived", first.EventLogText);
        Assert.Contains("outbound|0|20|outbound.order_released.order-1|OutboundOrderReleased", first.EventLogText);
        Assert.Contains("each_pick|0|30|each_pick.order_released.each-order-1|EachPickOrderReleased", first.EventLogText);
    }
}
