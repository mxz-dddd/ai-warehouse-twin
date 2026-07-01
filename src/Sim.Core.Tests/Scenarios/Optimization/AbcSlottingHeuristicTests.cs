using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sim.Core.Scenarios.Optimization;
using Xunit;

namespace Sim.Core.Tests.Scenarios.Optimization;

public sealed class AbcSlottingHeuristicTests
{
    private const string BaselineScenarioHash =
        "7b540d3772badc81c369bcb24d915a21c53a3e6b0cff5a47924a74e67e8fe4bb";
    private const string RunArtifactGoldenHash =
        "8ebff74a1a214a8d53590b69ad7be832c643333beb5afd88c3edc3f14295258d";
    private const string MovementArtifactGoldenHash =
        "ca1b15a4cdc413a2c003b84b57453dce0d9445b27cda07ed6a199720d671f0c3";

    [Fact]
    public void AbcSlotting_MediumWarehouse_RanksHighFrequencySkusByDemand()
    {
        var config = GenerateConfig();

        Assert.Equal(
            Enumerable.Range(1, 10).Select(index => $"SKU-HF-{index:000}").ToArray(),
            config.SkuRanking.Take(10).Select(entry => entry.SkuId).ToArray());
        Assert.All(config.SkuRanking.Take(10), entry =>
        {
            Assert.Equal(90m, entry.TotalOrderedQuantity);
            Assert.Equal(6, entry.OrderLineCount);
            Assert.Equal("high", entry.VelocityClass);
        });
        Assert.Equal("SKU-MF-011", config.SkuRanking[10].SkuId);
        Assert.Equal(16m, config.SkuRanking[10].TotalOrderedQuantity);
        Assert.Equal(2, config.SkuRanking[10].OrderLineCount);
    }

    [Fact]
    public void Optimization_MediumWarehouse_LocationRankingUsesAZonePickPathAnchor()
    {
        var config = GenerateConfig();

        Assert.Equal(AbcSlottingHeuristic.DefaultAnchorNodeId, config.Anchor.NodeId);
        Assert.Equal("pick_path", config.Anchor.AnchorType);
        Assert.Contains("zone-b-pick", config.Anchor.SelectionReason);
        Assert.All(config.LocationRanking.Take(8), entry =>
        {
            Assert.StartsWith("pick-a-", entry.LocationId, StringComparison.Ordinal);
            Assert.Equal("aisle-a-main", entry.NodeId);
        });
        Assert.True(config.LocationRanking[0].DistanceToAnchorM <= config.LocationRanking[^1].DistanceToAnchorM);
    }

    [Fact]
    public void OptimizedScenario_AbcSlottingConfig_IsDeterministicAndMatchesCommittedFile()
    {
        var scenarioJson = File.ReadAllText(MediumScenarioPath());

        var first = AbcSlottingHeuristic.GenerateConfigJson(scenarioJson);
        var second = AbcSlottingHeuristic.GenerateConfigJson(scenarioJson);
        var committed = File.ReadAllText(OptimizedConfigPath());

        Assert.Equal(first, second);
        Assert.Equal(committed, first);
    }

