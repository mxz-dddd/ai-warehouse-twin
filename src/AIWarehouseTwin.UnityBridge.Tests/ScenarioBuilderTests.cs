using AIWarehouseTwin.Simulation;
using Xunit;

namespace AIWarehouseTwin.UnityBridge.Tests;

public sealed class ScenarioBuilderTests
{
    [Fact]
    public void Build_returns_run_artifact_for_default_config()
    {
        var artifact = ScenarioBuilder.Build(new WarehouseConfig());

        Assert.NotNull(artifact);
        Assert.Equal("run-artifact.v1", artifact.SchemaVersion);
        Assert.Equal("warehouse-simulation-run", artifact.ArtifactKind);
        Assert.Equal("unity-demo-40x20-r3-sku200-w5-f2-o50", artifact.ScenarioId);
        Assert.Equal(50, artifact.KpiSummary.TotalCompletedWorkItems);
        Assert.Equal(7, artifact.Layout.Resources.Count);
    }

    [Fact]
    public void Build_is_deterministic_for_same_config()
    {
        var cfg = new WarehouseConfig();

        var first = ScenarioBuilder.Build(cfg);
        var second = ScenarioBuilder.Build(cfg);

        Assert.Equal(first.ScenarioId, second.ScenarioId);
        Assert.Equal(first.FinishedAtMs, second.FinishedAtMs);
        Assert.Equal(first.KpiSummary, second.KpiSummary);
        Assert.Equal(first.EventLog, second.EventLog);
        Assert.Equal(first.Layout.Resources, second.Layout.Resources);
    }

    [Fact]
    public void Build_rejects_null_config()
    {
        WarehouseConfig? cfg = null;

        Assert.Throws<ArgumentNullException>(() => ScenarioBuilder.Build(cfg!));
    }

    public static TheoryData<string, Action<WarehouseConfig>> InvalidConfigs => new()
    {
        { "lengthM", cfg => cfg.lengthM = 0f },
        { "widthM", cfg => cfg.widthM = 0f },
        { "shelfRows", cfg => cfg.shelfRows = 0 },
        { "skuCount", cfg => cfg.skuCount = 0 },
        { "workerCount", cfg => cfg.workerCount = 0 },
        { "forkliftCount", cfg => cfg.forkliftCount = -1 },
        { "orderCount", cfg => cfg.orderCount = 0 },
    };

    [Theory]
    [MemberData(nameof(InvalidConfigs))]
    public void Build_rejects_invalid_config_values(
        string expectedParamName,
        Action<WarehouseConfig> mutate)
    {
        var cfg = new WarehouseConfig();
        mutate(cfg);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ScenarioBuilder.Build(cfg));

        Assert.Equal(expectedParamName, ex.ParamName);
    }
}
