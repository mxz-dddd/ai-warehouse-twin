using Xunit;
using Sim.Core.Des;
using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Processes.EachPick.Events;
using Sim.Core.Resources;
using Sim.Core.World;

namespace Sim.Core.Tests.Processes.EachPick.Events;

public sealed class EachPickOrderReleasedEventTests
{
    [Fact]
    public void Constructor_Throws_ForEmptyOrderId()
    {
        var state = State();

        Assert.Throws<DomainRuleViolationException>(() =>
            new EachPickOrderReleasedEvent(state, "", 0));
    }

    [Fact]
    public void Constructor_Throws_ForNegativeOccursAtMs()
    {
        var state = State();

        Assert.Throws<DomainRuleViolationException>(() =>
            new EachPickOrderReleasedEvent(state, "order-1", -1));
    }

    [Fact]
    public void Execute_StartsOrder_BindsTote_AndAllocatesInventory()
    {
        var state = State();
        var context = Context(nowMs: 42);
        var simEvent = new EachPickOrderReleasedEvent(state, "order-1", 0);

        simEvent.Execute(context);

        Assert.True(state.StartedAtMsByOrderId.ContainsKey("order-1"));
        Assert.Equal(42, state.StartedAtMsByOrderId["order-1"]);

        Assert.True(state.Totes.ContainsKey("tote-order-1"));
        Assert.Equal("order-1", state.Totes["tote-order-1"].OrderId);
        Assert.Equal("Bound", state.Totes["tote-order-1"].Status);

        Assert.Equal(InventoryStatus.Allocated, state.GetInventory("inv-1").Status);
    }

    [Fact]
    public void Execute_Throws_WhenOrderDoesNotExist()
    {
        var state = State();
        var context = Context();
        var simEvent = new EachPickOrderReleasedEvent(state, "missing-order", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenNoMatchingSkuInventoryExists()
    {
        var state = State(inventorySkuId: "other-sku");
        var context = Context();
        var simEvent = new EachPickOrderReleasedEvent(state, "order-1", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenNoMatchingSourceLocationInventoryExists()
    {
        var state = State(inventoryLocationId: "other-location");
        var context = Context();
        var simEvent = new EachPickOrderReleasedEvent(state, "order-1", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenInventoryQuantityIsInsufficient()
    {
        var state = State(inventoryQuantity: 4m);
        var context = Context();
        var simEvent = new EachPickOrderReleasedEvent(state, "order-1", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenInventoryIsNotAvailable()
    {
        var state = State(inventoryStatus: InventoryStatus.Allocated);
        var context = Context();
        var simEvent = new EachPickOrderReleasedEvent(state, "order-1", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenOrderIsReleasedTwice()
    {
        var state = State();
        var context = Context();
        var simEvent = new EachPickOrderReleasedEvent(state, "order-1", 0);

        simEvent.Execute(context);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    private static EachPickSimulationState State(
        string orderId = "order-1",
        string orderSkuId = "sku-1",
        decimal orderQuantity = 5m,
        string sourceLocationId = "pick-face-1",
        string inventoryUnitId = "inv-1",
        string inventorySkuId = "sku-1",
        decimal inventoryQuantity = 5m,
        string inventoryLocationId = "pick-face-1",
        InventoryStatus inventoryStatus = InventoryStatus.Available)
    {
        return new EachPickSimulationState(
            [
                new EachPickOrder(
                    orderId,
                    "warehouse-1",
                    orderSkuId,
                    orderQuantity,
                    sourceLocationId,
                    "station-1",
                    "stage-1",
                    0)
            ],
            [
                new EachPickInventoryItem(
                    inventoryUnitId,
                    inventorySkuId,
                    inventoryQuantity,
                    inventoryLocationId,
                    inventoryStatus)
            ],
            new ResourcePool(
                "station-pool",
                ResourceType.Station,
                [new ResourceUnit("station-1", ResourceType.Station, "Station 1")]),
            new ResourcePool(
                "worker-pool",
                ResourceType.Worker,
                [new ResourceUnit("worker-1", ResourceType.Worker, "Worker 1")]),
            new EachPickProcessParameters(10, 20, 30, 40));
    }

    private static SimulationContext Context(long nowMs = 0)
    {
        return new SimulationContext(
            new SimClock(nowMs),
            new DeterministicRng(123),
            new SimEventQueue(),
            new SimEventLog(),
            new WorldState(nowMs));
    }
}