    [Fact]
    public void OptimizedScenario_AbcSlottingConfig_HasNoDanglingReferences()
    {
        using var scenario = JsonDocument.Parse(File.ReadAllText(MediumScenarioPath()));
        var root = scenario.RootElement;
        var skuIds = root.GetProperty("sku_master")
            .EnumerateArray()
            .Select(item => String(item, "sku_id"))
            .ToHashSet(StringComparer.Ordinal);
        var locationIds = root.GetProperty("layout")
            .GetProperty("locations")
            .EnumerateArray()
            .Select(item => String(item, "id"))
            .ToHashSet(StringComparer.Ordinal);
        var nodeIds = root.GetProperty("layout")
            .GetProperty("path_nodes")
            .EnumerateArray()
            .Select(item => String(item, "node_id"))
            .ToHashSet(StringComparer.Ordinal);
        var config = GenerateConfig();

        Assert.Equal("medium-warehouse", config.SourceScenario.ScenarioId);
        Assert.Equal(AbcSlottingHeuristic.DefaultBaselineScenarioPath, config.SourceScenario.Path);
        Assert.Equal(BaselineScenarioHash, config.SourceScenario.Sha256);

        foreach (var sku in config.SkuRanking)
        {
            Assert.Contains(sku.SkuId, skuIds);
            Assert.Contains(sku.CurrentLocationId, locationIds);
        }

        foreach (var location in config.LocationRanking)
        {
            Assert.Contains(location.LocationId, locationIds);
            Assert.Contains(location.NodeId, nodeIds);
        }

        foreach (var assignment in config.Assignments)
        {
            Assert.Contains(assignment.SkuId, skuIds);
            Assert.Contains(assignment.FromLocationId, locationIds);
            Assert.Contains(assignment.TargetLocationId, locationIds);
            Assert.Contains(assignment.TargetNodeId, nodeIds);
        }
    }

    [Fact]
    public void AbcSlotting_HighFrequencyAssignmentsAreCloserThanLowerFrequencyAssignments()
    {
        var config = GenerateConfig();

        var highFrequencyDistance = config.Assignments
            .Where(assignment => string.CompareOrdinal(assignment.SkuId, "SKU-HF-011") < 0)
            .Average(assignment => assignment.TargetDistanceToAnchorM);
        var lowerFrequencyDistance = config.Assignments
            .Where(assignment => assignment.SkuId.StartsWith("SKU-MF-", StringComparison.Ordinal))
            .Average(assignment => assignment.TargetDistanceToAnchorM);

        Assert.True(highFrequencyDistance < lowerFrequencyDistance);
    }

    [Fact]
    public void OptimizedScenario_DoesNotGenerateComparisonArtifactOrImprovementClaims()
    {
        var configText = File.ReadAllText(OptimizedConfigPath());

        Assert.DoesNotContain("ComparisonArtifact", configText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("comparison-artifact", configText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("improvement_pct", configText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("baseline_vs_optimized", configText, StringComparison.OrdinalIgnoreCase);
        Assert.True(Directory.Exists(OptimizedDir()));
        Assert.Empty(Directory.EnumerateFiles(OptimizedDir(), "*comparison*", SearchOption.AllDirectories));
    }

    [Fact]
    public void MediumWarehouse_BaselineScenarioAndGoldenArtifactsRemainUnchanged()
    {
        Assert.Equal(BaselineScenarioHash, Sha256(File.ReadAllText(MediumScenarioPath())));
        Assert.Equal(RunArtifactGoldenHash, Sha256(File.ReadAllText(RunArtifactGoldenPath())));
        Assert.Equal(MovementArtifactGoldenHash, Sha256(File.ReadAllText(MovementArtifactGoldenPath())));
    }

    private static AbcSlottingConfig GenerateConfig()
        => AbcSlottingHeuristic.GenerateConfig(File.ReadAllText(MediumScenarioPath()));

    private static string String(JsonElement element, string propertyName)
        => element.GetProperty(propertyName).GetString()!;

    private static string Sha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string MediumScenarioPath()
        => Path.Combine(RepoRoot(), "datasets", "medium-warehouse", "scenario.json");

    private static string RunArtifactGoldenPath()
        => Path.Combine(RepoRoot(), "datasets", "medium-warehouse", "artifacts", "run-artifact.v1.json");

    private static string MovementArtifactGoldenPath()
        => Path.Combine(RepoRoot(), "datasets", "medium-warehouse", "artifacts", "movement-artifact.v1.json");

    private static string OptimizedDir()
        => Path.Combine(RepoRoot(), "datasets", "medium-warehouse", "optimized");

    private static string OptimizedConfigPath()
        => Path.Combine(OptimizedDir(), "abc-slotting-config.json");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "WarehouseTwin.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (WarehouseTwin.sln).");
    }
}
