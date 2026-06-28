using Sim.Core.Des;
using Sim.Core.Resources;

namespace Sim.Core.Processes.EachPick.Events;

internal static class EachPickResourceCoordinator
{
    public static void RequestStation(
        EachPickSimulationState state,
        string orderId,
        SimulationContext context)
    {
        var requestId = state.StationRequestId(orderId);
        state.RegisterRequest(requestId, orderId);
        var stationLease = state.StationPool.AcquireOrQueue(
            new ResourceRequest(requestId, orderId, context.Clock.NowMs),
            context.Clock.NowMs);

        if (stationLease is not null)
        {
            AcceptStationLease(state, stationLease, context);
        }
    }

    public static void AcceptStationLease(
        EachPickSimulationState state,
        ResourceLease stationLease,
        SimulationContext context)
    {
        var orderId = state.OrderIdForRequest(stationLease.RequestId);
        state.StoreStationLease(orderId, stationLease);
        RequestWorker(state, orderId, context);
    }

    public static void AcceptWorkerLease(
        EachPickSimulationState state,
        ResourceLease workerLease,
        SimulationContext context)
    {
        var orderId = state.OrderIdForRequest(workerLease.RequestId);
        state.StoreWorkerLease(orderId, workerLease);
        SchedulePickIfReady(state, orderId, context);
    }

    private static void RequestWorker(
        EachPickSimulationState state,
        string orderId,
        SimulationContext context)
    {
        var requestId = state.WorkerRequestId(orderId);
        state.RegisterRequest(requestId, orderId);
        var workerLease = state.WorkerPool.AcquireOrQueue(
            new ResourceRequest(requestId, orderId, context.Clock.NowMs),
            context.Clock.NowMs);

        if (workerLease is not null)
        {
            AcceptWorkerLease(state, workerLease, context);
        }
    }

    private static void SchedulePickIfReady(
        EachPickSimulationState state,
        string orderId,
        SimulationContext context)
    {
        if (!state.HasStationLease(orderId) ||
            !state.HasWorkerLease(orderId) ||
            !state.TryMarkPickScheduled(orderId))
        {
            return;
        }

        context.EventQueue.Enqueue(new EachPickCompletedEvent(
            state,
            orderId,
            context.Clock.NowMs + state.Parameters.PickServiceDurationMs));
    }
}
