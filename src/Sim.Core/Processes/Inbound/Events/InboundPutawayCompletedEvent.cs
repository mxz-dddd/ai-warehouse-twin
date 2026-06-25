using Sim.Core.Des;

namespace Sim.Core.Processes.Inbound.Events;

public sealed class InboundPutawayCompletedEvent : ISimEvent
{
    private readonly InboundSimulationState _state;

    public InboundPutawayCompletedEvent(InboundSimulationState state, string receiptId, long occursAtMs)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        ReceiptId = receiptId;
        OccursAtMs = occursAtMs;
    }

    public string ReceiptId { get; }

    public string EventId => $"inbound.putaway_completed.{ReceiptId}";

    public long OccursAtMs { get; }

    public string EventType => "InboundPutawayCompleted";

    public void Execute(SimulationContext context)
    {
        _state.MarkAvailable(ReceiptId, context.Clock.NowMs);
        InboundWorldState.UpdateReceipt(context, ReceiptId, InboundWorldState.Available);

        var forkliftLease = _state.TakeForkliftLease(ReceiptId);
        var nextForkliftLease = _state.ForkliftPool.Release(forkliftLease, context.Clock.NowMs);
        if (nextForkliftLease is null)
        {
            return;
        }

        var nextReceiptId = _state.ReceiptIdForRequest(nextForkliftLease.RequestId);
        _state.StoreForkliftLease(nextReceiptId, nextForkliftLease);
        context.EventQueue.Enqueue(new InboundPutawayCompletedEvent(
            _state,
            nextReceiptId,
            context.Clock.NowMs + _state.Parameters.PutawayTotalDurationMs));
        InboundWorldState.UpdateReceipt(context, nextReceiptId, InboundWorldState.PutawayInProgress);
    }
}
