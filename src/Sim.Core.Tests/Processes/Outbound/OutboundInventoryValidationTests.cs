using Sim.Core.Domain;
using Sim.Core.Processes.Outbound;
using Sim.Core.Resources;
using Sim.Core.Scenarios;
using Xunit;

namespace Sim.Core.Tests.Processes.Outbound;

public sealed class OutboundInventoryValidationTests
{
    [Fact]
    public void Run_Throws_WhenInventoryIsInsufficient()
    {
        var scenario = ScenarioWithInventory(
            OutboundScenarioRunnerTests.Inventory("inv-1", "sku-order-1", 4m, "pick-1", InventoryStatus.Available));

        Assert.Throws<DomainRuleViolationException>(() => new OutboundScenarioRunner().Run(scenario));
    }

    [Fact]
    public void Run_Throws_WhenInventoryIsNotAvailable()
    {
        var scenario = ScenarioWithInventory(
            OutboundScenarioRunnerTests.Inventory("inv-1", "sku-order-1", 5m, "pick-1", InventoryStatus.Allocated));

        Assert.Throws<DomainRuleViolationException>(() => new OutboundScenarioRunner().Run(scenario));
    }

    [Fact]
    public void AllocateInventory_TransitionsInventoryToAllocated()
    {
        var state = CreateState();

        state.AllocateInventory("order-1");

        Assert.Equal(InventoryStatus.Allocated, state.InventoryForOrder("order-1").Status);
    }

    [Fact]
    public void Run_CompletesInventoryAsShipped()
    {
        var scenario = ScenarioWithInventory(
            OutboundScenarioRunnerTests.Inventory("inv-1", "sku-order-1", 5m, "pick-1", InventoryStatus.Available));

        var result = new OutboundScenarioRunner().Run(scenario);

        Assert.Equal(1, result.CompletedOrders);
        Assert.Equal(5m, result.TotalQuantityShipped);
    }

    [Fact]
    public void Run_DoesNotAllowOverPickingFromLargerInventoryUnit()
    {
        var scenario = ScenarioWithInventory(
            OutboundScenarioRunnerTests.Inventory("inv-1", "sku-order-1", 10m, "pick-1", InventoryStatus.Available));

        Assert.Throws<DomainRuleViolationException>(() => new OutboundScenarioRunner().Run(scenario));
    }

    private static OutboundScenario ScenarioWithInventory(OutboundInventoryItem inventory)
    {
        return new OutboundScenario(
            "inventory-validation",
            seed: 123,
            [OutboundScenarioRunnerTests.Order("order-1")],
            [inventory],
            new OutboundProcessParameters(10, 0, 10, 0),
            workerCount: 1,
            dockCount: 1);
    }

    private static OutboundSimulationState CreateState()
    {
        return new OutboundSimulationState(
            [OutboundScenarioRunnerTests.Order("order-1")],
            [OutboundScenarioRunnerTests.Inventory("inv-1", "sku-order-1", 5m, "pick-1", InventoryStatus.Available)],
            new ResourcePool("workers", ResourceType.Worker, [new ResourceUnit("worker-1", ResourceType.Worker, "Worker 1")]),
            new ResourcePool("docks", ResourceType.Dock, [new ResourceUnit("dock-1", ResourceType.Dock, "Dock 1")]),
            new OutboundProcessParameters(10, 0, 10, 0));
    }
}
