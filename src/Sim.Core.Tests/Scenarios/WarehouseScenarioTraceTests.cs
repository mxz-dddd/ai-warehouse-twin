using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Json;
using Sim.Core.Resources;
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
    public void RunWithTrace_CapturesEachPickResourceLeases()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        var timeline = new WarehouseScenarioRunner()
            .RunWithTrace(scenario)
            .ResourceLeaseTimeline;

        var eachPickEntries = timeline
            .Where(entry => entry.OperationType == "each_pick")
            .ToArray();

        Assert.NotEmpty(eachPickEntries);
        Assert.Contains(eachPickEntries, entry => entry.StageType == "station");
        Assert.Contains(eachPickEntries, entry => entry.StageType == "worker");
        Assert.All(
            eachPickEntries,
            entry =>
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.OperationId));
                Assert.False(string.IsNullOrWhiteSpace(entry.ResourceId));
                Assert.True(entry.StartedAtMs >= entry.RequestedAtMs);
                Assert.True(entry.FinishedAtMs > entry.StartedAtMs);
                Assert.Equal(
                    entry.FinishedAtMs - entry.StartedAtMs,
                    entry.DurationMs);
            });
    }

    [Fact]
    public void RunWithTrace_CapturesInboundOutboundAndEachPickLeases()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        var timeline = new WarehouseScenarioRunner()
            .RunWithTrace(scenario)
            .ResourceLeaseTimeline;

        AssertStages(timeline, "inbound", "dock", "forklift");
        AssertStages(timeline, "outbound", "dock", "worker");
        AssertStages(timeline, "each_pick", "station", "worker");
    }

    [Fact]
    public void RunWithTrace_EachPickLeaseTimeline_IsDeterministic()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());
        var runner = new WarehouseScenarioRunner();

        var first = runner.RunWithTrace(scenario).ResourceLeaseTimeline
            .Where(entry => entry.OperationType == "each_pick")
            .ToArray();
        var second = runner.RunWithTrace(scenario).ResourceLeaseTimeline
            .Where(entry => entry.OperationType == "each_pick")
            .ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void RunWithTrace_PositionTimeline_ContainsStartAndFinishForEveryLease()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        var result = new WarehouseScenarioRunner().RunWithTrace(scenario);

        Assert.NotEmpty(result.ResourceLeaseTimeline);
        Assert.Equal(
            result.ResourceLeaseTimeline.Count * 2,
            result.PositionTimeline.Count);

        foreach (var lease in result.ResourceLeaseTimeline)
        {
            var start = Assert.Single(
                result.PositionTimeline,
                entry =>
                    MatchesLease(entry, lease) &&
                    entry.EventType == "start");
            var finish = Assert.Single(
                result.PositionTimeline,
                entry =>
                    MatchesLease(entry, lease) &&
                    entry.EventType == "finish");

            Assert.Equal(lease.StartedAtMs, start.AtMs);
            Assert.Equal(lease.FinishedAtMs, finish.AtMs);
            Assert.False(string.IsNullOrWhiteSpace(start.Position.NodeId));
            Assert.False(string.IsNullOrWhiteSpace(finish.Position.NodeId));
        }
    }

    [Fact]
    public void RunWithTrace_PositionTimeline_IncludesInboundOutboundAndEachPick()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        var timeline = new WarehouseScenarioRunner()
            .RunWithTrace(scenario)
            .PositionTimeline;

        AssertStages(timeline, "inbound", "dock", "forklift");
        AssertStages(timeline, "outbound", "dock", "worker");
        AssertStages(timeline, "each_pick", "station", "worker");
    }

    [Fact]
    public void RunWithTrace_PositionTimeline_IsDeterministic()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());
        var runner = new WarehouseScenarioRunner();

        var first = runner.RunWithTrace(scenario);
        var second = runner.RunWithTrace(scenario);

        Assert.Equal(
            first.PositionTimeline.ToArray(),
            second.PositionTimeline.ToArray());
        Assert.Equal(
            first.Layout.Resources.ToArray(),
            second.Layout.Resources.ToArray());
    }

    [Fact]
    public void RunWithTrace_DefaultLayout_IsResourceIdSorted()
    {
        var scenario = WarehouseScenarioJsonLoader.Load(SampleScenarioPath());

        var resources = new WarehouseScenarioRunner()
            .RunWithTrace(scenario)
            .Layout
            .Resources
            .ToArray();

        Assert.Equal(
            resources.Select(resource => resource.ResourceId)
                .Order(StringComparer.Ordinal),
            resources.Select(resource => resource.ResourceId));

        for (var index = 0; index < resources.Length; index++)
        {
            Assert.Equal(resources[index].ResourceId, resources[index].Position.NodeId);
            Assert.Equal(index, resources[index].Position.X);
            Assert.Equal(0m, resources[index].Position.Y);
        }
    }

    [Fact]
    public void Run_DoesNotChangeTraditionalBehavior_WithPositionTimeline()
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

    private static void AssertStages(
        IEnumerable<Sim.Core.Resources.ResourceLeaseTimelineEntry> timeline,
        string operationType,
        params string[] expectedStages)
    {
        Assert.Equal(
            expectedStages.Order(StringComparer.Ordinal),
            timeline
                .Where(entry => entry.OperationType == operationType)
                .Select(entry => entry.StageType)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
    }

    private static void AssertStages(
        IEnumerable<WarehouseScenarioPositionTimelineEntry> timeline,
        string operationType,
        params string[] expectedStages)
    {
        Assert.Equal(
            expectedStages.Order(StringComparer.Ordinal),
            timeline
                .Where(entry => entry.OperationType == operationType)
                .Select(entry => entry.StageType)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
    }

    private static bool MatchesLease(
        WarehouseScenarioPositionTimelineEntry entry,
        ResourceLeaseTimelineEntry lease)
    {
        return entry.OperationId == lease.OperationId &&
               entry.OperationType == lease.OperationType &&
               entry.StageType == lease.StageType &&
               entry.ResourceId == lease.ResourceId;
    }
}
