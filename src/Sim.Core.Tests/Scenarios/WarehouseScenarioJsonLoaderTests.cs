using Xunit;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Json;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseScenarioJsonLoaderTests
{
    [Fact]
    public void Load_ReadsSampleSmallWarehouseScenarioFromJson()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

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
    public void LoadedSampleSmallWarehouse_RunsToStableSummary()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

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
    public void LoadedSampleSmallWarehouse_ProducesExpectedEventLog()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        var result = new WarehouseScenarioRunner().Run(scenario);
        var lines = result.EventLogText.Split('\n');

        Assert.Equal(10, lines.Length);
        Assert.DoesNotContain('\r', result.EventLogText);
        Assert.Contains("inbound|0|10|inbound.receipt_arrived.receipt-1|InboundReceiptArrived", lines);
        Assert.Contains("outbound|0|20|outbound.order_released.order-1|OutboundOrderReleased", lines);
        Assert.Contains("each_pick|0|30|each_pick.order_released.each-order-1|EachPickOrderReleased", lines);
    }

    [Fact]
    public void FromJson_RejectsMissingRequiredChildScenarioId()
    {
        const string json = """
        {
          "scenario_id": "invalid",
          "seed": 1,
          "inbound": {
            "seed": 1,
            "dock_count": 1,
            "forklift_count": 1,
            "process": {
              "unload_duration_ms": 100,
              "qc_duration_ms": 50,
              "putaway_duration_ms": 50
            },
            "receipts": []
          }
        }
        """;

        var error = Assert.Throws<ArgumentException>(
            () => WarehouseScenarioJsonLoader.FromJson(json));

        Assert.Contains("inbound.scenario_id", error.Message);
    }

    private static string SampleScenarioPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "datasets",
                "sample-small-warehouse",
                "scenario.json");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Cannot find datasets/sample-small-warehouse/scenario.json from test output directory.");
    }
}
