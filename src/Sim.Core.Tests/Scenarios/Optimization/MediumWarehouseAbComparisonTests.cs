using System.Diagnostics;
using System.Text.Json;
using Sim.Contracts.Artifacts;
using Sim.Core.Scenarios.Optimization;
using Sim.Report;
using Xunit;

namespace Sim.Core.Tests.Scenarios.Optimization;

public sealed class MediumWarehouseAbComparisonTests
{
    [Fact]
    public void AbComparison_OptimizedScenario_AppliesAbcSlottingAssignments()
    {
        var optimizedScenarioJson = MediumWarehouseAbComparison.GenerateOptimizedScenarioJson(
            File.ReadAllText(MediumScenarioPath()),
            File.ReadAllText(OptimizedConfigPath()));

        using var document = JsonDocument.Parse(optimizedScenarioJson);
        var root = document.RootElement;

        Assert.Equal(
            MediumWarehouseAbComparison.OptimizedScenarioId,
            root.GetProperty("scenario_id").GetString());
        Assert.Contains(
            "not globally optimal",
            root.GetProperty("description").GetString(),
            StringComparison.Ordinal);
        Assert.Contains(
            "deterministic modeled simulation artifacts",
            root.GetProperty("description").GetString(),
            StringComparison.Ordinal);

        var outboundOrder = root.GetProperty("outbound")
            .GetProperty("orders")
            .EnumerateArray()
            .First(order => String(order, "sku_id") == "SKU-HF-007");
        var eachPickOrder = root.GetProperty("each_pick")
            .GetProperty("orders")
            .EnumerateArray()
            .First(order => String(order, "sku_id") == "SKU-HF-007");

        Assert.Equal("pick-a-07", String(outboundOrder, "pick_location_id"));
        Assert.Equal("pick-a-07", String(eachPickOrder, "pick_face_location_id"));
    }

    [Fact]
    public void ImprovementPct_DirectionRules_AreStable()
    {
        Assert.Equal(
            20m,
            MediumWarehouseAbComparison.CalculateImprovementPct(
                baselineValue: 100m,
                optimizedValue: 80m,
                lowerIsBetter: true));
        Assert.Equal(
            -20m,
            MediumWarehouseAbComparison.CalculateImprovementPct(
                baselineValue: 100m,
                optimizedValue: 120m,
                lowerIsBetter: true));
        Assert.Equal(
            20m,
            MediumWarehouseAbComparison.CalculateImprovementPct(
                baselineValue: 100m,
                optimizedValue: 120m,
                lowerIsBetter: false));
        Assert.Equal(
            -20m,
            MediumWarehouseAbComparison.CalculateImprovementPct(
                baselineValue: 100m,
                optimizedValue: 80m,
                lowerIsBetter: false));
    }

    [Fact]
    public void ImprovementPct_BaselineZero_DoesNotEmitNanOrInfinity()
    {
        Assert.Equal(
            0m,
            MediumWarehouseAbComparison.CalculateImprovementPct(
                baselineValue: 0m,
                optimizedValue: 0m,
                lowerIsBetter: true));
        Assert.Null(
            MediumWarehouseAbComparison.CalculateImprovementPct(
                baselineValue: 0m,
                optimizedValue: 10m,
                lowerIsBetter: false));

        var improvement = MediumWarehouseAbComparison.BuildImprovementPct(
            RunArtifactKpiJson(totalCompletedWorkItems: 0),
            RunArtifactKpiJson(totalCompletedWorkItems: 10));

        Assert.DoesNotContain("total_completed_work_items", improvement.Keys);
    }

    [Fact]
    public void KpiDelta_FromRunArtifacts_UsesLowerAndHigherDirectionMetadata()
    {
        var deltas = MediumWarehouseAbComparison.BuildKpiDeltas(
            RunArtifactKpiJson(
                orderCycleP50Ms: 100,
                avgWaitMs: 200,
                totalCompletedWorkItems: 10,
                throughputPerHour: 20),
            RunArtifactKpiJson(
                orderCycleP50Ms: 80,
                avgWaitMs: 240,
                totalCompletedWorkItems: 12,
                throughputPerHour: 30));
        var improvement = MediumWarehouseAbComparison.BuildImprovementPct(
            RunArtifactKpiJson(
                orderCycleP50Ms: 100,
                avgWaitMs: 200,
                totalCompletedWorkItems: 10,
                throughputPerHour: 20),
            RunArtifactKpiJson(
                orderCycleP50Ms: 80,
                avgWaitMs: 240,
                totalCompletedWorkItems: 12,
                throughputPerHour: 30));

        Assert.True(deltas["order_cycle_p50_ms"].LowerIsBetter);
        Assert.False(deltas["total_work_item_throughput_per_hour"].LowerIsBetter);
        Assert.Equal(-20m, deltas["order_cycle_p50_ms"].Delta);
        Assert.Equal(20m, improvement["order_cycle_p50_ms"]);
        Assert.Equal(-20m, improvement["avg_wait_ms"]);
        Assert.Equal(20m, improvement["total_completed_work_items"]);
        Assert.Equal(50m, improvement["total_work_item_throughput_per_hour"]);
    }

