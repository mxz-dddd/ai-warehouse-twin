using Sim.Core.World;

namespace Sim.Core.Scenarios;

public sealed record InboundRunResult(
    string ScenarioId,
    int Seed,
    int CompletedReceipts,
    decimal TotalQuantityAvailable,
    long StartedAtMs,
    long FinishedAtMs,
    string EventLogText,
    WorldState FinalWorldState);
