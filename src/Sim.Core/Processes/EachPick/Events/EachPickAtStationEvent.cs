using Sim.Core.Des;
using Sim.Core.Domain;

namespace Sim.Core.Processes.EachPick.Events;

public sealed class EachPickAtStationEvent : ISimEvent
{
    private readonly EachPickSimulationState _state;

    public EachPickAtStationEvent(
        EachPickSimulationState state,
        string orderId,
        long occursAtMs)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));

        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new DomainRuleViolationException("EachPickAtStationEvent OrderId cannot be empty.");
        }

        if (occursAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"EachPickAtStationEvent OccursAtMs cannot be negative. OccursAtMs: {occursAtMs}.");
        }

        OrderId = orderId;
        OccursAtMs = occursAtMs;
    }

    public string OrderId { get; }

    public string EventId => $"each_pick.at_station.{OrderId}";

    public long OccursAtMs { get; }

    public string EventType => "EachPickAtStation";

    public void Execute(SimulationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var order = _state.GetOrder(OrderId);

        if (!_state.StartedAtMsByOrderId.ContainsKey(order.OrderId))
        {
            throw new DomainRuleViolationException(
                $"Each-pick order has not started. OrderId: {order.OrderId}.");
        }

        var toteId = $"tote-{order.OrderId}";
        if (!_state.Totes.ContainsKey(toteId))
        {
            throw new DomainRuleViolationException(
                $"Each-pick order has no bound tote. OrderId: {order.OrderId}, ToteId: {toteId}.");
        }

        var candidate = _state.Inventory.Values
            .Where(item =>
                item.SkuId == order.SkuId &&
                item.Status == InventoryStatus.Allocated &&
                item.Quantity >= order.Quantity)
            .OrderBy(item => item.InventoryUnitId)
            .FirstOrDefault();

        if (candidate is null)
        {
            throw new DomainRuleViolationException(
                $"Each-pick order has no ALLOCATED inventory at station. OrderId: {order.OrderId}, SkuId: {order.SkuId}, Quantity: {order.Quantity}.");
        }

        InventoryStateMachine.EnsureCanTransition(candidate.Status, InventoryStatus.Picking);

        _state.UpsertInventory(new EachPickInventoryItem(
            candidate.InventoryUnitId,
            candidate.SkuId,
            candidate.Quantity,
            order.PickStationId,
            InventoryStatus.Picking));

        EachPickResourceCoordinator.RequestStation(
            _state,
            order.OrderId,
            context);
    }
}
