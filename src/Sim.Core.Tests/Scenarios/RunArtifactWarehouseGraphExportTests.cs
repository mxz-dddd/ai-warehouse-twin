using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class RunArtifactWarehouseGraphExportTests
{
    [Fact]
    public void ExportArtifact_WithLayoutGraph_WritesStaticWarehouseGraph()
    {
        using var temp = TempDirectory.Create();
        var scenarioPath = WriteScenarioWithLayoutGraph(temp.Path);
        var outputPath = Path.Combine(temp.Path, "run-artifact.v1.json");

        var result = RunCli("export-artifact", scenarioPath, "-o", outputPath);

        Assert.Equal(0, result.ExitCode);
        using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
        var graph = document.RootElement.GetProperty("warehouse_graph");
        var nodes = graph.GetProperty("nodes").EnumerateArray().ToArray();
        var edges = graph.GetProperty("edges").EnumerateArray().ToArray();
        var nodeIds = nodes
            .Select(node => node.GetProperty("node_id").GetString())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(3, nodes.Length);
        Assert.Equal(2, edges.Length);
        Assert.Contains(nodes, node =>
            node.GetProperty("node_id").GetString() == "node-dock-in" &&
            node.GetProperty("node_type").GetString() == "dock" &&
            node.GetProperty("x").GetDecimal() == 0m &&
            node.GetProperty("y").GetDecimal() == 0m);

        foreach (var edge in edges)
        {
            Assert.Contains(edge.GetProperty("from_node_id").GetString(), nodeIds);
            Assert.Contains(edge.GetProperty("to_node_id").GetString(), nodeIds);
            Assert.False(edge.TryGetProperty("distance_mm", out _));
            Assert.True(edge.TryGetProperty("distance_m", out _));
            Assert.Equal(0, edge.GetProperty("travel_time_ms").GetInt64());
        }

        var firstEdge = edges.Single(edge =>
            edge.GetProperty("edge_id").GetString() == "edge-dock-aisle");
        Assert.Equal(2.5m, firstEdge.GetProperty("distance_m").GetDecimal());
        Assert.True(firstEdge.GetProperty("bidirectional").GetBoolean());
    }

    [Fact]
    public void ExportArtifact_WithLayoutGraph_IsByteStable()
    {
        using var temp = TempDirectory.Create();
        var scenarioPath = WriteScenarioWithLayoutGraph(temp.Path);
        var firstPath = Path.Combine(temp.Path, "run-artifact-a.json");
        var secondPath = Path.Combine(temp.Path, "run-artifact-b.json");

        var first = RunCli("export-artifact", scenarioPath, "-o", firstPath);
        var second = RunCli("export-artifact", scenarioPath, "-o", secondPath);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Equal(File.ReadAllBytes(firstPath), File.ReadAllBytes(secondPath));
    }

    [Fact]
    public void ExportArtifact_WithoutLayoutGraph_OmitsWarehouseGraph()
    {
        using var temp = TempDirectory.Create();
        var outputPath = Path.Combine(temp.Path, "run-artifact.v1.json");

        var result = RunCli("export-artifact", SampleScenarioPath(), "-o", outputPath);

        Assert.Equal(0, result.ExitCode);
        using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
        Assert.False(document.RootElement.TryGetProperty("warehouse_graph", out _));
    }

    private static string WriteScenarioWithLayoutGraph(string directory)
    {
        var scenario = JsonNode.Parse(File.ReadAllText(SampleScenarioPath()))!.AsObject();
        scenario["layout_graph"] = JsonNode.Parse("""
        {
          "nodes": [
            { "nodeId": "node-dock-in", "nodeType": "dock", "xMm": 0, "yMm": 0 },
            { "nodeId": "node-aisle-a", "nodeType": "aisle", "xMm": 2500, "yMm": 0 },
            { "nodeId": "node-pack-out", "nodeType": "pack_station", "xMm": 5000, "yMm": 1000 }
          ],
          "edges": [
            {
              "edgeId": "edge-dock-aisle",
              "fromNodeId": "node-dock-in",
              "toNodeId": "node-aisle-a",
              "distanceMm": 2500,
              "bidirectional": true
            },
            {
              "edgeId": "edge-aisle-pack",
              "fromNodeId": "node-aisle-a",
              "toNodeId": "node-pack-out",
              "distanceMm": 3200,
              "bidirectional": false
            }
          ]
        }
        """);

        var path = Path.Combine(directory, "scenario-with-layout-graph.json");
        File.WriteAllText(
            path,
            scenario.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        return path;
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
        process.WaitForExit();

        return new CliResult(
            process.ExitCode,
            standardOutputTask.GetAwaiter().GetResult(),
            standardErrorTask.GetAwaiter().GetResult());
    }

    private static string SampleScenarioPath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "datasets",
            "sample-small-warehouse",
            "scenario.json");
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
                $"awt-run-graph-{Guid.NewGuid():N}");
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
