using Sim.Core.Des;
using Sim.Core.Resources;

namespace Sim.Core.Processes.Outbound.Events;

public sealed class OutboundOrderReleasedEvent : ISimEvent
{
    private readonly OutboundSimulationState _state;

    public OutboundOrderReleasedEvent(OutboundSimulationState state, string orderId, long occursAtMs)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        OrderId = orderId;
        OccursAtMs = occursAtMs;
    }

    public string OrderId { get; }

    public string EventId => $"outbound.order_released.{OrderId}";

    public long OccursAtMs { get; }

    public string EventType => "OutboundOrderReleased";

    public void Execute(SimulationContext context)
    {
        var order = _state.Orders[OrderId];
        _state.MarkStarted(order.OrderId, context.Clock.NowMs);
        _state.AllocateInventory(order.OrderId);

        var requestId = _state.WorkerRequestId(order.OrderId);
        _state.RegisterRequest(requestId, order.OrderId);
        var lease = _state.WorkerPool.AcquireOrQueue(
            new ResourceRequest(requestId, order.OrderId, context.Clock.NowMs),
            context.Clock.NowMs);

        if (lease is null)
        {
            OutboundWorldState.UpdateOrder(context, order.OrderId, OutboundWorldState.WaitingWorker);
            return;
        }

        _state.StoreWorkerLease(order.OrderId, lease);
        _state.MarkPicking(order.OrderId);
        context.EventQueue.Enqueue(new OutboundPickCompletedEvent(
            _state,
            order.OrderId,
            context.Clock.NowMs + _state.Parameters.PickTotalDurationMs));
        OutboundWorldState.UpdateOrder(context, order.OrderId, OutboundWorldState.Picking);
    }
}
