using Sim.Core.Domain;

namespace Sim.Core.Scenarios;

public sealed record WarehouseKpiSummary
{
    public WarehouseKpiSummary(
        long totalDurationMs,
        int totalCompletedWorkItems,
        int eventLogLineCount,
        decimal receiptThroughputPerHour,
        decimal outboundOrderThroughputPerHour,
        decimal eachPickOrderThroughputPerHour,
        decimal totalWorkItemThroughputPerHour)
    {
        if (totalDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Warehouse KPI total duration cannot be negative. TotalDurationMs: {totalDurationMs}.");
        }

        if (totalCompletedWorkItems < 0)
        {
            throw new DomainRuleViolationException(
                $"Warehouse KPI completed work item count cannot be negative. TotalCompletedWorkItems: {totalCompletedWorkItems}.");
        }

        if (eventLogLineCount < 0)
        {
            throw new DomainRuleViolationException(
                $"Warehouse KPI event log line count cannot be negative. EventLogLineCount: {eventLogLineCount}.");
        }

        TotalDurationMs = totalDurationMs;
        TotalCompletedWorkItems = totalCompletedWorkItems;
        EventLogLineCount = eventLogLineCount;
        ReceiptThroughputPerHour = receiptThroughputPerHour;
        OutboundOrderThroughputPerHour = outboundOrderThroughputPerHour;
        EachPickOrderThroughputPerHour = eachPickOrderThroughputPerHour;
        TotalWorkItemThroughputPerHour = totalWorkItemThroughputPerHour;
    }

    public long TotalDurationMs { get; }

    public int TotalCompletedWorkItems { get; }

    public int EventLogLineCount { get; }

    public decimal ReceiptThroughputPerHour { get; }

    public decimal OutboundOrderThroughputPerHour { get; }

    public decimal EachPickOrderThroughputPerHour { get; }

    public decimal TotalWorkItemThroughputPerHour { get; }

    public static WarehouseKpiSummary FromRunResult(WarehouseRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var totalDurationMs = result.FinishedAtMs - result.StartedAtMs;
        var totalCompletedWorkItems =
            result.CompletedReceipts +
            result.CompletedOutboundOrders +
            result.CompletedEachPickOrders;

        var durationHours = ToHours(totalDurationMs);

        return new WarehouseKpiSummary(
            totalDurationMs,
            totalCompletedWorkItems,
            CountEventLogLines(result.EventLogText),
            PerHour(result.CompletedReceipts, durationHours),
            PerHour(result.CompletedOutboundOrders, durationHours),
            PerHour(result.CompletedEachPickOrders, durationHours),
            PerHour(totalCompletedWorkItems, durationHours));
    }

    private static decimal ToHours(long durationMs)
    {
        return durationMs <= 0
            ? 0m
            : durationMs / 3_600_000m;
    }

    private static decimal PerHour(int count, decimal durationHours)
    {
        return durationHours <= 0m
            ? 0m
            : count / durationHours;
    }

    private static int CountEventLogLines(string eventLogText)
    {
        if (string.IsNullOrWhiteSpace(eventLogText))
        {
            return 0;
        }

        return eventLogText
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Count(line => line.Length > 0);
    }
}
