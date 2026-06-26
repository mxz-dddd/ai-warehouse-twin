using Sim.Core.Des;
using Sim.Core.Resources;

namespace Sim.Core.Processes.Outbound.Events;

public sealed class OutboundPickCompletedEvent : ISimEvent
{
    private readonly OutboundSimulationState _state;

    public OutboundPickCompletedEvent(OutboundSimulationState state, string orderId, long occursAtMs)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        OrderId = orderId;
        OccursAtMs = occursAtMs;
    }

    public string OrderId { get; }

    public string EventId => $"outbound.pick_completed.{OrderId}";

    public long OccursAtMs { get; }

    public string EventType => "OutboundPickCompleted";

    public void Execute(SimulationContext context)
    {
        var workerLease = _state.TakeWorkerLease(OrderId);
        var nextWorkerLease = _state.WorkerPool.Release(workerLease, context.Clock.NowMs);
        if (nextWorkerLease is not null)
        {
            var nextOrderId = _state.OrderIdForRequest(nextWorkerLease.RequestId);
            _state.StoreWorkerLease(nextOrderId, nextWorkerLease);
            _state.MarkPicking(nextOrderId);
            context.EventQueue.Enqueue(new OutboundPickCompletedEvent(
                _state,
                nextOrderId,
                context.Clock.NowMs + _state.Parameters.PickTotalDurationMs));
            OutboundWorldState.UpdateOrder(context, nextOrderId, OutboundWorldState.Picking);
        }

        _state.MarkPickedAndStaged(OrderId);
        OutboundWorldState.UpdateOrder(context, OrderId, OutboundWorldState.PickedStaged);

        var requestId = _state.DockRequestId(OrderId);
        _state.RegisterRequest(requestId, OrderId);
        var dockLease = _state.DockPool.AcquireOrQueue(
            new ResourceRequest(requestId, OrderId, context.Clock.NowMs),
            context.Clock.NowMs);

        if (dockLease is null)
        {
            OutboundWorldState.UpdateOrder(context, OrderId, OutboundWorldState.WaitingDock);
            return;
        }

        _state.StoreDockLease(OrderId, dockLease);
        context.EventQueue.Enqueue(new OutboundLoadCompletedEvent(
            _state,
            OrderId,
            context.Clock.NowMs + _state.Parameters.LoadTotalDurationMs));
        OutboundWorldState.UpdateOrder(context, OrderId, OutboundWorldState.Loading);
    }
}
