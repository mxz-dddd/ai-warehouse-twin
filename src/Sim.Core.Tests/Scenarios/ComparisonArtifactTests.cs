using System.Text.Json;
using Sim.Contracts.Artifacts;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class ComparisonArtifactTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void ComparisonArtifact_MapsScenarioIdsAndMetrics()
    {
        var artifact = LoadGolden();

        Assert.Equal(
            ComparisonArtifact.CurrentSchemaVersion,
            artifact.SchemaVersion);
        Assert.Equal(
            "sample-small-warehouse",
            artifact.Baseline.ScenarioId);
        Assert.Equal(
            "sample-small-warehouse-candidate",
            artifact.Candidate.ScenarioId);
        Assert.Equal(410, artifact.Baseline.Metrics.FinishedAtMs);
        Assert.Equal(360, artifact.Candidate.Metrics.FinishedAtMs);
        Assert.Equal(3, artifact.Baseline.Metrics.CompletedReceipts +
            artifact.Baseline.Metrics.CompletedOutboundOrders +
            artifact.Baseline.Metrics.CompletedEachPickOrders);
        Assert.Equal(3, artifact.Candidate.Metrics.CompletedReceipts +
            artifact.Candidate.Metrics.CompletedOutboundOrders +
            artifact.Candidate.Metrics.CompletedEachPickOrders);
    }

    [Fact]
    public void ComparisonArtifact_MapsDeltasDeterministically()
    {
        var deltas = LoadGolden().Deltas;

        Assert.Equal(
            [
                "finished_at_ms",
                "completed_receipts",
                "completed_outbound_orders",
                "completed_each_pick_orders",
                "total_quantity_received",
                "total_quantity_shipped",
                "total_quantity_picked",
                "inbound_receipt_throughput_per_hour",
                "outbound_order_throughput_per_hour",
                "each_pick_order_throughput_per_hour",
                "total_work_item_throughput_per_hour",
            ],
            deltas.Select(delta => delta.MetricName));
    }

    [Fact]
    public void ComparisonArtifact_MapsFinishedAtDelta()
    {
        var delta = Delta(LoadGolden(), "finished_at_ms");

        Assert.Equal(410m, delta.BaselineValue);
        Assert.Equal(360m, delta.CandidateValue);
        Assert.Equal(-50m, delta.Delta);
        Assert.Equal(-12.195m, delta.DeltaPercent);
        Assert.Equal("decrease", delta.Direction);
    }

    [Fact]
    public void ComparisonArtifact_MapsThroughputDelta()
    {
        var delta = Delta(
            LoadGolden(),
            "total_work_item_throughput_per_hour");

        Assert.Equal(27_000.000m, delta.BaselineValue);
        Assert.Equal(30_857.143m, delta.CandidateValue);
        Assert.Equal(3_857.143m, delta.Delta);
        Assert.Equal(14.286m, delta.DeltaPercent);
        Assert.Equal("increase", delta.Direction);
    }

    [Fact]
    public void ComparisonArtifact_RoundTripsLayoutFreeComparisonContract()
    {
        var artifact = LoadGolden();

        var json = JsonSerializer.Serialize(artifact, Options);
        var roundTripped = JsonSerializer.Deserialize<ComparisonArtifact>(
            json,
            Options);

        Assert.NotNull(roundTripped);
        Assert.Equal(artifact.SchemaVersion, roundTripped.SchemaVersion);
        Assert.Equal(artifact.Baseline, roundTripped.Baseline);
        Assert.Equal(artifact.Candidate, roundTripped.Candidate);
        Assert.Equal(artifact.Deltas.ToArray(), roundTripped.Deltas.ToArray());
    }

    private static ComparisonArtifact LoadGolden()
    {
        var artifact = JsonSerializer.Deserialize<ComparisonArtifact>(
            File.ReadAllText(ComparisonArtifactPath()),
            Options);

        Assert.NotNull(artifact);
        return artifact;
    }

    private static ComparisonArtifactDelta Delta(
        ComparisonArtifact artifact,
        string metricName)
    {
        return Assert.Single(
            artifact.Deltas,
            delta => delta.MetricName == metricName);
    }

    private static string ComparisonArtifactPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "datasets",
                "sample-small-warehouse",
                "artifacts",
                "comparison-artifact.v1.json");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Cannot find datasets/sample-small-warehouse/artifacts/comparison-artifact.v1.json from test output directory.");
    }
}
