using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Unified;

public sealed record WarehouseUnifiedCustomerKpiSummary
{
    public WarehouseUnifiedCustomerKpiSummary(
        int operationCount,
        long totalWaitingTimeMs,
        decimal averageWaitingTimeMs,
        long maxWaitingTimeMs,
        long p50WaitingTimeMs,
        long p90WaitingTimeMs,
        long p95WaitingTimeMs,
        long totalCycleTimeMs,
        decimal averageCycleTimeMs,
        long maxCycleTimeMs,
        long p50CycleTimeMs,
        long p90CycleTimeMs,
        long p95CycleTimeMs,
        long totalServiceDurationMs)
    {
        if (operationCount <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified customer KPI operation count must be positive. OperationCount: {operationCount}.");
        }

        EnsureNonNegative(totalWaitingTimeMs, nameof(totalWaitingTimeMs));
        EnsureNonNegative(averageWaitingTimeMs, nameof(averageWaitingTimeMs));
        EnsureNonNegative(maxWaitingTimeMs, nameof(maxWaitingTimeMs));
        EnsureNonNegative(p50WaitingTimeMs, nameof(p50WaitingTimeMs));
        EnsureNonNegative(p90WaitingTimeMs, nameof(p90WaitingTimeMs));
        EnsureNonNegative(p95WaitingTimeMs, nameof(p95WaitingTimeMs));
        EnsureNonNegative(totalCycleTimeMs, nameof(totalCycleTimeMs));
        EnsureNonNegative(averageCycleTimeMs, nameof(averageCycleTimeMs));
        EnsureNonNegative(maxCycleTimeMs, nameof(maxCycleTimeMs));
        EnsureNonNegative(p50CycleTimeMs, nameof(p50CycleTimeMs));
        EnsureNonNegative(p90CycleTimeMs, nameof(p90CycleTimeMs));
        EnsureNonNegative(p95CycleTimeMs, nameof(p95CycleTimeMs));
        EnsureNonNegative(totalServiceDurationMs, nameof(totalServiceDurationMs));

        OperationCount = operationCount;
        TotalWaitingTimeMs = totalWaitingTimeMs;
        AverageWaitingTimeMs = averageWaitingTimeMs;
        MaxWaitingTimeMs = maxWaitingTimeMs;
        P50WaitingTimeMs = p50WaitingTimeMs;
        P90WaitingTimeMs = p90WaitingTimeMs;
        P95WaitingTimeMs = p95WaitingTimeMs;
        TotalCycleTimeMs = totalCycleTimeMs;
        AverageCycleTimeMs = averageCycleTimeMs;
        MaxCycleTimeMs = maxCycleTimeMs;
        P50CycleTimeMs = p50CycleTimeMs;
        P90CycleTimeMs = p90CycleTimeMs;
        P95CycleTimeMs = p95CycleTimeMs;
        TotalServiceDurationMs = totalServiceDurationMs;
    }

    public int OperationCount { get; }

    public long TotalWaitingTimeMs { get; }

    public decimal AverageWaitingTimeMs { get; }

    public long MaxWaitingTimeMs { get; }

    public long P50WaitingTimeMs { get; }

    public long P90WaitingTimeMs { get; }

    public long P95WaitingTimeMs { get; }

    public long TotalCycleTimeMs { get; }

    public decimal AverageCycleTimeMs { get; }

    public long MaxCycleTimeMs { get; }

    public long P50CycleTimeMs { get; }

    public long P90CycleTimeMs { get; }

    public long P95CycleTimeMs { get; }

    public long TotalServiceDurationMs { get; }

    public static WarehouseUnifiedCustomerKpiSummary FromTelemetry(
        IReadOnlyList<WarehouseUnifiedOperationTelemetry> telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (telemetry.Count == 0)
        {
            throw new DomainRuleViolationException(
                "Unified customer KPI summary requires at least one telemetry record.");
        }

        var waitingTimes = telemetry
            .Select(item => item.WaitingTimeMs)
            .Order()
            .ToArray();
        var cycleTimes = telemetry
            .Select(item => item.FinishedAtMs - item.RequestedAtMs)
            .Order()
            .ToArray();

        var totalWaitingTimeMs = waitingTimes.Sum();
        var totalCycleTimeMs = cycleTimes.Sum();

        return new WarehouseUnifiedCustomerKpiSummary(
            telemetry.Count,
            totalWaitingTimeMs,
            totalWaitingTimeMs / (decimal)telemetry.Count,
            waitingTimes[^1],
            PercentileNearestRank(waitingTimes, 0.50m),
            PercentileNearestRank(waitingTimes, 0.90m),
            PercentileNearestRank(waitingTimes, 0.95m),
            totalCycleTimeMs,
            totalCycleTimeMs / (decimal)telemetry.Count,
            cycleTimes[^1],
            PercentileNearestRank(cycleTimes, 0.50m),
            PercentileNearestRank(cycleTimes, 0.90m),
            PercentileNearestRank(cycleTimes, 0.95m),
            telemetry.Sum(item => item.DurationMs));
    }

    private static long PercentileNearestRank(
        IReadOnlyList<long> sortedValues,
        decimal percentile)
    {
        if (sortedValues.Count == 0)
        {
            throw new DomainRuleViolationException(
                "Cannot compute percentile for an empty value set.");
        }

        if (percentile <= 0m || percentile > 1m)
        {
            throw new DomainRuleViolationException(
                $"Percentile must be in the range (0, 1]. Percentile: {percentile}.");
        }

        var rank = (int)Math.Ceiling(sortedValues.Count * percentile);
        var index = Math.Clamp(rank - 1, 0, sortedValues.Count - 1);

        return sortedValues[index];
    }

    private static void EnsureNonNegative(long value, string name)
    {
        if (value < 0)
        {
            throw new DomainRuleViolationException(
                $"Unified customer KPI value cannot be negative. {name}: {value}.");
        }
    }

    private static void EnsureNonNegative(decimal value, string name)
    {
        if (value < 0m)
        {
            throw new DomainRuleViolationException(
                $"Unified customer KPI value cannot be negative. {name}: {value}.");
        }
    }
}
