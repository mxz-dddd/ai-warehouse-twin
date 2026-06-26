using Sim.Core.Des;

namespace Sim.Core.Processes.Outbound.Events;

public sealed class OutboundLoadCompletedEvent : ISimEvent
{
    private readonly OutboundSimulationState _state;

    public OutboundLoadCompletedEvent(OutboundSimulationState state, string orderId, long occursAtMs)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        OrderId = orderId;
        OccursAtMs = occursAtMs;
    }

    public string OrderId { get; }

    public string EventId => $"outbound.load_completed.{OrderId}";

    public long OccursAtMs { get; }

    public string EventType => "OutboundLoadCompleted";

    public void Execute(SimulationContext context)
    {
        _state.MarkShipped(OrderId, context.Clock.NowMs);
        OutboundWorldState.UpdateOrder(context, OrderId, OutboundWorldState.Shipped);

        var dockLease = _state.TakeDockLease(OrderId);
        var nextDockLease = _state.DockPool.Release(dockLease, context.Clock.NowMs);
        if (nextDockLease is null)
        {
            return;
        }

        var nextOrderId = _state.OrderIdForRequest(nextDockLease.RequestId);
        _state.StoreDockLease(nextOrderId, nextDockLease);
        context.EventQueue.Enqueue(new OutboundLoadCompletedEvent(
            _state,
            nextOrderId,
            context.Clock.NowMs + _state.Parameters.LoadTotalDurationMs));
        OutboundWorldState.UpdateOrder(context, nextOrderId, OutboundWorldState.Loading);
    }
}
