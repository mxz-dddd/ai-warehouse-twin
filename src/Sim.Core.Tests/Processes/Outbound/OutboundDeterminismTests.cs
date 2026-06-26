using Sim.Core.Domain;
using Sim.Core.Processes.Outbound;
using Sim.Core.Scenarios;
using Xunit;

namespace Sim.Core.Tests.Processes.Outbound;

public sealed class OutboundDeterminismTests
{
    [Fact]
    public void Run_ProducesSameResult_ForSameScenario()
    {
        var scenario = new OutboundScenario(
            "deterministic-outbound",
            seed: 987,
            [Order("order-1", 0), Order("order-2", 25)],
            [
                Inventory("inv-1", "sku-order-1"),
                Inventory("inv-2", "sku-order-2"),
            ],
            new OutboundProcessParameters(100, 25, 50, 25),
            workerCount: 1,
            dockCount: 1);

        var first = new OutboundScenarioRunner().Run(scenario);
        var second = new OutboundScenarioRunner().Run(scenario);

        Assert.Equal(first.EventLogText, second.EventLogText);
        Assert.Equal(first.CompletedOrders, second.CompletedOrders);
        Assert.Equal(first.TotalQuantityShipped, second.TotalQuantityShipped);
        Assert.Equal(first.FinishedAtMs, second.FinishedAtMs);
    }

    private static OutboundOrder Order(string orderId, long releasedAtMs)
    {
        return OutboundScenarioRunnerTests.Order(orderId, releasedAtMs: releasedAtMs);
    }

    private static OutboundInventoryItem Inventory(string inventoryUnitId, string skuId)
    {
        return OutboundScenarioRunnerTests.Inventory(
            inventoryUnitId,
            skuId,
            5m,
            "pick-1",
            InventoryStatus.Available);
    }
}
