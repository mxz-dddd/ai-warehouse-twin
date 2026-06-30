using Sim.Core.Processes.Inbound;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Json;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseScenarioComparisonRunnerTests
{
    [Fact]
    public void Compare_ReturnsBaselineAndCandidateScenarioIds()
    {
        var result = new WarehouseScenarioComparisonRunner().Compare(
            Scenario("baseline", unloadDurationMs: 100),
            Scenario("candidate", unloadDurationMs: 50));

        Assert.Equal("baseline", result.BaselineScenarioId);
        Assert.Equal("candidate", result.CandidateScenarioId);
    }

    [Fact]
    public void Compare_ComputesMetricDeltas()
    {
        var result = new WarehouseScenarioComparisonRunner().Compare(
            Scenario("baseline", unloadDurationMs: 100),
            Scenario("candidate", unloadDurationMs: 50));

        var finishedAt = Delta(result, "finished_at_ms");
        Assert.Equal(100m, finishedAt.BaselineValue);
        Assert.Equal(50m, finishedAt.CandidateValue);
        Assert.Equal(-50m, finishedAt.Delta);
        Assert.Equal("decrease", finishedAt.Direction);

        var throughput = Delta(
            result,
            "inbound_receipt_throughput_per_hour");
        Assert.InRange(throughput.BaselineValue, 35_999.999m, 36_000.001m);
        Assert.InRange(throughput.CandidateValue, 71_999.999m, 72_000.001m);
        Assert.InRange(throughput.Delta, 35_999.999m, 36_000.001m);
        Assert.Equal("increase", throughput.Direction);
    }

    [Fact]
    public void Compare_ComputesDeltaPercent_WhenBaselineNonZero()
    {
        var delta = new WarehouseScenarioComparisonDelta(
            "unit_test_metric",
            baselineValue: 100m,
            candidateValue: 125m);

        Assert.Equal(25m, delta.Delta);
        Assert.Equal(25m, delta.DeltaPercent);
        Assert.Equal("increase", delta.Direction);
    }

    [Fact]
    public void Compare_UsesNullDeltaPercent_WhenBaselineZero()
    {
        var delta = new WarehouseScenarioComparisonDelta(
            "unit_test_metric",
            baselineValue: 0m,
            candidateValue: 10m);

        Assert.Equal(10m, delta.Delta);
        Assert.Null(delta.DeltaPercent);
        Assert.Equal("increase", delta.Direction);
    }

    [Fact]
    public void Compare_IsDeterministic()
    {
        var runner = new WarehouseScenarioComparisonRunner();
        var baseline = Scenario("baseline", unloadDurationMs: 100);
        var candidate = Scenario("candidate", unloadDurationMs: 50);

        var first = runner.Compare(baseline, candidate);
        var second = runner.Compare(baseline, candidate);

        Assert.Equal(first.Deltas.ToArray(), second.Deltas.ToArray());
        Assert.Equal(first.BaselineMetrics, second.BaselineMetrics);
        Assert.Equal(first.CandidateMetrics, second.CandidateMetrics);
    }

    [Fact]
    public void WarehouseScenarioComparisonRunner_Compare_DefaultPath_RemainsLegacy()
    {
        var baseline = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());
        var candidate = WarehouseScenarioJsonLoader.Load(CandidateScenarioPath());

        var result = new WarehouseScenarioComparisonRunner().Compare(
            baseline,
            candidate);

        Assert.Equal(220, result.BaselineMetrics.FinishedAtMs);
        Assert.Equal(210, result.CandidateMetrics.FinishedAtMs);
        Assert.Equal(3, TotalCompleted(result.BaselineMetrics));
        Assert.Equal(3, TotalCompleted(result.CandidateMetrics));
        Assert.InRange(
            result.BaselineMetrics.TotalWorkItemThroughputPerHour,
            51_428.570m,
            51_428.572m);
        Assert.Equal(
            -10m,
            Delta(result, "finished_at_ms").Delta);
    }

    [Fact]
    public void WarehouseScenarioComparisonRunner_CompareWithUnifiedAdapter_RunsSampleComparison()
    {
        var baseline = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());
        var candidate = WarehouseScenarioJsonLoader.Load(CandidateScenarioPath());

        var result = new WarehouseScenarioComparisonRunner()
            .CompareWithUnifiedAdapter(baseline, candidate);

        Assert.Equal("sample-small-warehouse", result.BaselineScenarioId);
        Assert.Equal("sample-small-warehouse-candidate", result.CandidateScenarioId);
        Assert.Equal(3, TotalCompleted(result.BaselineMetrics));
        Assert.Equal(3, TotalCompleted(result.CandidateMetrics));
        Assert.True(result.BaselineMetrics.FinishedAtMs > 0);
        Assert.True(result.CandidateMetrics.FinishedAtMs > 0);
        Assert.NotEmpty(result.Deltas);
    }

    [Fact]
    public void WarehouseScenarioComparisonRunner_CompareWithUnifiedAdapter_DiffersFromLegacyTimingButKeepsCounts()
    {
        var baseline = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());
        var candidate = WarehouseScenarioJsonLoader.Load(CandidateScenarioPath());
        var runner = new WarehouseScenarioComparisonRunner();

        var legacy = runner.Compare(baseline, candidate);
        var unified = runner.CompareWithUnifiedAdapter(baseline, candidate);

        Assert.NotEqual(
            legacy.BaselineMetrics.FinishedAtMs,
            unified.BaselineMetrics.FinishedAtMs);
        Assert.NotEqual(
            legacy.CandidateMetrics.FinishedAtMs,
            unified.CandidateMetrics.FinishedAtMs);
        Assert.NotEqual(
            legacy.BaselineMetrics.TotalWorkItemThroughputPerHour,
            unified.BaselineMetrics.TotalWorkItemThroughputPerHour);
        Assert.Equal(
            TotalCompleted(legacy.BaselineMetrics),
            TotalCompleted(unified.BaselineMetrics));
        Assert.Equal(
            TotalCompleted(legacy.CandidateMetrics),
            TotalCompleted(unified.CandidateMetrics));
        Assert.Equal(
            legacy.BaselineMetrics.TotalQuantityReceived,
            unified.BaselineMetrics.TotalQuantityReceived);
        Assert.Equal(
            legacy.CandidateMetrics.TotalQuantityPicked,
            unified.CandidateMetrics.TotalQuantityPicked);
    }

    private static WarehouseScenarioComparisonDelta Delta(
        WarehouseScenarioComparisonResult result,
        string metricName)
    {
        return Assert.Single(
            result.Deltas,
            delta => delta.MetricName == metricName);
    }

    private static int TotalCompleted(WarehouseScenarioComparisonMetrics metrics)
    {
        return metrics.CompletedReceipts +
               metrics.CompletedOutboundOrders +
               metrics.CompletedEachPickOrders;
    }

    private static string SampleScenarioPath()
    {
        return DatasetPath("scenario.json");
    }

    private static string CandidateScenarioPath()
    {
        return DatasetPath("scenario-candidate.json");
    }

    private static string DatasetPath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "datasets",
                "sample-small-warehouse",
                fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Cannot find datasets/sample-small-warehouse/{fileName} from test output directory.");
    }

    private static WarehouseScenario Scenario(
        string scenarioId,
        long unloadDurationMs)
    {
        return new WarehouseScenario(
            scenarioId,
            seed: 1,
            inboundScenario: new InboundScenario(
                $"{scenarioId}.inbound",
                seed: 11,
                [
                    new InboundReceipt(
                        "receipt-1",
                        "warehouse-1",
                        "sku-1",
                        5m,
                        "stage-1",
                        "reserve-1",
                        arrivesAtMs: 0),
                ],
                new InboundProcessParameters(
                    unloadDurationMs,
                    putawayTravelDurationMs: 0,
                    putawayServiceDurationMs: 0),
                dockCount: 1,
                forkliftCount: 1),
            outboundScenario: null,
            eachPickScenario: null);
    }
}
