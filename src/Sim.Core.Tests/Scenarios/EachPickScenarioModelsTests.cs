using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Scenarios;
using Sim.Core.World;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class EachPickScenarioModelsTests
{
    [Fact]
    public void EachPickScenario_CreatesScenario()
    {
        var scenario = Scenario();

        Assert.Equal("scenario-1", scenario.ScenarioId);
        Assert.Equal(123, scenario.Seed);
        Assert.Single(scenario.Orders);
        Assert.Single(scenario.InitialInventory);
        Assert.Equal(1, scenario.StationCount);
        Assert.Equal(1, scenario.WorkerCount);
    }

    [Fact]
    public void EachPickScenario_Throws_ForEmptyScenarioId()
    {
        Assert.Throws<DomainRuleViolationException>(() => Scenario(scenarioId: ""));
    }

    [Fact]
    public void EachPickScenario_Throws_ForEmptyOrders()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new EachPickScenario("scenario-1", 123, [], [Inventory()], Parameters(), 1, 1));
    }

    [Fact]
    public void EachPickScenario_Throws_ForEmptyInitialInventory()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new EachPickScenario("scenario-1", 123, [Order()], [], Parameters(), 1, 1));
    }

    [Fact]
    public void EachPickScenario_Throws_ForNullParameters()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EachPickScenario("scenario-1", 123, [Order()], [Inventory()], null!, 1, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EachPickScenario_Throws_ForInvalidStationCount(int stationCount)
    {
        Assert.Throws<DomainRuleViolationException>(() => Scenario(stationCount: stationCount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EachPickScenario_Throws_ForInvalidWorkerCount(int workerCount)
    {
        Assert.Throws<DomainRuleViolationException>(() => Scenario(workerCount: workerCount));
    }

    [Fact]
    public void EachPickRunResult_CreatesResult()
    {
        var result = Result();

        Assert.Equal("scenario-1", result.ScenarioId);
        Assert.Equal(123, result.Seed);
        Assert.Equal(1, result.CompletedEachPickOrders);
        Assert.Equal(5m, result.TotalQuantityPicked);
    }

    [Fact]
    public void EachPickRunResult_Throws_ForNegativeCompletedEachPickOrders()
    {
        Assert.Throws<DomainRuleViolationException>(() => Result(completedEachPickOrders: -1));
    }

    [Fact]
    public void EachPickRunResult_Throws_ForNegativeTotalQuantityPicked()
    {
        Assert.Throws<DomainRuleViolationException>(() => Result(totalQuantityPicked: -1m));
    }

    [Fact]
    public void EachPickRunResult_Throws_WhenFinishedBeforeStarted()
    {
        Assert.Throws<DomainRuleViolationException>(() => Result(startedAtMs: 20, finishedAtMs: 10));
    }

    [Fact]
    public void EachPickRunResult_Throws_ForNullFinalWorldState()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new EachPickRunResult(
                "scenario-1",
                123,
                1,
                5m,
                0,
                100,
                string.Empty,
                null!));
    }

    private static EachPickScenario Scenario(
        string scenarioId = "scenario-1",
        int stationCount = 1,
        int workerCount = 1)
    {
        return new EachPickScenario(
            scenarioId,
            123,
            [Order()],
            [Inventory()],
            Parameters(),
            stationCount,
            workerCount);
    }

    private static EachPickRunResult Result(
        int completedEachPickOrders = 1,
        decimal totalQuantityPicked = 5m,
        long startedAtMs = 10,
        long finishedAtMs = 20,
        WorldState? finalWorldState = null)
    {
        return new EachPickRunResult(
            "scenario-1",
            123,
            completedEachPickOrders,
            totalQuantityPicked,
            startedAtMs,
            finishedAtMs,
            "",
            finalWorldState ?? new WorldState(20));
    }

    private static EachPickOrder Order()
    {
        return new EachPickOrder(
            "order-1",
            "warehouse-1",
            "sku-1",
            5m,
            "pick-face-1",
            "station-1",
            "stage-1",
            0);
    }

    private static EachPickInventoryItem Inventory()
    {
        return new EachPickInventoryItem(
            "inv-1",
            "sku-1",
            5m,
            "pick-face-1",
            InventoryStatus.Available);
    }

    private static EachPickProcessParameters Parameters()
    {
        return new EachPickProcessParameters(10, 20, 30, 40);
    }
}
