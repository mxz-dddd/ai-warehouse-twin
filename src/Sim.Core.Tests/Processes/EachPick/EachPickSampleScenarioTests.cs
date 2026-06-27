using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Json;
using Xunit;

namespace Sim.Core.Tests.Processes.EachPick;

public sealed class EachPickSampleScenarioTests
{
    [Fact]
    public void SampleEachPickScenario_LoadsAndCompletes()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        Assert.Equal("sample-each-pick", scenario.ScenarioId);
        Assert.Equal(20240628, scenario.Seed);
        Assert.Null(scenario.InboundScenario);
        Assert.Null(scenario.OutboundScenario);
        Assert.NotNull(scenario.EachPickScenario);

        var result = new WarehouseScenarioRunner().Run(scenario);

        Assert.Equal(1, result.CompletedEachPickOrders);
        Assert.Equal(5m, result.TotalQuantityPicked);
        Assert.Equal(0, result.StartedAtMs);
        Assert.Equal(100, result.FinishedAtMs);
        Assert.Equal(100, result.FinalWorldState.TimeMs);
        Assert.Equal(100, result.KpiSummary.TotalDurationMs);
        Assert.Equal(1, result.KpiSummary.TotalCompletedWorkItems);
        Assert.Equal(4, result.KpiSummary.EventLogLineCount);
        Assert.InRange(
            result.KpiSummary.EachPickOrderThroughputPerHour,
            35999.999m,
            36000.001m);
        Assert.InRange(
            result.KpiSummary.TotalWorkItemThroughputPerHour,
            35999.999m,
            36000.001m);

        var eventLines = result.EventLogText.Split('\n');
        Assert.Equal(4, eventLines.Length);
        Assert.Single(
            eventLines,
            line => line.EndsWith(
                "|EachPickOrderReleased",
                StringComparison.Ordinal));
        Assert.Contains(
            "each_pick|0|0|each_pick.order_released.each-order-1|EachPickOrderReleased",
            eventLines);
        Assert.Contains(
            "each_pick|1|30|each_pick.at_station.each-order-1|EachPickAtStation",
            eventLines);
        Assert.Contains(
            "each_pick|2|60|each_pick.completed.each-order-1|EachPickCompleted",
            eventLines);
        Assert.Contains(
            "each_pick|3|100|each_pick.staged.each-order-1|EachPickStaged",
            eventLines);
    }

    [Fact]
    public void SampleEachPickScenario_IsDeterministic()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());
        var runner = new WarehouseScenarioRunner();

        var first = runner.Run(scenario);
        var second = runner.Run(scenario);

        Assert.Equal(first.EventLogText, second.EventLogText);
        Assert.Equal(first.FinishedAtMs, second.FinishedAtMs);
        Assert.Equal(first.KpiSummary, second.KpiSummary);
    }

    private static string SampleScenarioPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "datasets",
                "sample-each-pick",
                "scenario.json");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Cannot find datasets/sample-each-pick/scenario.json from test output directory.");
    }
}
