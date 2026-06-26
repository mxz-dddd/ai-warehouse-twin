using Sim.Core.Des;
using Sim.Core.Processes.Outbound;
using Sim.Core.Processes.Outbound.Events;
using Sim.Core.Resources;
using Sim.Core.World;

namespace Sim.Core.Scenarios;

public sealed class OutboundScenarioRunner
{
    public OutboundRunResult Run(OutboundScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var workerPool = new ResourcePool(
            "outbound-workers",
            ResourceType.Worker,
            Enumerable.Range(1, scenario.WorkerCount)
                .Select(index => new ResourceUnit($"worker-{index}", ResourceType.Worker, $"Worker {index}")));

        var dockPool = new ResourcePool(
            "outbound-docks",
            ResourceType.Dock,
            Enumerable.Range(1, scenario.DockCount)
                .Select(index => new ResourceUnit($"dock-{index}", ResourceType.Dock, $"Dock {index}")));

        var outboundState = new OutboundSimulationState(
            scenario.Orders,
            scenario.InitialInventory,
            workerPool,
            dockPool,
            scenario.Parameters);

        var context = new SimulationContext(
            new SimClock(),
            new DeterministicRng(scenario.Seed),
            new SimEventQueue(),
            new SimEventLog(),
            new WorldState(0));

        foreach (var order in scenario.Orders.OrderBy(order => order.ReleasedAtMs).ThenBy(order => order.OrderId))
        {
            context.EventQueue.Enqueue(new OutboundOrderReleasedEvent(
                outboundState,
                order.OrderId,
                order.ReleasedAtMs));
        }

        var maxEvents = Math.Max(100, scenario.Orders.Count * 10);
        new Simulator(context).RunUntil(long.MaxValue, maxEvents);

        var startedAtMs = scenario.Orders.Min(order => order.ReleasedAtMs);
        var finishedAtMs = outboundState.CompletedAtByOrderId.Count == 0
            ? startedAtMs
            : outboundState.CompletedAtByOrderId.Values.Max();

        return new OutboundRunResult(
            scenario.ScenarioId,
            scenario.Seed,
            outboundState.CompletedOrderIds.Count,
            outboundState.TotalShippedQuantity(),
            startedAtMs,
            finishedAtMs,
            context.EventLog.ToDeterministicText(),
            context.WorldState);
    }
}
