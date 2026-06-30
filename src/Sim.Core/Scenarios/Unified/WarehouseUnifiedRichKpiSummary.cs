using System.Collections.ObjectModel;
using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Unified;

public sealed record WarehouseUnifiedRichKpiSummary
{
    public WarehouseUnifiedRichKpiSummary(
        long? orderCycleP50Ms,
        long? orderCycleP90Ms,
        long? orderCycleP95Ms,
        decimal? averageWaitMs,
        IReadOnlyDictionary<string, decimal> resourceUtilization,
        IReadOnlyList<WarehouseUnifiedBottleneckRankedSummary> bottlenecks,
        IReadOnlyDictionary<string, decimal> travelDistanceMByActorType,
        decimal throughputPerSimulatedHour)
    {
        EnsureNonNegative(orderCycleP50Ms, nameof(orderCycleP50Ms));
        EnsureNonNegative(orderCycleP90Ms, nameof(orderCycleP90Ms));
        EnsureNonNegative(orderCycleP95Ms, nameof(orderCycleP95Ms));
        EnsureNonNegative(averageWaitMs, nameof(averageWaitMs));
        EnsureNonNegative(throughputPerSimulatedHour, nameof(throughputPerSimulatedHour));

        ArgumentNullException.ThrowIfNull(resourceUtilization);
        ArgumentNullException.ThrowIfNull(bottlenecks);
        ArgumentNullException.ThrowIfNull(travelDistanceMByActorType);

        OrderCycleP50Ms = orderCycleP50Ms;
        OrderCycleP90Ms = orderCycleP90Ms;
        OrderCycleP95Ms = orderCycleP95Ms;
        AverageWaitMs = averageWaitMs;
        ResourceUtilization = resourceUtilization;
        Bottlenecks = bottlenecks;
        TravelDistanceMByActorType = travelDistanceMByActorType;
        ThroughputPerSimulatedHour = throughputPerSimulatedHour;
    }

    public long? OrderCycleP50Ms { get; }

    public long? OrderCycleP90Ms { get; }

    public long? OrderCycleP95Ms { get; }

    public decimal? AverageWaitMs { get; }

    // Ratio in the range [0, 1], computed as busy time / simulated duration.
    public IReadOnlyDictionary<string, decimal> ResourceUtilization { get; }

    public IReadOnlyList<WarehouseUnifiedBottleneckRankedSummary> Bottlenecks { get; }

    public IReadOnlyDictionary<string, decimal> TravelDistanceMByActorType { get; }

    public decimal ThroughputPerSimulatedHour { get; }

    public static WarehouseUnifiedRichKpiSummary FromTelemetry(
        IReadOnlyList<WarehouseUnifiedOperationTelemetry> telemetry,
        long runStartedAtMs,
        long runFinishedAtMs,
        IReadOnlyDictionary<string, WarehouseUnifiedResourceKpiSummary> resourceKpis,
        int bottleneckTopN = 5)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(resourceKpis);

        if (runStartedAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Unified rich KPI run start cannot be negative. RunStartedAtMs: {runStartedAtMs}.");
        }

        if (runFinishedAtMs < runStartedAtMs)
        {
            throw new DomainRuleViolationException(
                $"Unified rich KPI run finish cannot be before run start. RunStartedAtMs: {runStartedAtMs}, RunFinishedAtMs: {runFinishedAtMs}.");
        }

        if (bottleneckTopN <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified rich KPI bottleneck top-N must be positive. BottleneckTopN: {bottleneckTopN}.");
        }

        if (telemetry.Count == 0)
        {
            return new WarehouseUnifiedRichKpiSummary(
                orderCycleP50Ms: null,
                orderCycleP90Ms: null,
                orderCycleP95Ms: null,
                averageWaitMs: null,
                resourceUtilization: EmptyDecimalDictionary(),
                bottlenecks: Array.Empty<WarehouseUnifiedBottleneckRankedSummary>(),
                travelDistanceMByActorType: EmptyDecimalDictionary(),
                throughputPerSimulatedHour: 0m);
        }

        var simulatedDurationMs = runFinishedAtMs - runStartedAtMs;

        if (simulatedDurationMs <= 0)
        {
            throw new DomainRuleViolationException(
                $"Unified rich KPI simulated duration must be positive when telemetry is present. SimulatedDurationMs: {simulatedDurationMs}.");
        }

        var customerSummary = WarehouseUnifiedCustomerKpiSummary.FromTelemetry(telemetry);
        var resourceUtilization = new SortedDictionary<string, decimal>(
            StringComparer.Ordinal);

        foreach (var entry in resourceKpis.OrderBy(
                     entry => entry.Key,
                     StringComparer.Ordinal))
        {
            resourceUtilization[entry.Key] = entry.Value.Utilization;
        }

        var bottlenecks = resourceKpis.Count == 0
            ? Array.Empty<WarehouseUnifiedBottleneckRankedSummary>()
            : WarehouseUnifiedBottleneckRankedSummary.RankTop(
                resourceKpis,
                Math.Min(bottleneckTopN, resourceKpis.Count));

        return new WarehouseUnifiedRichKpiSummary(
            customerSummary.P50CycleTimeMs,
            customerSummary.P90CycleTimeMs,
            customerSummary.P95CycleTimeMs,
            customerSummary.AverageWaitingTimeMs,
            new ReadOnlyDictionary<string, decimal>(resourceUtilization),
            bottlenecks,
            EmptyDecimalDictionary(),
            telemetry.Count * 3_600_000m / simulatedDurationMs);
    }

    private static IReadOnlyDictionary<string, decimal> EmptyDecimalDictionary()
    {
        return new ReadOnlyDictionary<string, decimal>(
            new SortedDictionary<string, decimal>(StringComparer.Ordinal));
    }

    private static void EnsureNonNegative(long? value, string name)
    {
        if (value < 0)
        {
            throw new DomainRuleViolationException(
                $"Unified rich KPI value cannot be negative. {name}: {value}.");
        }
    }

    private static void EnsureNonNegative(decimal? value, string name)
    {
        if (value < 0m)
        {
            throw new DomainRuleViolationException(
                $"Unified rich KPI value cannot be negative. {name}: {value}.");
        }
    }
}
