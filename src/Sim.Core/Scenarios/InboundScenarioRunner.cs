using Sim.Core.Des;
using Sim.Core.Processes.Inbound;
using Sim.Core.Processes.Inbound.Events;
using Sim.Core.Resources;
using Sim.Core.World;

namespace Sim.Core.Scenarios;

public sealed class InboundScenarioRunner
{
    public InboundRunResult Run(InboundScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var dockPool = new ResourcePool(
            "inbound-docks",
            ResourceType.Dock,
            Enumerable.Range(1, scenario.DockCount)
                .Select(index => new ResourceUnit($"dock-{index}", ResourceType.Dock, $"Dock {index}")));

        var forkliftPool = new ResourcePool(
            "inbound-forklifts",
            ResourceType.Forklift,
            Enumerable.Range(1, scenario.ForkliftCount)
                .Select(index => new ResourceUnit($"forklift-{index}", ResourceType.Forklift, $"Forklift {index}")));

        var inboundState = new InboundSimulationState(
            scenario.Receipts,
            dockPool,
            forkliftPool,
            scenario.Parameters);

        var context = new SimulationContext(
            new SimClock(),
            new DeterministicRng(scenario.Seed),
            new SimEventQueue(),
            new SimEventLog(),
            new WorldState(0));

        foreach (var receipt in scenario.Receipts.OrderBy(receipt => receipt.ArrivesAtMs).ThenBy(receipt => receipt.ReceiptId))
        {
            context.EventQueue.Enqueue(new InboundReceiptArrivedEvent(
                inboundState,
                receipt.ReceiptId,
                receipt.ArrivesAtMs));
        }

        var maxEvents = Math.Max(100, scenario.Receipts.Count * 10);
        new Simulator(context).RunUntil(long.MaxValue, maxEvents);

        var startedAtMs = scenario.Receipts.Min(receipt => receipt.ArrivesAtMs);
        var finishedAtMs = inboundState.CompletedAtByReceiptId.Count == 0
            ? startedAtMs
            : inboundState.CompletedAtByReceiptId.Values.Max();

        return new InboundRunResult(
            scenario.ScenarioId,
            scenario.Seed,
            inboundState.CompletedReceiptIds.Count,
            inboundState.TotalAvailableQuantity(),
            startedAtMs,
            finishedAtMs,
            context.EventLog.ToDeterministicText(),
            context.WorldState);
    }
}
