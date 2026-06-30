using Sim.Core.Domain;
using Sim.Core.Scenarios.Unified;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseUnifiedRichKpiSummaryTests
{
    [Fact]
    public void RichKpiSummary_OrderCyclePercentiles_UseNearestRank()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [
                Telemetry("op-1", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 10),
                Telemetry("op-2", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 20),
                Telemetry("op-3", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 30),
                Telemetry("op-4", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 40),
                Telemetry("op-5", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 50)
            ],
            runStartedAtMs: 0,
            runFinishedAtMs: 50,
            resourceKpis: EmptyResourceKpis());

        // sorted values ascending; rank = ceil(n * percentile); index = clamp(rank - 1, 0, n - 1)
        Assert.Equal(30, summary.OrderCycleP50Ms);
        Assert.Equal(50, summary.OrderCycleP90Ms);
        Assert.Equal(50, summary.OrderCycleP95Ms);
    }

    [Fact]
    public void CustomerKpiSummary_OrderCyclePercentiles_RejectEmptyValues()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => WarehouseUnifiedCustomerKpiSummary.FromTelemetry([]));

        Assert.Contains(
            "requires at least one telemetry record",
            exception.Message);
    }

    [Fact]
    public void RichKpiSummary_OrderCyclePercentiles_ReturnNullForEmptyTelemetry()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [],
            runStartedAtMs: 0,
            runFinishedAtMs: 0,
            resourceKpis: EmptyResourceKpis());

        Assert.Null(summary.OrderCycleP50Ms);
        Assert.Null(summary.OrderCycleP90Ms);
        Assert.Null(summary.OrderCycleP95Ms);
        Assert.Null(summary.AverageWaitMs);
        Assert.Empty(summary.ResourceUtilization);
        Assert.Empty(summary.Bottlenecks);
        Assert.Equal(0m, summary.ThroughputPerSimulatedHour);
    }

    [Fact]
    public void RichKpiSummary_OrderCyclePercentiles_SingleElementUsesOnlyValue()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [Telemetry("op-1", requestedAtMs: 0, startedAtMs: 5, finishedAtMs: 42)],
            runStartedAtMs: 0,
            runFinishedAtMs: 42,
            resourceKpis: EmptyResourceKpis());

        Assert.Equal(42, summary.OrderCycleP50Ms);
        Assert.Equal(42, summary.OrderCycleP90Ms);
        Assert.Equal(42, summary.OrderCycleP95Ms);
    }

    [Fact]
    public void RichKpiSummary_OrderCyclePercentiles_EvenCountUsesNearestRank()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [
                Telemetry("op-1", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 10),
                Telemetry("op-2", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 20),
                Telemetry("op-3", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 30),
                Telemetry("op-4", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 40)
            ],
            runStartedAtMs: 0,
            runFinishedAtMs: 40,
            resourceKpis: EmptyResourceKpis());

        Assert.Equal(20, summary.OrderCycleP50Ms);
        Assert.Equal(40, summary.OrderCycleP90Ms);
        Assert.Equal(40, summary.OrderCycleP95Ms);
    }

    [Fact]
    public void RichKpiSummary_OrderCyclePercentiles_OddCountUsesNearestRank()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [
                Telemetry("op-1", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 10),
                Telemetry("op-2", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 20),
                Telemetry("op-3", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 30)
            ],
            runStartedAtMs: 0,
            runFinishedAtMs: 30,
            resourceKpis: EmptyResourceKpis());

        Assert.Equal(20, summary.OrderCycleP50Ms);
        Assert.Equal(30, summary.OrderCycleP90Ms);
        Assert.Equal(30, summary.OrderCycleP95Ms);
    }

    [Fact]
    public void RichKpiSummary_OrderCyclePercentiles_BoundaryValuesRemainStable()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [
                Telemetry("op-1", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 0),
                Telemetry("op-2", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 1),
                Telemetry("op-3", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 3_600_000)
            ],
            runStartedAtMs: 0,
            runFinishedAtMs: 3_600_000,
            resourceKpis: EmptyResourceKpis());

        Assert.Equal(1, summary.OrderCycleP50Ms);
        Assert.Equal(3_600_000, summary.OrderCycleP90Ms);
        Assert.Equal(3_600_000, summary.OrderCycleP95Ms);
    }

    [Fact]
    public void RichKpiSummary_AvgWaitMs_UsesWaitingTimeOverCompletedOperationCount()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [
                Telemetry("op-1", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 10),
                Telemetry("op-2", requestedAtMs: 0, startedAtMs: 30, finishedAtMs: 40),
                Telemetry("op-3", requestedAtMs: 0, startedAtMs: 90, finishedAtMs: 100)
            ],
            runStartedAtMs: 0,
            runFinishedAtMs: 100,
            resourceKpis: EmptyResourceKpis());

        Assert.Equal(40m, summary.AverageWaitMs);
    }

    [Fact]
    public void RichKpiSummary_ResourceUtilization_UsesBusyTimeOverRunDurationRatio()
    {
        var resourceKpis =
            WarehouseUnifiedResourceKpiSummary.ByResourceId(
                [
                    Telemetry("dock-a", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 10, resourceId: "dock-1"),
                    Telemetry("dock-b", requestedAtMs: 0, startedAtMs: 20, finishedAtMs: 60, resourceId: "dock-1"),
                    Telemetry("station-a", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 25, resourceId: "station-1")
                ],
                runWindowDurationMs: 100);

        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [
                Telemetry("dock-a", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 10, resourceId: "dock-1"),
                Telemetry("dock-b", requestedAtMs: 0, startedAtMs: 20, finishedAtMs: 60, resourceId: "dock-1"),
                Telemetry("station-a", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 25, resourceId: "station-1")
            ],
            runStartedAtMs: 0,
            runFinishedAtMs: 100,
            resourceKpis);

        Assert.Equal(0.5m, summary.ResourceUtilization["dock-1"]);
        Assert.Equal(0.25m, summary.ResourceUtilization["station-1"]);
    }

    [Fact]
    public void RichKpiSummary_Bottlenecks_OrderByUtilizationThenWaitThenStableTieBreakers()
    {
        var resourceKpis =
            new Dictionary<string, WarehouseUnifiedResourceKpiSummary>
            {
                ["station-1"] = ResourceKpi(
                    "station-1",
                    utilization: 0.90m,
                    totalWaitingTimeMs: 20,
                    averageWaitingTimeMs: 10m,
                    operationCount: 2,
                    totalBusyDurationMs: 80),
                ["dock-2"] = ResourceKpi(
                    "dock-2",
                    utilization: 0.90m,
                    totalWaitingTimeMs: 20,
                    averageWaitingTimeMs: 10m,
                    operationCount: 2,
                    totalBusyDurationMs: 80),
                ["dock-1"] = ResourceKpi(
                    "dock-1",
                    utilization: 0.90m,
                    totalWaitingTimeMs: 30,
                    averageWaitingTimeMs: 15m,
                    operationCount: 2,
                    totalBusyDurationMs: 70),
                ["pack-1"] = ResourceKpi(
                    "pack-1",
                    utilization: 0.80m,
                    totalWaitingTimeMs: 100,
                    averageWaitingTimeMs: 100m,
                    operationCount: 1,
                    totalBusyDurationMs: 90)
            };

        var bottlenecks = WarehouseUnifiedBottleneckRankedSummary.RankTop(
            resourceKpis,
            topN: 4);

        Assert.Equal(
            ["dock-1", "dock-2", "station-1", "pack-1"],
            bottlenecks.Select(item => item.ResourceId));
        Assert.Equal([1, 2, 3, 4], bottlenecks.Select(item => item.Rank));
        Assert.All(bottlenecks, item => Assert.Equal("unknown", item.ResourceType));
        Assert.Equal(30, bottlenecks[0].TotalWaitingTimeMs);
        Assert.Equal(15m, bottlenecks[0].AverageWaitingTimeMs);
    }

    [Fact]
    public void RichKpiSummary_MissingResourceKpis_ReturnsEmptyResourceOutputs()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [Telemetry("op-1", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 10)],
            runStartedAtMs: 0,
            runFinishedAtMs: 10,
            resourceKpis: EmptyResourceKpis());

        Assert.Empty(summary.ResourceUtilization);
        Assert.Empty(summary.Bottlenecks);
    }

    [Fact]
    public void RichKpiSummary_ThroughputPerSimulatedHour_UsesCompletedCountOverDurationHours()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [
                Telemetry("op-1", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 100),
                Telemetry("op-2", requestedAtMs: 0, startedAtMs: 100, finishedAtMs: 200)
            ],
            runStartedAtMs: 0,
            runFinishedAtMs: 1_800_000,
            resourceKpis: EmptyResourceKpis());

        Assert.Equal(4m, summary.ThroughputPerSimulatedHour);
    }

    [Fact]
    public void RichKpiSummary_TravelDistance_ReturnsEmptyWhenNoReliableMovementDistance()
    {
        var summary = WarehouseUnifiedRichKpiSummary.FromTelemetry(
            [Telemetry("op-1", requestedAtMs: 0, startedAtMs: 0, finishedAtMs: 10)],
            runStartedAtMs: 0,
            runFinishedAtMs: 10,
            resourceKpis: EmptyResourceKpis());

        Assert.Empty(summary.TravelDistanceMByActorType);
    }

    [Fact]
    public void CombinedRunner_RichKpiSummary_ComputesCoreA3Kpis()
    {
        var result = new WarehouseUnifiedOperationRunner().Run(
            new Dictionary<string, decimal> { ["SKU-A"] = 100m },
            [
                Operation("a-dock", requestedAtMs: 0, resourceId: "dock-1", durationMs: 10),
                Operation("b-dock", requestedAtMs: 0, resourceId: "dock-1", durationMs: 20),
                Operation("c-station", requestedAtMs: 0, resourceId: "station-1", durationMs: 50)
            ]);

        Assert.Equal(30, result.RichKpiSummary.OrderCycleP50Ms);
        Assert.Equal(50, result.RichKpiSummary.OrderCycleP90Ms);
        Assert.Equal(50, result.RichKpiSummary.OrderCycleP95Ms);
        Assert.Equal(10m / 3m, result.RichKpiSummary.AverageWaitMs);
        Assert.Equal(0.6m, result.RichKpiSummary.ResourceUtilization["dock-1"]);
        Assert.Equal(1m, result.RichKpiSummary.ResourceUtilization["station-1"]);
        Assert.Equal("station-1", result.RichKpiSummary.Bottlenecks[0].ResourceId);
        Assert.Equal(216_000m, result.RichKpiSummary.ThroughputPerSimulatedHour);
        Assert.Empty(result.RichKpiSummary.TravelDistanceMByActorType);
    }

    private static WarehouseUnifiedOperationTelemetry Telemetry(
        string operationId,
        long requestedAtMs,
        long startedAtMs,
        long finishedAtMs,
        string resourceId = "dock-1")
    {
        return new WarehouseUnifiedOperationTelemetry(
            operationId,
            WarehouseUnifiedOperationType.Outbound,
            resourceId,
            requestedAtMs,
            startedAtMs,
            finishedAtMs,
            startedAtMs - requestedAtMs,
            finishedAtMs - startedAtMs,
            "SKU-A",
            -1m);
    }

    private static WarehouseUnifiedResourceKpiSummary ResourceKpi(
        string resourceId,
        decimal utilization,
        long totalWaitingTimeMs,
        decimal averageWaitingTimeMs,
        int operationCount,
        long totalBusyDurationMs)
    {
        return new WarehouseUnifiedResourceKpiSummary(
            resourceId,
            operationCount,
            totalBusyDurationMs,
            utilization,
            firstStartedAtMs: 0,
            lastFinishedAtMs: totalBusyDurationMs,
            totalWaitingTimeMs,
            averageWaitingTimeMs);
    }

    private static IReadOnlyDictionary<string, WarehouseUnifiedResourceKpiSummary> EmptyResourceKpis()
    {
        return new Dictionary<string, WarehouseUnifiedResourceKpiSummary>();
    }

    private static WarehouseUnifiedOperation Operation(
        string operationId,
        long requestedAtMs,
        string resourceId,
        long durationMs)
    {
        return new WarehouseUnifiedOperation(
            operationId,
            WarehouseUnifiedOperationType.Inbound,
            requestedAtMs,
            resourceId,
            durationMs,
            "SKU-A",
            1m);
    }
}
