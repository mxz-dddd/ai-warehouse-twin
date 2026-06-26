using Sim.Core.Des;
using Sim.Core.World;

namespace Sim.Core.Processes.Outbound.Events;

internal static class OutboundWorldState
{
    public const string WaitingWorker = "WAITING_WORKER";
    public const string Picking = "PICKING";
    public const string PickedStaged = "PICKED_STAGED";
    public const string WaitingDock = "WAITING_DOCK";
    public const string Loading = "LOADING";
    public const string Shipped = "SHIPPED";

    public static void UpdateOrder(SimulationContext context, string orderId, string status)
    {
        var (xMm, yMm, zMm) = status switch
        {
            WaitingWorker or Picking => (0L, 1000L, 0L),
            PickedStaged or WaitingDock => (1000L, 1000L, 0L),
            Loading => (2000L, 1000L, 0L),
            Shipped => (3000L, 1000L, 0L),
            _ => (0L, 1000L, 0L),
        };

        context.WorldState = context.WorldState.UpsertEntity(
            new EntityPose($"order:{orderId}", xMm, yMm, zMm, status));
    }
}
