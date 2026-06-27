using Sim.Core.Domain;
using Sim.Core.World;

namespace Sim.Core.Scenarios;

public sealed record WarehouseRunResult
{
    public WarehouseRunResult(
        string scenarioId,
        int seed,
        InboundRunResult? inboundResult,
        OutboundRunResult? outboundResult,
        EachPickRunResult? eachPickResult,
        long startedAtMs,
        long finishedAtMs,
        string eventLogText,
        WorldState finalWorldState)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new DomainRuleViolationException("WarehouseRunResult ScenarioId cannot be empty.");
        }

        if (inboundResult is null &&
            outboundResult is null &&
            eachPickResult is null)
        {
            throw new DomainRuleViolationException(
                "WarehouseRunResult requires at least one child result.");
        }

        if (startedAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"WarehouseRunResult StartedAtMs cannot be negative. StartedAtMs: {startedAtMs}.");
        }

        if (finishedAtMs < startedAtMs)
        {
            throw new DomainRuleViolationException(
                $"WarehouseRunResult FinishedAtMs cannot be earlier than StartedAtMs. StartedAtMs: {startedAtMs}, FinishedAtMs: {finishedAtMs}.");
        }

        ScenarioId = scenarioId;
        Seed = seed;
        InboundResult = inboundResult;
        OutboundResult = outboundResult;
        EachPickResult = eachPickResult;
        StartedAtMs = startedAtMs;
        FinishedAtMs = finishedAtMs;
        EventLogText = eventLogText ?? string.Empty;
        FinalWorldState = finalWorldState ?? throw new ArgumentNullException(nameof(finalWorldState));
    }

    public string ScenarioId { get; }

    public int Seed { get; }

    public InboundRunResult? InboundResult { get; }

    public OutboundRunResult? OutboundResult { get; }

    public EachPickRunResult? EachPickResult { get; }

    public int CompletedReceipts => InboundResult?.CompletedReceipts ?? 0;

    public int CompletedOutboundOrders => OutboundResult?.CompletedOrders ?? 0;

    public int CompletedEachPickOrders => EachPickResult?.CompletedEachPickOrders ?? 0;

    public decimal TotalQuantityAvailable => InboundResult?.TotalQuantityAvailable ?? 0m;

    public decimal TotalQuantityShipped => OutboundResult?.TotalQuantityShipped ?? 0m;

    public decimal TotalQuantityPicked => EachPickResult?.TotalQuantityPicked ?? 0m;

    public long StartedAtMs { get; }

    public long FinishedAtMs { get; }

    public string EventLogText { get; }

    public WarehouseKpiSummary KpiSummary => WarehouseKpiSummary.FromRunResult(this);

    public WorldState FinalWorldState { get; }
}
