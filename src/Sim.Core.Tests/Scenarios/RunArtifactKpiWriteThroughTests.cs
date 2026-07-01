using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class RunArtifactKpiWriteThroughTests
{
    [Fact]
    public void ExportArtifact_UnifiedRunner_WritesRichKpiSummaryFromA3aResult()
    {
        using var temp = TempDirectory.Create();
        var scenarioPath = Path.Combine(temp.Path, "scenario.json");
        var artifactPath = Path.Combine(temp.Path, "run-artifact.v1.json");
        File.WriteAllText(scenarioPath, RichKpiScenarioJson());

        var result = RunCli(
            "export-artifact",
            scenarioPath,
            "-o",
            artifactPath);

        Assert.Equal(0, result.ExitCode);

        using var document = JsonDocument.Parse(File.ReadAllText(artifactPath));
        var root = document.RootElement;
        var kpi = root.GetProperty("kpi_summary");

        Assert.Equal(30m, Decimal(kpi, "order_cycle_p50_ms"));
        Assert.Equal(50m, Decimal(kpi, "order_cycle_p90_ms"));
        Assert.Equal(50m, Decimal(kpi, "order_cycle_p95_ms"));
        Assert.Equal(3.333m, Decimal(kpi, "avg_wait_ms"));

        var utilization = kpi.GetProperty("resource_utilization");
        Assert.Equal(["dock-1", "station-1"], utilization.EnumerateObject().Select(item => item.Name));
        Assert.Equal(60m, Decimal(utilization, "dock-1"));
        Assert.Equal(100m, Decimal(utilization, "station-1"));

        var bottlenecks = kpi.GetProperty("bottlenecks").EnumerateArray().ToArray();
        Assert.Equal(["station-1", "dock-1"], bottlenecks.Select(item => item.GetProperty("resource_id").GetString()));
        Assert.Equal(1, bottlenecks[0].GetProperty("rank").GetInt32());
        Assert.Equal("unknown", bottlenecks[0].GetProperty("resource_type").GetString());
        Assert.Equal(0m, Decimal(bottlenecks[0], "avg_wait_ms"));
        Assert.Equal(0m, Decimal(bottlenecks[0], "total_wait_ms"));
        Assert.Equal(100m, Decimal(bottlenecks[0], "utilization"));
        Assert.Equal(60m, Decimal(bottlenecks[1], "utilization"));

        Assert.Empty(kpi.GetProperty("travel_distance_m_by_actor_type").EnumerateObject());
        Assert.Equal(216_000m, Decimal(kpi, "total_work_item_throughput_per_hour"));
        Assert.False(root.TryGetProperty("warehouse_graph", out _));
    }

    [Fact]
    public void ExportArtifact_LegacyRunner_DoesNotRequireRichKpiSummary()
    {
        using var temp = TempDirectory.Create();
        var scenarioPath = Path.Combine(temp.Path, "scenario.json");
        var artifactPath = Path.Combine(temp.Path, "legacy-run-artifact.v1.json");
        File.WriteAllText(scenarioPath, RichKpiScenarioJson());

        var result = RunCli(
            "export-artifact",
            scenarioPath,
            "-o",
            artifactPath,
            "--runner",
            "legacy");

        Assert.Equal(0, result.ExitCode);

        using var document = JsonDocument.Parse(File.ReadAllText(artifactPath));
        var kpi = document.RootElement.GetProperty("kpi_summary");

        Assert.False(kpi.TryGetProperty("order_cycle_p50_ms", out _));
        Assert.False(kpi.TryGetProperty("avg_wait_ms", out _));
        Assert.False(kpi.TryGetProperty("resource_utilization", out _));
        Assert.False(kpi.TryGetProperty("bottlenecks", out _));
        Assert.False(kpi.TryGetProperty("travel_distance_m_by_actor_type", out _));
    }

    [Fact]
    public void ExportArtifact_UnifiedRunner_RichKpiSummaryIsDeterministic()
    {
        using var temp = TempDirectory.Create();
        var scenarioPath = Path.Combine(temp.Path, "scenario.json");
        var firstArtifactPath = Path.Combine(temp.Path, "first-run-artifact.v1.json");
        var secondArtifactPath = Path.Combine(temp.Path, "second-run-artifact.v1.json");
        File.WriteAllText(scenarioPath, RichKpiScenarioJson());

        var first = RunCli(
            "export-artifact",
            scenarioPath,
            "-o",
            firstArtifactPath);
        var second = RunCli(
            "export-artifact",
            scenarioPath,
            "-o",
            secondArtifactPath);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);

        using var firstDocument = JsonDocument.Parse(File.ReadAllText(firstArtifactPath));
        using var secondDocument = JsonDocument.Parse(File.ReadAllText(secondArtifactPath));

        Assert.Equal(
            firstDocument.RootElement.GetProperty("kpi_summary").GetRawText(),
            secondDocument.RootElement.GetProperty("kpi_summary").GetRawText());
    }

    private static CliResult RunCli(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = RepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(CliProjectPath());
        startInfo.ArgumentList.Add("--");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Sim.Cli process.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"Sim.Cli process did not exit within 30 seconds. Arguments: {string.Join(" ", arguments)}");
        }

        return new CliResult(
            process.ExitCode,
            standardOutputTask.GetAwaiter().GetResult(),
            standardErrorTask.GetAwaiter().GetResult());
    }

    private static decimal Decimal(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetDecimal();
    }

    private static string RichKpiScenarioJson()
    {
        return """
        {
          "schema_version": "warehouse-scenario.v0",
          "scenario_id": "a3b-kpi-write-through",
          "seed": 4242,
          "inbound": {
            "scenario_id": "a3b-kpi-write-through.inbound",
            "seed": 1,
            "dock_count": 1,
            "forklift_count": 1,
            "process": {
              "unload_duration_ms": 10,
              "qc_duration_ms": 0,
              "putaway_duration_ms": 0
            },
            "receipts": [
              {
                "receipt_id": "receipt-1",
                "warehouse_id": "warehouse-1",
                "sku_id": "sku-inbound-1",
                "quantity": 1,
                "staging_location_id": "inbound-stage-1",
                "storage_location_id": "reserve-1",
                "arrives_at_ms": 0
              }
            ]
          },
          "outbound": {
            "scenario_id": "a3b-kpi-write-through.outbound",
            "seed": 2,
            "worker_count": 1,
            "dock_count": 1,
            "process": {
              "pick_duration_ms": 20,
              "stage_duration_ms": 0,
              "dock_travel_duration_ms": 0,
              "load_duration_ms": 0
            },
            "inventory": [
              {
                "inventory_id": "inv-outbound-1",
                "sku_id": "sku-outbound-1",
                "quantity": 1,
                "location_id": "pick-1",
                "status": "available"
              }
            ],
            "orders": [
              {
                "order_id": "order-1",
                "warehouse_id": "warehouse-1",
                "sku_id": "sku-outbound-1",
                "quantity": 1,
                "pick_location_id": "pick-1",
                "staging_location_id": "outbound-stage-1",
                "dock_id": "dock-1",
                "released_at_ms": 0
              }
            ]
          },
          "each_pick": {
            "scenario_id": "a3b-kpi-write-through.each-pick",
            "seed": 3,
            "station_count": 1,
            "worker_count": 1,
            "process": {
              "tote_bind_duration_ms": 0,
              "travel_to_station_duration_ms": 0,
              "pick_service_duration_ms": 50,
              "move_to_staging_duration_ms": 0
            },
            "inventory": [
              {
                "inventory_id": "inv-each-pick-1",
                "sku_id": "sku-each-pick-1",
                "quantity": 1,
                "location_id": "pick-face-1",
                "status": "available"
              }
            ],
            "orders": [
              {
                "order_id": "each-order-1",
                "warehouse_id": "warehouse-1",
                "sku_id": "sku-each-pick-1",
                "quantity": 1,
                "pick_face_location_id": "pick-face-1",
                "pick_station_id": "station-1",
                "staging_location_id": "each-pick-stage-1",
                "released_at_ms": 0
              }
            ]
          }
        }
        """;
    }

    private static string CliProjectPath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "src",
            "Sim.Cli",
            "Sim.Cli.csproj");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "Sim.Cli",
                "Sim.Cli.csproj");

            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Cannot find repository root from test output directory.");
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
                $"awt-a3b-kpi-artifact-{Guid.NewGuid():N}");
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
