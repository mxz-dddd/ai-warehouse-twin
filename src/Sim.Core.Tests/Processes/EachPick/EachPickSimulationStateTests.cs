using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Resources;
using Xunit;

namespace Sim.Core.Tests.Processes.EachPick;

public sealed class EachPickSimulationStateTests
{
    [Fact]
    public void Constructor_CreatesState()
    {
        var state = CreateState();

        Assert.Single(state.Orders);
        Assert.Single(state.Inventory);
        Assert.Empty(state.Totes);
        Assert.NotNull(state.StationPool);
        Assert.NotNull(state.WorkerPool);
        Assert.NotNull(state.Parameters);
    }

    [Fact]
    public void Constructor_Throws_ForEmptyOrders()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new EachPickSimulationState([], [Inventory()], StationPool(), WorkerPool(), Parameters()));
    }

    [Fact]
    public void Constructor_Throws_ForEmptyInitialInventory()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new EachPickSimulationState([Order()], [], StationPool(), WorkerPool(), Parameters()));
    }

    [Fact]
    public void Constructor_Throws_ForNullStationPool()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EachPickSimulationState([Order()], [Inventory()], null!, WorkerPool(), Parameters()));
    }

    [Fact]
    public void Constructor_Throws_ForNullWorkerPool()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EachPickSimulationState([Order()], [Inventory()], StationPool(), null!, Parameters()));
    }

    [Fact]
    public void Constructor_Throws_ForNullParameters()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EachPickSimulationState([Order()], [Inventory()], StationPool(), WorkerPool(), null!));
    }

    [Fact]
    public void Constructor_Throws_ForDuplicateOrderId()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new EachPickSimulationState([Order(), Order()], [Inventory()], StationPool(), WorkerPool(), Parameters()));
    }

    [Fact]
    public void Constructor_Throws_ForDuplicateInventoryUnitId()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new EachPickSimulationState([Order()], [Inventory(), Inventory()], StationPool(), WorkerPool(), Parameters()));
    }

    [Fact]
    public void GetOrder_ReturnsOrder()
    {
        var state = CreateState();

        Assert.Equal("order-1", state.GetOrder("order-1").OrderId);
    }

    [Fact]
    public void GetOrder_Throws_ForUnknownOrder()
    {
        var state = CreateState();

        Assert.Throws<DomainRuleViolationException>(() => state.GetOrder("missing"));
    }

    [Fact]
    public void GetInventory_ReturnsInventory()
    {
        var state = CreateState();

        Assert.Equal("inv-1", state.GetInventory("inv-1").InventoryUnitId);
    }

    [Fact]
    public void GetInventory_Throws_ForUnknownInventory()
    {
        var state = CreateState();

        Assert.Throws<DomainRuleViolationException>(() => state.GetInventory("missing"));
    }

    [Fact]
    public void UpsertInventory_UpdatesInventory()
    {
        var state = CreateState();
        var updated = new EachPickInventoryItem("inv-1", "sku-1", 5m, "station-1", InventoryStatus.Allocated);

        state.UpsertInventory(updated);

        Assert.Equal(InventoryStatus.Allocated, state.GetInventory("inv-1").Status);
        Assert.Equal("station-1", state.GetInventory("inv-1").LocationId);
    }

    [Fact]
    public void RegisterTote_AddsTote()
    {
        var state = CreateState();

        state.RegisterTote(new Tote("tote-1", "order-1", "EMPTY_BOUND"));

        Assert.True(state.Totes.ContainsKey("tote-1"));
    }

    [Fact]
    public void RegisterTote_Throws_ForDuplicateToteId()
    {
        var state = CreateState();
        state.RegisterTote(new Tote("tote-1", "order-1", "EMPTY_BOUND"));

        Assert.Throws<DomainRuleViolationException>(
            () => state.RegisterTote(new Tote("tote-1", "order-1", "EMPTY_BOUND")));
    }

    [Fact]
    public void MarkStarted_RecordsStartTime()
    {
        var state = CreateState();

        state.MarkStarted("order-1", 10);

        Assert.Equal(10, state.StartedAtMsByOrderId["order-1"]);
    }

    [Fact]
    public void MarkCompleted_RecordsCompletionAndCompletedOrderId()
    {
        var state = CreateState();
        state.MarkStarted("order-1", 10);

        state.MarkCompleted("order-1", 20);

        Assert.Equal(20, state.CompletedAtMsByOrderId["order-1"]);
        Assert.Contains("order-1", state.CompletedOrderIds);
    }

    [Fact]
    public void MarkCompleted_Throws_WhenCompletedBeforeStarted()
    {
        var state = CreateState();
        state.MarkStarted("order-1", 20);

        Assert.Throws<DomainRuleViolationException>(() => state.MarkCompleted("order-1", 10));
    }

    private static EachPickSimulationState CreateState()
    {
        return new EachPickSimulationState(
            [Order()],
            [Inventory()],
            StationPool(),
            WorkerPool(),
            Parameters());
    }

    private static EachPickOrder Order(string orderId = "order-1")
    {
        return new EachPickOrder(
            orderId,
            "warehouse-1",
            "sku-1",
            5m,
            "pick-face-1",
            "station-1",
            "stage-1",
            0);
    }

    private static EachPickInventoryItem Inventory(string inventoryUnitId = "inv-1")
    {
        return new EachPickInventoryItem(
            inventoryUnitId,
            "sku-1",
            5m,
            "pick-face-1",
            InventoryStatus.Available);
    }

    private static ResourcePool StationPool()
    {
        return new ResourcePool(
            "stations",
            ResourceType.Station,
            [new ResourceUnit("station-1", ResourceType.Station, "Station 1")]);
    }

    private static ResourcePool WorkerPool()
    {
        return new ResourcePool(
            "workers",
            ResourceType.Worker,
            [new ResourceUnit("worker-1", ResourceType.Worker, "Worker 1")]);
    }

    private static EachPickProcessParameters Parameters()
    {
        return new EachPickProcessParameters(10, 20, 30, 40);
    }
}
