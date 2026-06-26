using Sim.Core.Des;
using Sim.Core.Domain;

namespace Sim.Core.Processes.EachPick.Events;

public sealed class EachPickOrderReleasedEvent : ISimEvent
{
    private readonly EachPickSimulationState _state;

    public EachPickOrderReleasedEvent(
        EachPickSimulationState state,
        string orderId,
        long occursAtMs)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));

        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new DomainRuleViolationException("EachPickOrderReleasedEvent OrderId cannot be empty.");
        }

        if (occursAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"EachPickOrderReleasedEvent OccursAtMs cannot be negative. OccursAtMs: {occursAtMs}.");
        }

        OrderId = orderId;
        OccursAtMs = occursAtMs;
    }

    public string OrderId { get; }

    public string EventId => $"each_pick.order_released.{OrderId}";

    public long OccursAtMs { get; }

    public string EventType => "EachPickOrderReleased";

    public void Execute(SimulationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var order = _state.GetOrder(OrderId);

        if (_state.StartedAtMsByOrderId.ContainsKey(order.OrderId))
        {
            throw new DomainRuleViolationException(
                $"Each-pick order has already started. OrderId: {order.OrderId}.");
        }

        var candidate = _state.Inventory.Values
            .Where(item =>
                item.SkuId == order.SkuId &&
                item.LocationId == order.SourceLocationId &&
                item.Status == InventoryStatus.Available &&
                item.Quantity >= order.Quantity)
            .OrderBy(item => item.InventoryUnitId)
            .FirstOrDefault();

        if (candidate is null)
        {
            throw new DomainRuleViolationException(
                $"Insufficient AVAILABLE each-pick inventory for order {order.OrderId}. SkuId: {order.SkuId}, SourceLocationId: {order.SourceLocationId}, Quantity: {order.Quantity}.");
        }

        InventoryStateMachine.EnsureCanTransition(candidate.Status, InventoryStatus.Allocated);

        _state.UpsertInventory(new EachPickInventoryItem(
            candidate.InventoryUnitId,
            candidate.SkuId,
            candidate.Quantity,
            candidate.LocationId,
            InventoryStatus.Allocated));

        _state.RegisterTote(new Tote(
            $"tote-{order.OrderId}",
            order.OrderId,
            "Bound"));

        _state.MarkStarted(order.OrderId, context.Clock.NowMs);
    }
}
