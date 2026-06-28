using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Json;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseScenarioTraceTests
{
    [Fact]
    public void RunWithTrace_FromSampleScenario_ProducesActualResourceLeaseTimeline()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        var result = new WarehouseScenarioRunner().RunWithTrace(scenario);

        Assert.NotNull(result.RunResult);
        Assert.NotEmpty(result.ResourceLeaseTimeline);
        Assert.All(
            result.ResourceLeaseTimeline,
            entry =>
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.OperationId));
                Assert.False(string.IsNullOrWhiteSpace(entry.OperationType));
                Assert.False(string.IsNullOrWhiteSpace(entry.StageType));
                Assert.False(string.IsNullOrWhiteSpace(entry.ResourceId));
                Assert.True(entry.StartedAtMs >= entry.RequestedAtMs);
                Assert.True(entry.FinishedAtMs > entry.StartedAtMs);
                Assert.Equal(
                    entry.FinishedAtMs - entry.StartedAtMs,
                    entry.DurationMs);
            });
    }

    [Fact]
    public void RunWithTrace_ResourceLeaseTimeline_IsDeterministic()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());
        var runner = new WarehouseScenarioRunner();

        var first = runner.RunWithTrace(scenario);
        var second = runner.RunWithTrace(scenario);

        Assert.Equal(
            first.ResourceLeaseTimeline.ToArray(),
            second.ResourceLeaseTimeline.ToArray());
        Assert.Equal(first.RunResult.EventLogText, second.RunResult.EventLogText);
        Assert.Equal(first.RunResult.KpiSummary, second.RunResult.KpiSummary);
    }

    [Fact]
    public void RunWithTrace_CapturesInboundAndOutboundMultiStageLeases()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        var timeline = new WarehouseScenarioRunner()
            .RunWithTrace(scenario)
            .ResourceLeaseTimeline;

        Assert.Equal(
            ["dock", "forklift"],
            timeline
                .Where(entry =>
                    entry.OperationType == "inbound" &&
                    entry.OperationId == "receipt-1")
                .Select(entry => entry.StageType)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            ["dock", "worker"],
            timeline
                .Where(entry =>
                    entry.OperationType == "outbound" &&
                    entry.OperationId == "order-1")
                .Select(entry => entry.StageType)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void RunWithTrace_DoesNotFabricateEachPickLeases()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        var timeline = new WarehouseScenarioRunner()
            .RunWithTrace(scenario)
            .ResourceLeaseTimeline;

        Assert.DoesNotContain(
            timeline,
            entry => entry.OperationType == "each_pick");
    }

    [Fact]
    public void Run_DoesNotChangeTraditionalBehavior()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());
        var runner = new WarehouseScenarioRunner();

        var traditional = runner.Run(scenario);
        var traced = runner.RunWithTrace(scenario).RunResult;

        Assert.Equal(traditional.ScenarioId, traced.ScenarioId);
        Assert.Equal(traditional.Seed, traced.Seed);
        Assert.Equal(traditional.StartedAtMs, traced.StartedAtMs);
        Assert.Equal(traditional.FinishedAtMs, traced.FinishedAtMs);
        Assert.Equal(traditional.CompletedReceipts, traced.CompletedReceipts);
        Assert.Equal(
            traditional.CompletedOutboundOrders,
            traced.CompletedOutboundOrders);
        Assert.Equal(
            traditional.CompletedEachPickOrders,
            traced.CompletedEachPickOrders);
        Assert.Equal(traditional.EventLogText, traced.EventLogText);
        Assert.Equal(traditional.KpiSummary, traced.KpiSummary);
    }

    private static string SampleScenarioPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "datasets",
                "sample-small-warehouse",
                "scenario.json");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Cannot find datasets/sample-small-warehouse/scenario.json from test output directory.");
    }
}
