using Sim.Core.Des;
using Sim.Core.Resources;

namespace Sim.Core.Processes.Inbound.Events;

public sealed class InboundUnloadCompletedEvent : ISimEvent
{
    private readonly InboundSimulationState _state;

    public InboundUnloadCompletedEvent(InboundSimulationState state, string receiptId, long occursAtMs)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        ReceiptId = receiptId;
        OccursAtMs = occursAtMs;
    }

    public string ReceiptId { get; }

    public string EventId => $"inbound.unload_completed.{ReceiptId}";

    public long OccursAtMs { get; }

    public string EventType => "InboundUnloadCompleted";

    public void Execute(SimulationContext context)
    {
        var dockLease = _state.TakeDockLease(ReceiptId);
        var nextDockLease = _state.DockPool.Release(dockLease, context.Clock.NowMs);
        if (nextDockLease is not null)
        {
            var nextReceiptId = _state.ReceiptIdForRequest(nextDockLease.RequestId);
            _state.StoreDockLease(nextReceiptId, nextDockLease);
            context.EventQueue.Enqueue(new InboundUnloadCompletedEvent(
                _state,
                nextReceiptId,
                context.Clock.NowMs + _state.Parameters.UnloadDurationMs));
            InboundWorldState.UpdateReceipt(context, nextReceiptId, InboundWorldState.Unloading);
        }

        _state.MarkReceived(ReceiptId);
        InboundWorldState.UpdateReceipt(context, ReceiptId, InboundWorldState.ReceivedStaging);

        var requestId = _state.ForkliftRequestId(ReceiptId);
        _state.RegisterRequest(requestId, ReceiptId);
        var forkliftLease = _state.ForkliftPool.AcquireOrQueue(
            new ResourceRequest(requestId, ReceiptId, context.Clock.NowMs),
            context.Clock.NowMs);

        if (forkliftLease is null)
        {
            InboundWorldState.UpdateReceipt(context, ReceiptId, InboundWorldState.WaitingForklift);
            return;
        }

        _state.StoreForkliftLease(ReceiptId, forkliftLease);
        context.EventQueue.Enqueue(new InboundPutawayCompletedEvent(
            _state,
            ReceiptId,
            context.Clock.NowMs + _state.Parameters.PutawayTotalDurationMs));
        InboundWorldState.UpdateReceipt(context, ReceiptId, InboundWorldState.PutawayInProgress);
    }
}
