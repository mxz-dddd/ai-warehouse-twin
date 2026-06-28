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
        return Run(scenario, traceCollector: null);
    }

    internal EachPickRunResult Run(
        EachPickScenario scenario,
        ResourceLeaseTraceCollector? traceCollector)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var stationPool = new ResourcePool(
            "each-pick-stations",
            ResourceType.Station,
            Enumerable.Range(1, scenario.StationCount)
                .Select(index => new ResourceUnit($"station-{index}", ResourceType.Station, $"Station {index}")),
            TraceContext(traceCollector, "station"));

        var workerPool = new ResourcePool(
            "each-pick-workers",
            ResourceType.Worker,
            Enumerable.Range(1, scenario.WorkerCount)
                .Select(index => new ResourceUnit($"worker-{index}", ResourceType.Worker, $"Worker {index}")),
            TraceContext(traceCollector, "worker"));

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
            context.EventQueue.Enqueue(new EachPickOrderReleasedEvent(
                eachPickState,
                order.OrderId,
                releasedAtMs));

            context.EventQueue.Enqueue(new EachPickAtStationEvent(
                eachPickState,
                order.OrderId,
                atStationAtMs));
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

    private static ResourceLeaseTraceContext? TraceContext(
        ResourceLeaseTraceCollector? collector,
        string stageType)
    {
        return collector is null
            ? null
            : new ResourceLeaseTraceContext(collector, "each_pick", stageType);
    }
}
