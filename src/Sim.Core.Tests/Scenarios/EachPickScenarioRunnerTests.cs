using Xunit;
using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Scenarios;

namespace Sim.Core.Tests.Scenarios;

public sealed class EachPickScenarioRunnerTests
{
    [Fact]
    public void Run_CompletesSingleEachPickOrder()
    {
        var scenario = Scenario();
        var result = new EachPickScenarioRunner().Run(scenario);

        Assert.Equal("each-pick-scenario-1", result.ScenarioId);
        Assert.Equal(123, result.Seed);
        Assert.Equal(1, result.CompletedEachPickOrders);
        Assert.Equal(5m, result.TotalQuantityPicked);
        Assert.Equal(0, result.StartedAtMs);
        Assert.Equal(100, result.FinishedAtMs);
        Assert.Equal(100, result.FinalWorldState.TimeMs);

        Assert.Contains("each_pick.order_released.order-1", result.EventLogText);
        Assert.Contains("EachPickOrderReleased", result.EventLogText);
        Assert.Contains("each_pick.at_station.order-1", result.EventLogText);
        Assert.Contains("EachPickAtStation", result.EventLogText);
        Assert.Contains("each_pick.completed.order-1", result.EventLogText);
        Assert.Contains("EachPickCompleted", result.EventLogText);
        Assert.Contains("each_pick.staged.order-1", result.EventLogText);
        Assert.Contains("EachPickStaged", result.EventLogText);
    }


    [Fact]
    public void Run_RecordsExactSingleOrderEventLog()
    {
        var result = new EachPickScenarioRunner().Run(Scenario());

        var expected = string.Join(
            "\n",
            new[]
            {
                "0|0|each_pick.order_released.order-1|EachPickOrderReleased",
                "1|30|each_pick.at_station.order-1|EachPickAtStation",
                "2|60|each_pick.completed.order-1|EachPickCompleted",
                "3|100|each_pick.staged.order-1|EachPickStaged"
            });

        Assert.Equal(expected, result.EventLogText);
        Assert.DoesNotContain('\r', result.EventLogText);
    }

    [Fact]
    public void Run_ProducesDeterministicEventLog()
    {
        var scenario = Scenario();

        var first = new EachPickScenarioRunner().Run(scenario);
        var second = new EachPickScenarioRunner().Run(scenario);

        Assert.Equal(first.EventLogText, second.EventLogText);
        Assert.Equal(first.FinishedAtMs, second.FinishedAtMs);
        Assert.Equal(first.TotalQuantityPicked, second.TotalQuantityPicked);
    }

    [Fact]
    public void Run_CompletesMultipleOrders()
    {
        var scenario = new EachPickScenario(
            "each-pick-scenario-multi",
            456,
            [
                Order("order-2", "sku-2", 3m, "pick-face-2", "station-1", "stage-1", 10),
                Order("order-1", "sku-1", 5m, "pick-face-1", "station-1", "stage-1", 0)
            ],
            [
                Inventory("inv-2", "sku-2", 3m, "pick-face-2", InventoryStatus.Available),
                Inventory("inv-1", "sku-1", 5m, "pick-face-1", InventoryStatus.Available)
            ],
            Parameters(),
            stationCount: 1,
            workerCount: 1);

        var result = new EachPickScenarioRunner().Run(scenario);

        Assert.Equal(2, result.CompletedEachPickOrders);
        Assert.Equal(8m, result.TotalQuantityPicked);
        Assert.Equal(0, result.StartedAtMs);
        Assert.Equal(110, result.FinishedAtMs);

        Assert.Contains("each_pick.staged.order-1", result.EventLogText);
        Assert.Contains("each_pick.staged.order-2", result.EventLogText);
    }

    [Fact]
    public void Run_Throws_WhenNoAvailableInventoryCanBeAllocated()
    {
        var scenario = new EachPickScenario(
            "each-pick-scenario-no-inventory",
            123,
            [Order()],
            [Inventory(status: InventoryStatus.Allocated)],
            Parameters(),
            stationCount: 1,
            workerCount: 1);

        Assert.Throws<DomainRuleViolationException>(() =>
            new EachPickScenarioRunner().Run(scenario));
    }

    private static EachPickScenario Scenario()
    {
        return new EachPickScenario(
            "each-pick-scenario-1",
            123,
            [Order()],
            [Inventory()],
            Parameters(),
            stationCount: 1,
            workerCount: 1);
    }

    private static EachPickOrder Order(
        string orderId = "order-1",
        string skuId = "sku-1",
        decimal quantity = 5m,
        string sourceLocationId = "pick-face-1",
        string pickStationId = "station-1",
        string stagingLocationId = "stage-1",
        long releasedAtMs = 0)
    {
        return new EachPickOrder(
            orderId,
            "warehouse-1",
            skuId,
            quantity,
            sourceLocationId,
            pickStationId,
            stagingLocationId,
            releasedAtMs);
    }

    private static EachPickInventoryItem Inventory(
        string inventoryUnitId = "inv-1",
        string skuId = "sku-1",
        decimal quantity = 5m,
        string locationId = "pick-face-1",
        InventoryStatus status = InventoryStatus.Available)
    {
        return new EachPickInventoryItem(
            inventoryUnitId,
            skuId,
            quantity,
            locationId,
            status);
    }

    private static EachPickProcessParameters Parameters()
    {
        return new EachPickProcessParameters(
            toteBindDurationMs: 10,
            travelToStationDurationMs: 20,
            pickServiceDurationMs: 30,
            moveToStagingDurationMs: 40);
    }
}
