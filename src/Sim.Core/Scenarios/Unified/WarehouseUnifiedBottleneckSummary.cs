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

        if (resourceKpis.Count == 0)
        {
            throw new DomainRuleViolationException(
                "Unified bottleneck summary requires at least one resource KPI.");
        }

        var candidate = resourceKpis.Values
            .OrderByDescending(summary => summary.Utilization)
            .ThenByDescending(summary => summary.TotalBusyDurationMs)
            .ThenByDescending(summary => summary.OperationCount)
            .ThenBy(summary => summary.ResourceId, StringComparer.Ordinal)
            .First();

        return new WarehouseUnifiedBottleneckSummary(
            candidate.ResourceId,
            candidate.Utilization,
            candidate.TotalBusyDurationMs,
            candidate.OperationCount);
    }
}
