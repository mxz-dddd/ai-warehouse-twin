using System.Collections.ObjectModel;
using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Unified;

public sealed record WarehouseUnifiedResourceKpiSummary
{
    public WarehouseUnifiedResourceKpiSummary(
        string resourceId,
        int operationCount,
        long totalBusyDurationMs,
        decimal utilization,
        long firstStartedAtMs,
        long lastFinishedAtMs,
        long totalWaitingTimeMs = 0,
        decimal averageWaitingTimeMs = 0m,
        string resourceType = "unknown")
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Unified resource KPI resource id cannot be empty.");
        }

        if (operationCount <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified resource KPI operation count must be positive. OperationCount: {operationCount}.");
        }

        if (totalBusyDurationMs <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified resource KPI total busy duration must be positive. TotalBusyDurationMs: {totalBusyDurationMs}.");
        }

        if (utilization < 0m || utilization > 1m)
        {
            throw new DomainRuleViolationException(
                $"Unified resource KPI utilization must be in the range [0, 1]. Utilization: {utilization}.");
        }

        if (lastFinishedAtMs <= firstStartedAtMs)
        {
            throw new DomainRuleViolationException(
                $"Unified resource KPI last finish must be after first start. FirstStartedAtMs: {firstStartedAtMs}, LastFinishedAtMs: {lastFinishedAtMs}.");
        }

        if (totalWaitingTimeMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Unified resource KPI total waiting time cannot be negative. TotalWaitingTimeMs: {totalWaitingTimeMs}.");
        }

        if (averageWaitingTimeMs < 0m)
        {
            throw new DomainRuleViolationException(
                $"Unified resource KPI average waiting time cannot be negative. AverageWaitingTimeMs: {averageWaitingTimeMs}.");
        }

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new DomainRuleViolationException(
                "Unified resource KPI resource type cannot be empty.");
        }

        ResourceId = resourceId;
        OperationCount = operationCount;
        TotalBusyDurationMs = totalBusyDurationMs;
        Utilization = utilization;
        FirstStartedAtMs = firstStartedAtMs;
        LastFinishedAtMs = lastFinishedAtMs;
        TotalWaitingTimeMs = totalWaitingTimeMs;
        AverageWaitingTimeMs = averageWaitingTimeMs;
        ResourceType = resourceType;
    }

    public string ResourceId { get; }

    public int OperationCount { get; }

    public long TotalBusyDurationMs { get; }

    public decimal Utilization { get; }

    public long FirstStartedAtMs { get; }

    public long LastFinishedAtMs { get; }

    public long TotalWaitingTimeMs { get; }

    public decimal AverageWaitingTimeMs { get; }

    public string ResourceType { get; }

    public static IReadOnlyDictionary<string, WarehouseUnifiedResourceKpiSummary> ByResourceId(
        IReadOnlyList<WarehouseUnifiedOperationTelemetry> telemetry,
        long runWindowDurationMs)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (runWindowDurationMs <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified resource KPI run window duration must be positive. RunWindowDurationMs: {runWindowDurationMs}.");
        }

        var summaries = new SortedDictionary<string, WarehouseUnifiedResourceKpiSummary>(
            StringComparer.Ordinal);

        foreach (var group in telemetry
                     .GroupBy(item => item.ResourceId, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var items = group.ToArray();
            var totalBusyDurationMs = items.Sum(item => item.DurationMs);
            var totalWaitingTimeMs = items.Sum(item => item.WaitingTimeMs);

            summaries[group.Key] = new WarehouseUnifiedResourceKpiSummary(
                group.Key,
                items.Length,
                totalBusyDurationMs,
                totalBusyDurationMs / (decimal)runWindowDurationMs,
                items.Min(item => item.StartedAtMs),
                items.Max(item => item.FinishedAtMs),
                totalWaitingTimeMs,
                totalWaitingTimeMs / (decimal)items.Length);
        }

        return new ReadOnlyDictionary<string, WarehouseUnifiedResourceKpiSummary>(
            summaries);
    }
}
