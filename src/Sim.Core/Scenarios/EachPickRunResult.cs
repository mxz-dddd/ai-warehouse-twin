using Sim.Core.Domain;
using Sim.Core.World;

namespace Sim.Core.Scenarios;

public sealed record EachPickRunResult
{
    public EachPickRunResult(
        string scenarioId,
        int seed,
        int completedEachPickOrders,
        decimal totalQuantityPicked,
        long startedAtMs,
        long finishedAtMs,
        string eventLogText,
        WorldState finalWorldState)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new DomainRuleViolationException("EachPickRunResult ScenarioId cannot be empty.");
        }

        if (completedEachPickOrders < 0)
        {
            throw new DomainRuleViolationException(
                $"CompletedEachPickOrders cannot be negative. CompletedEachPickOrders: {completedEachPickOrders}.");
        }

        if (totalQuantityPicked < 0)
        {
            throw new DomainRuleViolationException(
                $"TotalQuantityPicked cannot be negative. TotalQuantityPicked: {totalQuantityPicked}.");
        }

        if (startedAtMs < 0)
        {
            throw new DomainRuleViolationException($"StartedAtMs cannot be negative. StartedAtMs: {startedAtMs}.");
        }

        if (finishedAtMs < startedAtMs)
        {
            throw new DomainRuleViolationException(
                $"FinishedAtMs cannot be earlier than StartedAtMs. StartedAtMs: {startedAtMs}, FinishedAtMs: {finishedAtMs}.");
        }

        ScenarioId = scenarioId;
        Seed = seed;
        CompletedEachPickOrders = completedEachPickOrders;
        TotalQuantityPicked = totalQuantityPicked;
        StartedAtMs = startedAtMs;
        FinishedAtMs = finishedAtMs;
        EventLogText = eventLogText ?? string.Empty;
        FinalWorldState = finalWorldState ?? throw new ArgumentNullException(nameof(finalWorldState));
    }

    public string ScenarioId { get; }

    public int Seed { get; }

    public int CompletedEachPickOrders { get; }

    public decimal TotalQuantityPicked { get; }

    public long StartedAtMs { get; }

    public long FinishedAtMs { get; }

    public string EventLogText { get; }

    public WorldState FinalWorldState { get; }
}
