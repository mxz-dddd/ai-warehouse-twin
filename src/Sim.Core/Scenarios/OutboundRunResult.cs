using Sim.Core.World;

namespace Sim.Core.Scenarios;

public sealed record OutboundRunResult(
    string ScenarioId,
    int Seed,
    int CompletedOrders,
    decimal TotalQuantityShipped,
    long StartedAtMs,
    long FinishedAtMs,
    string EventLogText,
    WorldState FinalWorldState);
