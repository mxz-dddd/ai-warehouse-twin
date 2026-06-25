using Sim.Core.Des;
using Sim.Core.Resources;

namespace Sim.Core.Processes.Inbound.Events;

public sealed class InboundReceiptArrivedEvent : ISimEvent
{
    private readonly InboundSimulationState _state;

    public InboundReceiptArrivedEvent(InboundSimulationState state, string receiptId, long occursAtMs)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        ReceiptId = receiptId;
        OccursAtMs = occursAtMs;
    }

    public string ReceiptId { get; }

    public string EventId => $"inbound.receipt_arrived.{ReceiptId}";

    public long OccursAtMs { get; }

    public string EventType => "InboundReceiptArrived";

    public void Execute(SimulationContext context)
    {
        var receipt = _state.Receipts[ReceiptId];
        _state.MarkStarted(receipt.ReceiptId, context.Clock.NowMs);

        var requestId = _state.DockRequestId(receipt.ReceiptId);
        _state.RegisterRequest(requestId, receipt.ReceiptId);
        var lease = _state.DockPool.AcquireOrQueue(
            new ResourceRequest(requestId, receipt.ReceiptId, context.Clock.NowMs),
            context.Clock.NowMs);

        if (lease is null)
        {
            InboundWorldState.UpdateReceipt(context, receipt.ReceiptId, InboundWorldState.WaitingDock);
            return;
        }

        _state.StoreDockLease(receipt.ReceiptId, lease);
        context.EventQueue.Enqueue(new InboundUnloadCompletedEvent(
            _state,
            receipt.ReceiptId,
            context.Clock.NowMs + _state.Parameters.UnloadDurationMs));
        InboundWorldState.UpdateReceipt(context, receipt.ReceiptId, InboundWorldState.Unloading);
    }
}
