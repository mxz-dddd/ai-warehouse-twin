using Sim.Core.Des;
using Sim.Core.World;

namespace Sim.Core.Processes.Inbound.Events;

internal static class InboundWorldState
{
    public const string WaitingDock = "WAITING_DOCK";
    public const string Unloading = "UNLOADING";
    public const string ReceivedStaging = "RECEIVED_STAGING";
    public const string WaitingForklift = "WAITING_FORKLIFT";
    public const string PutawayInProgress = "PUTAWAY_IN_PROGRESS";
    public const string Available = "AVAILABLE";

    public static void UpdateReceipt(SimulationContext context, string receiptId, string status)
    {
        var (xMm, yMm, zMm) = status switch
        {
            WaitingDock or Unloading => (0L, 0L, 0L),
            ReceivedStaging or WaitingForklift => (1000L, 0L, 0L),
            PutawayInProgress => (2000L, 0L, 0L),
            Available => (3000L, 0L, 0L),
            _ => (0L, 0L, 0L),
        };

        context.WorldState = context.WorldState.UpsertEntity(
            new EntityPose($"receipt:{receiptId}", xMm, yMm, zMm, status));
    }
}
