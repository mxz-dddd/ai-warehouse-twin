using Sim.Core.Processes.Inbound;
using Sim.Core.Scenarios;
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

    private static WarehouseScenarioComparisonDelta Delta(
        WarehouseScenarioComparisonResult result,
        string metricName)
    {
        return Assert.Single(
            result.Deltas,
            delta => delta.MetricName == metricName);
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
