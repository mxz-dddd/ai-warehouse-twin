using Xunit;
using Sim.Core.Des;
using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Processes.EachPick.Events;
using Sim.Core.Resources;
using Sim.Core.World;

namespace Sim.Core.Tests.Processes.EachPick.Events;

public sealed class EachPickCompletedEventTests
{
    [Fact]
    public void Constructor_Throws_ForEmptyOrderId()
    {
        var state = State();

        Assert.Throws<DomainRuleViolationException>(() =>
            new EachPickCompletedEvent(state, "", 0));
    }

    [Fact]
    public void Constructor_Throws_ForNegativeOccursAtMs()
    {
        var state = State();

        Assert.Throws<DomainRuleViolationException>(() =>
            new EachPickCompletedEvent(state, "order-1", -1));
    }

    [Fact]
    public void Execute_MovesPickingInventoryToPicked()
    {
        var state = State();
        var context = Context(nowMs: 100);
        var simEvent = new EachPickCompletedEvent(state, "order-1", 0);

        simEvent.Execute(context);

        var item = state.GetInventory("inv-1");
        Assert.Equal(InventoryStatus.Picked, item.Status);
        Assert.Equal("station-1", item.LocationId);
    }

    [Fact]
    public void Execute_Throws_WhenOrderDoesNotExist()
    {
        var state = State();
        var context = Context();
        var simEvent = new EachPickCompletedEvent(state, "missing-order", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenOrderHasNotStarted()
    {
        var state = State(markStarted: false);
        var context = Context();
        var simEvent = new EachPickCompletedEvent(state, "order-1", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenOrderHasNoBoundTote()
    {
        var state = State(registerTote: false);
        var context = Context();
        var simEvent = new EachPickCompletedEvent(state, "order-1", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenNoPickingInventoryExists()
    {
        var state = State(inventoryStatus: InventoryStatus.Allocated);
        var context = Context();
        var simEvent = new EachPickCompletedEvent(state, "order-1", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenNoMatchingSkuPickingInventoryExists()
    {
        var state = State(inventorySkuId: "other-sku");
        var context = Context();
        var simEvent = new EachPickCompletedEvent(state, "order-1", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    [Fact]
    public void Execute_Throws_WhenPickingInventoryQuantityIsInsufficient()
    {
        var state = State(inventoryQuantity: 4m);
        var context = Context();
        var simEvent = new EachPickCompletedEvent(state, "order-1", 0);

        Assert.Throws<DomainRuleViolationException>(() => simEvent.Execute(context));
    }

    private static EachPickSimulationState State(
        bool markStarted = true,
        bool registerTote = true,
        string orderId = "order-1",
        string orderSkuId = "sku-1",
        decimal orderQuantity = 5m,
        string sourceLocationId = "pick-face-1",
        string pickStationId = "station-1",
        string inventoryUnitId = "inv-1",
        string inventorySkuId = "sku-1",
        decimal inventoryQuantity = 5m,
        string inventoryLocationId = "station-1",
        InventoryStatus inventoryStatus = InventoryStatus.Picking)
    {
        var state = new EachPickSimulationState(
            [
                new EachPickOrder(
                    orderId,
                    "warehouse-1",
                    orderSkuId,
                    orderQuantity,
                    sourceLocationId,
                    pickStationId,
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

        if (markStarted)
        {
            state.MarkStarted(orderId, 10);
        }

        if (registerTote)
        {
            state.RegisterTote(new Tote($"tote-{orderId}", orderId, "Bound"));
        }

        return state;
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
