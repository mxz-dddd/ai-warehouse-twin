using Sim.Core.Domain;
using Sim.Core.Processes.Outbound;
using Sim.Core.Scenarios;
using Xunit;

namespace Sim.Core.Tests.Processes.Outbound;

public sealed class OutboundQueueingTests
{
    [Fact]
    public void Run_QueuesSecondOrder_WhenSingleWorkerIsBusy()
    {
        var scenario = new OutboundScenario(
            "worker-queue",
            seed: 123,
            [Order("order-1"), Order("order-2")],
            [
                Inventory("inv-1", "sku-order-1"),
                Inventory("inv-2", "sku-order-2"),
            ],
            new OutboundProcessParameters(100, 0, 10, 0),
            workerCount: 1,
            dockCount: 2);

        var result = new OutboundScenarioRunner().Run(scenario);

        Assert.Equal(2, result.CompletedOrders);
        Assert.Equal(10m, result.TotalQuantityShipped);
        Assert.Equal(210, result.FinishedAtMs);
        Assert.Equal(
            "0|0|outbound.order_released.order-1|OutboundOrderReleased\n" +
            "1|0|outbound.order_released.order-2|OutboundOrderReleased\n" +
            "2|100|outbound.pick_completed.order-1|OutboundPickCompleted\n" +
            "3|110|outbound.load_completed.order-1|OutboundLoadCompleted\n" +
            "4|200|outbound.pick_completed.order-2|OutboundPickCompleted\n" +
            "5|210|outbound.load_completed.order-2|OutboundLoadCompleted",
            result.EventLogText);
    }

    [Fact]
    public void Run_QueuesSecondOrder_WhenSingleDockIsBusy()
    {
        var scenario = new OutboundScenario(
            "dock-queue",
            seed: 123,
            [Order("order-1"), Order("order-2")],
            [
                Inventory("inv-1", "sku-order-1"),
                Inventory("inv-2", "sku-order-2"),
            ],
            new OutboundProcessParameters(100, 0, 100, 0),
            workerCount: 2,
            dockCount: 1);

        var result = new OutboundScenarioRunner().Run(scenario);

        Assert.Equal(2, result.CompletedOrders);
        Assert.Equal(300, result.FinishedAtMs);
        Assert.Contains("2|100|outbound.pick_completed.order-1|OutboundPickCompleted", result.EventLogText);
        Assert.Contains("3|100|outbound.pick_completed.order-2|OutboundPickCompleted", result.EventLogText);
        Assert.Contains("4|200|outbound.load_completed.order-1|OutboundLoadCompleted", result.EventLogText);
        Assert.Contains("5|300|outbound.load_completed.order-2|OutboundLoadCompleted", result.EventLogText);
    }

    private static OutboundOrder Order(string orderId)
    {
        return OutboundScenarioRunnerTests.Order(orderId);
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
