using Sim.Core.Domain;
using Sim.Core.Processes.Outbound;
using Sim.Core.Scenarios;
using Xunit;

namespace Sim.Core.Tests.Processes.Outbound;

public sealed class OutboundScenarioRunnerTests
{
    [Fact]
    public void Run_CompletesSingleOrderToShipped()
    {
        var scenario = new OutboundScenario(
            "single-outbound",
            seed: 123,
            [Order("order-1", quantity: 7m, releasedAtMs: 10)],
            [Inventory("inv-1", "sku-order-1", 7m, "pick-1", InventoryStatus.Available)],
            new OutboundProcessParameters(50, 50, 25, 75),
            workerCount: 1,
            dockCount: 1);

        var result = new OutboundScenarioRunner().Run(scenario);

        Assert.Equal(1, result.CompletedOrders);
        Assert.Equal(7m, result.TotalQuantityShipped);
        Assert.Equal(10, result.StartedAtMs);
        Assert.Equal(210, result.FinishedAtMs);
        Assert.Equal("SHIPPED", result.FinalWorldState.Entities["order:order-1"].Status);
        Assert.Contains("outbound.order_released.order-1", result.EventLogText);
        Assert.Contains("outbound.pick_completed.order-1", result.EventLogText);
        Assert.Contains("outbound.load_completed.order-1", result.EventLogText);
    }

    internal static OutboundOrder Order(
        string orderId,
        decimal quantity = 5m,
        long releasedAtMs = 0)
    {
        return new OutboundOrder(
            orderId,
            "warehouse-1",
            $"sku-{orderId}",
            quantity,
            "pick-1",
            "stage-1",
            "dock-1",
            releasedAtMs);
    }

    internal static OutboundInventoryItem Inventory(
        string inventoryUnitId,
        string skuId,
        decimal quantity,
        string locationId,
        InventoryStatus status)
    {
        return new OutboundInventoryItem(inventoryUnitId, skuId, quantity, locationId, status);
    }
}
