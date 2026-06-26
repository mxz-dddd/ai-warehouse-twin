using Sim.Core.Des;
using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Processes.EachPick.Events;
using Sim.Core.Resources;
using Sim.Core.World;

namespace Sim.Core.Scenarios;

public sealed class EachPickScenarioRunner
{
    public EachPickRunResult Run(EachPickScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var stationPool = new ResourcePool(
            "each-pick-stations",
            ResourceType.Station,
            Enumerable.Range(1, scenario.StationCount)
                .Select(index => new ResourceUnit($"station-{index}", ResourceType.Station, $"Station {index}")));

        var workerPool = new ResourcePool(
            "each-pick-workers",
            ResourceType.Worker,
            Enumerable.Range(1, scenario.WorkerCount)
                .Select(index => new ResourceUnit($"worker-{index}", ResourceType.Worker, $"Worker {index}")));

        var eachPickState = new EachPickSimulationState(
            scenario.Orders,
            scenario.InitialInventory,
            stationPool,
            workerPool,
            scenario.Parameters);

        var context = new SimulationContext(
            new SimClock(),
            new DeterministicRng(scenario.Seed),
            new SimEventQueue(),
            new SimEventLog(),
            new WorldState(0));

        foreach (var order in scenario.Orders.OrderBy(order => order.ReleasedAtMs).ThenBy(order => order.OrderId))
        {
            var releasedAtMs = order.ReleasedAtMs;
            var atStationAtMs = releasedAtMs
                + scenario.Parameters.ToteBindDurationMs
                + scenario.Parameters.TravelToStationDurationMs;
            var completedAtMs = atStationAtMs + scenario.Parameters.PickServiceDurationMs;
            var stagedAtMs = completedAtMs + scenario.Parameters.MoveToStagingDurationMs;

            context.EventQueue.Enqueue(new EachPickOrderReleasedEvent(
                eachPickState,
                order.OrderId,
                releasedAtMs));

            context.EventQueue.Enqueue(new EachPickAtStationEvent(
                eachPickState,
                order.OrderId,
                atStationAtMs));

            context.EventQueue.Enqueue(new EachPickCompletedEvent(
                eachPickState,
                order.OrderId,
                completedAtMs));

            context.EventQueue.Enqueue(new EachPickStagedEvent(
                eachPickState,
                order.OrderId,
                stagedAtMs));
        }

        var maxEvents = Math.Max(100, scenario.Orders.Count * 20);
        new Simulator(context).RunUntil(long.MaxValue, maxEvents);

        var startedAtMs = scenario.Orders.Min(order => order.ReleasedAtMs);
        var finishedAtMs = eachPickState.CompletedAtMsByOrderId.Count == 0
            ? startedAtMs
            : eachPickState.CompletedAtMsByOrderId.Values.Max();

        var totalQuantityPicked = eachPickState.CompletedOrderIds
            .Select(orderId => eachPickState.GetOrder(orderId).Quantity)
            .Sum();

        return new EachPickRunResult(
            scenario.ScenarioId,
            scenario.Seed,
            eachPickState.CompletedOrderIds.Count,
            totalQuantityPicked,
            startedAtMs,
            finishedAtMs,
            context.EventLog.ToDeterministicText(),
            context.WorldState);
    }
}
