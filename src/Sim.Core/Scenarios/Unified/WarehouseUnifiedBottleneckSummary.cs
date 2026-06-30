using System.Collections.ObjectModel;
using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Unified;

public sealed record WarehouseUnifiedBottleneckSummary
{
    public WarehouseUnifiedBottleneckSummary(
        string resourceId,
        decimal utilization,
        long totalBusyDurationMs,
        int operationCount)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Unified bottleneck summary resource id cannot be empty.");
        }

        if (utilization < 0m || utilization > 1m)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck summary utilization must be in the range [0, 1]. Utilization: {utilization}.");
        }

        if (totalBusyDurationMs <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck summary total busy duration must be positive. TotalBusyDurationMs: {totalBusyDurationMs}.");
        }

        if (operationCount <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck summary operation count must be positive. OperationCount: {operationCount}.");
        }

        ResourceId = resourceId;
        Utilization = utilization;
        TotalBusyDurationMs = totalBusyDurationMs;
        OperationCount = operationCount;
    }

    public string ResourceId { get; }

    public decimal Utilization { get; }

    public long TotalBusyDurationMs { get; }

    public int OperationCount { get; }

    public static WarehouseUnifiedBottleneckSummary FromResourceKpis(
        IReadOnlyDictionary<string, WarehouseUnifiedResourceKpiSummary> resourceKpis)
    {
        ArgumentNullException.ThrowIfNull(resourceKpis);

        var candidate = WarehouseUnifiedBottleneckRankedSummary
            .RankTop(resourceKpis, topN: 1)
            .Single();

        return new WarehouseUnifiedBottleneckSummary(
            candidate.ResourceId,
            candidate.Utilization,
            candidate.TotalBusyDurationMs,
            candidate.OperationCount);
    }
}

public sealed record WarehouseUnifiedBottleneckRankedSummary
{
    public WarehouseUnifiedBottleneckRankedSummary(
        int rank,
        string resourceId,
        string resourceType,
        decimal averageWaitingTimeMs,
        long totalWaitingTimeMs,
        long totalBusyDurationMs,
        int operationCount,
        decimal utilization)
    {
        if (rank <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck rank must be positive. Rank: {rank}.");
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Unified bottleneck ranked summary resource id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new DomainRuleViolationException(
                "Unified bottleneck ranked summary resource type cannot be empty.");
        }

        if (averageWaitingTimeMs < 0m)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck ranked summary average waiting time cannot be negative. AverageWaitingTimeMs: {averageWaitingTimeMs}.");
        }

        if (totalWaitingTimeMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck ranked summary total waiting time cannot be negative. TotalWaitingTimeMs: {totalWaitingTimeMs}.");
        }

        if (totalBusyDurationMs <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck ranked summary total busy duration must be positive. TotalBusyDurationMs: {totalBusyDurationMs}.");
        }

        if (operationCount <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck ranked summary operation count must be positive. OperationCount: {operationCount}.");
        }

        if (utilization < 0m || utilization > 1m)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck ranked summary utilization must be in the range [0, 1]. Utilization: {utilization}.");
        }

        Rank = rank;
        ResourceId = resourceId;
        ResourceType = resourceType;
        AverageWaitingTimeMs = averageWaitingTimeMs;
        TotalWaitingTimeMs = totalWaitingTimeMs;
        TotalBusyDurationMs = totalBusyDurationMs;
        OperationCount = operationCount;
        Utilization = utilization;
    }

    public int Rank { get; }

    public string ResourceId { get; }

    public string ResourceType { get; }

    public decimal AverageWaitingTimeMs { get; }

    public long TotalWaitingTimeMs { get; }

    public long TotalBusyDurationMs { get; }

    public int OperationCount { get; }

    public decimal Utilization { get; }

    public static IReadOnlyList<WarehouseUnifiedBottleneckRankedSummary> RankTop(
        IReadOnlyDictionary<string, WarehouseUnifiedResourceKpiSummary> resourceKpis,
        int topN)
    {
        ArgumentNullException.ThrowIfNull(resourceKpis);

        if (resourceKpis.Count == 0)
        {
            throw new DomainRuleViolationException(
                "Unified bottleneck summary requires at least one resource KPI.");
        }

        if (topN <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified bottleneck top-N must be positive. TopN: {topN}.");
        }

        var ranked = resourceKpis.Values
            .OrderByDescending(summary => summary.Utilization)
            .ThenByDescending(summary => summary.TotalWaitingTimeMs)
            .ThenByDescending(summary => summary.AverageWaitingTimeMs)
            .ThenByDescending(summary => summary.OperationCount)
            .ThenBy(summary => summary.ResourceId, StringComparer.Ordinal)
            .Take(topN)
            .Select((summary, index) => new WarehouseUnifiedBottleneckRankedSummary(
                index + 1,
                summary.ResourceId,
                summary.ResourceType,
                summary.AverageWaitingTimeMs,
                summary.TotalWaitingTimeMs,
                summary.TotalBusyDurationMs,
                summary.OperationCount,
                summary.Utilization))
            .ToArray();

        return new ReadOnlyCollection<WarehouseUnifiedBottleneckRankedSummary>(
            ranked);
    }
}
