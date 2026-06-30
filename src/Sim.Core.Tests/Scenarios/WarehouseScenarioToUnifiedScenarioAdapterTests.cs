using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Samples;
using Sim.Core.Scenarios.Unified;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseScenarioToUnifiedScenarioAdapterTests
{
    [Fact]
    public void WarehouseScenarioToUnifiedScenarioAdapter_ConvertsSampleScenario()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();

        var unified = WarehouseScenarioToUnifiedScenarioAdapter.Convert(scenario);

        Assert.Equal("sample-small-warehouse", unified.ScenarioId);
        Assert.Equal(20240627, unified.Seed);
        Assert.Equal(
            ["sku-each-1", "sku-outbound-1"],
            unified.InitialInventory.Keys);
        Assert.Equal(9m, unified.InitialInventory["sku-each-1"]);
        Assert.Equal(8m, unified.InitialInventory["sku-outbound-1"]);

        Assert.Equal(
            ["inbound:receipt-1", "outbound:order-1", "each_pick:each-order-1"],
            unified.Operations.Select(operation => operation.OperationId));
    }

    [Fact]
    public void WarehouseScenarioToUnifiedScenarioAdapter_MapsStableResourceIds()
    {
        var unified = WarehouseScenarioToUnifiedScenarioAdapter.Convert(
            WarehouseSampleScenarioFactory.CreateSmallWarehouse());

        Assert.Equal(
            [
                "inbound:receipt-1:dock-1",
                "outbound:order-1:dock-1",
                "each_pick:each-order-1:station-1"
            ],
            unified.Operations.Select(operation =>
                $"{operation.OperationId}:{operation.ResourceId}"));
    }

    [Fact]
    public void WarehouseScenarioToUnifiedScenarioAdapter_IsDeterministic()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();

        var first = WarehouseScenarioToUnifiedScenarioAdapter.Convert(scenario);
        var second = WarehouseScenarioToUnifiedScenarioAdapter.Convert(scenario);

        Assert.Equal(first.InitialInventory.ToArray(), second.InitialInventory.ToArray());
        Assert.Equal(first.Operations.ToArray(), second.Operations.ToArray());
    }

    [Fact]
    public void WarehouseScenarioToUnifiedScenarioAdapter_OutputRunsOnUnifiedRunner()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var legacy = new WarehouseScenarioRunner().Run(scenario);
        var unifiedScenario = WarehouseScenarioToUnifiedScenarioAdapter.Convert(scenario);

        var unified = new WarehouseScenarioRunner().RunUnified(unifiedScenario);

        Assert.Equal(legacy.CompletedReceipts, unified.CompletedReceipts);
        Assert.Equal(legacy.CompletedOutboundOrders, unified.CompletedOutboundOrders);
        Assert.Equal(legacy.CompletedEachPickOrders, unified.CompletedEachPickOrders);
        Assert.Equal(legacy.TotalQuantityAvailable, unified.TotalQuantityAvailable);
        Assert.Equal(legacy.TotalQuantityShipped, unified.TotalQuantityShipped);
        Assert.Equal(legacy.TotalQuantityPicked, unified.TotalQuantityPicked);
        Assert.Equal(7m, unified.FinalInventorySnapshot["sku-inbound-1"]);
        Assert.Equal(0m, unified.FinalInventorySnapshot["sku-outbound-1"]);
        Assert.Equal(0m, unified.FinalInventorySnapshot["sku-each-1"]);
    }

    [Fact]
    public void WarehouseScenarioToUnifiedScenarioAdapter_DoesNotSwitchDefaultRunner()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();

        var legacy = new WarehouseScenarioRunner().Run(scenario);

        Assert.Equal(220, legacy.FinishedAtMs);
        Assert.Empty(legacy.FinalInventorySnapshot);
    }
}