    [Fact]
    public void ComparisonArtifact_CommittedMediumAbArtifact_DeserializesWithA5bFields()
    {
        var artifact = ComparisonArtifactLoader.Load(ComparisonArtifactPath());
        var comparisonText = File.ReadAllText(ComparisonArtifactPath());

        Assert.Equal(ComparisonArtifact.CurrentSchemaVersion, artifact.SchemaVersion);
        Assert.Equal("medium-warehouse", artifact.Baseline.ScenarioId);
        Assert.Equal(
            MediumWarehouseAbComparison.OptimizedScenarioId,
            artifact.Candidate.ScenarioId);
        Assert.NotNull(artifact.KpiDeltas);
        Assert.NotNull(artifact.ImprovementPct);
        Assert.Contains("order_cycle_p50_ms", artifact.KpiDeltas!.Keys);
        Assert.Contains("total_work_item_throughput_per_hour", artifact.ImprovementPct!.Keys);

        using var document = JsonDocument.Parse(comparisonText);
        Assert.Equal("medium-warehouse", String(document.RootElement, "baseline_run_id"));
        Assert.Equal(
            MediumWarehouseAbComparison.OptimizedScenarioId,
            String(document.RootElement, "optimized_run_id"));
        Assert.Contains(
            "not globally optimal",
            String(document.RootElement, "optimization_note"),
            StringComparison.Ordinal);
        Assert.Equal("deterministic_modeled", String(document.RootElement, "evidence_level"));
    }

    [Fact]
    public void AbComparison_CliExport_IsDeterministicAndMatchesCommittedFiles()
    {
        using var first = TempDirectory.Create();
        using var second = TempDirectory.Create();

        Assert.Equal(0, RunCli(OutputDir(first.Path)).ExitCode);
        Assert.Equal(0, RunCli(OutputDir(second.Path)).ExitCode);

        foreach (var relativePath in new[]
        {
            "scenario.json",
            Path.Combine("artifacts", "run-artifact.v1.json"),
            Path.Combine("artifacts", "movement-artifact.v1.json"),
            Path.Combine("artifacts", "comparison-artifact.v1.json"),
        })
        {
            var firstPath = Path.Combine(OutputDir(first.Path), relativePath);
            var secondPath = Path.Combine(OutputDir(second.Path), relativePath);
            var committedPath = Path.Combine(OptimizedDir(), relativePath);

            Assert.Equal(File.ReadAllBytes(firstPath), File.ReadAllBytes(secondPath));
            Assert.Equal(File.ReadAllBytes(committedPath), File.ReadAllBytes(firstPath));
        }
    }

    private static CliResult RunCli(string outputDir)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = RepoRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(CliProjectPath());
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("export-medium-ab-comparison");
        startInfo.ArgumentList.Add(MediumScenarioPath());
        startInfo.ArgumentList.Add(OptimizedConfigPath());
        startInfo.ArgumentList.Add("--baseline-run-artifact");
        startInfo.ArgumentList.Add(BaselineRunArtifactPath());
        startInfo.ArgumentList.Add("--output-dir");
        startInfo.ArgumentList.Add(outputDir);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Sim.Cli process.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Sim.Cli A5b export did not exit within 60 seconds.");
        }

        return new CliResult(
            process.ExitCode,
            standardOutputTask.GetAwaiter().GetResult(),
            standardErrorTask.GetAwaiter().GetResult());
    }

    private static string RunArtifactKpiJson(
        decimal orderCycleP50Ms = 100,
        decimal avgWaitMs = 100,
        decimal totalCompletedWorkItems = 1,
        decimal throughputPerHour = 1)
    {
        return $$"""
        {
          "kpi_summary": {
            "order_cycle_p50_ms": {{orderCycleP50Ms}},
            "order_cycle_p90_ms": 100,
            "order_cycle_p95_ms": 100,
            "avg_wait_ms": {{avgWaitMs}},
            "total_duration_ms": 100,
            "total_completed_work_items": {{totalCompletedWorkItems}},
            "outbound_order_throughput_per_hour": 1,
            "each_pick_order_throughput_per_hour": 1,
            "total_work_item_throughput_per_hour": {{throughputPerHour}}
          }
        }
        """;
    }

    private static string String(JsonElement element, string propertyName)
        => element.GetProperty(propertyName).GetString()!;

    private static string MediumScenarioPath()
        => Path.Combine(RepoRoot(), "datasets", "medium-warehouse", "scenario.json");

    private static string BaselineRunArtifactPath()
        => Path.Combine(RepoRoot(), "datasets", "medium-warehouse", "artifacts", "run-artifact.v1.json");

    private static string OptimizedDir()
        => Path.Combine(RepoRoot(), "datasets", "medium-warehouse", "optimized");

    private static string OptimizedConfigPath()
        => Path.Combine(OptimizedDir(), "abc-slotting-config.json");

    private static string ComparisonArtifactPath()
        => Path.Combine(OptimizedDir(), "artifacts", "comparison-artifact.v1.json");

    private static string OutputDir(string tempPath)
        => Path.Combine(tempPath, "optimized");

    private static string CliProjectPath()
        => Path.Combine(RepoRoot(), "src", "Sim.Cli", "Sim.Cli.csproj");

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

    private sealed record CliResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"awt-a5b-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return new TempDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
