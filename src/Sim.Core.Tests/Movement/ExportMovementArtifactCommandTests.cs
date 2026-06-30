using System.Diagnostics;
using Sim.Report;
using Xunit;

namespace Sim.Core.Tests.Movement;

public sealed class ExportMovementArtifactCommandTests
{
    [Fact]
    public void ExportMovementArtifact_WritesValidMovementArtifactJson()
    {
        using var temp = TempDirectory.Create();
        var outputPath = Path.Combine(temp.Path, "movement-artifact.v1.json");

        var result = RunCli(
            "export-movement-artifact",
            SampleScenarioPath(),
            "-o",
            outputPath,
            "--run-id",
            "cli-r2b-test-run",
            "--source-run-artifact",
            "run-artifact.v1.json");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(outputPath));

        var artifact = MovementArtifactLoader.Load(outputPath);
        Assert.Equal(MovementArtifactLoader.CurrentSchemaVersion, artifact.schema_version);
        Assert.Equal(MovementArtifactLoader.CurrentArtifactKind, artifact.artifact_kind);
        Assert.False(string.IsNullOrWhiteSpace(artifact.scenario_id));
        Assert.Equal("cli-r2b-test-run", artifact.run_id);
        Assert.NotNull(artifact.warehouse_graph);
        Assert.NotEmpty(artifact.actors);
        Assert.NotEmpty(artifact.movement_events);
        Assert.NotEmpty(artifact.route_segments);
        Assert.NotNull(artifact.provenance);
    }

    [Fact]
    public void ExportMovementArtifact_SameInputTwice_ProducesIdenticalBytes()
    {
        using var temp = TempDirectory.Create();
        var firstPath = Path.Combine(temp.Path, "movement-1.json");
        var secondPath = Path.Combine(temp.Path, "movement-2.json");

        var first = RunCli(
            "export-movement-artifact",
            SampleScenarioPath(),
            "--output",
            firstPath,
            "--run-id",
            "stable-run");
        var second = RunCli(
            "export-movement-artifact",
            SampleScenarioPath(),
            "--output",
            secondPath,
            "--run-id",
            "stable-run");

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Equal(
            File.ReadAllBytes(firstPath),
            File.ReadAllBytes(secondPath));
    }

    [Fact]
    public void ExportMovementArtifact_MissingOutput_ReturnsNonZero()
    {
        var result = RunCli(
            "export-movement-artifact",
            SampleScenarioPath());

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(
            "requires -o or --output",
            result.StandardError,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ExportMovementArtifact_DoesNotChangeExistingExportArtifactBehavior()
    {
        using var temp = TempDirectory.Create();
        var outputPath = Path.Combine(temp.Path, "run-artifact.v1.json");

        var result = RunCli(
            "export-artifact",
            SampleScenarioPath(),
            "-o",
            outputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            File.ReadAllBytes(SampleRunArtifactGoldenPath()),
            File.ReadAllBytes(outputPath));
    }

    [Fact]
    public void ExportMovementArtifact_DoesNotWriteGoldenArtifacts()
    {
        using var temp = TempDirectory.Create();
        var before = ArtifactFileSnapshot();
        var outputPath = Path.Combine(temp.Path, "movement-artifact.v1.json");

        var result = RunCli(
            "export-movement-artifact",
            SampleScenarioPath(),
            "-o",
            outputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(outputPath));
        Assert.Equal(before, ArtifactFileSnapshot());
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

    private static IReadOnlyList<string> ArtifactFileSnapshot()
    {
        return Directory
            .EnumerateFiles(DatasetsDirectory(), "*", SearchOption.AllDirectories)
            .Where(path => path.Contains(
                $"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string SampleScenarioPath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "datasets",
            "sample-small-warehouse",
            "scenario.json");
    }

    private static string SampleRunArtifactGoldenPath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "datasets",
            "sample-small-warehouse",
            "artifacts",
            "run-artifact.v1.json");
    }

    private static string CliProjectPath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "src",
            "Sim.Cli",
            "Sim.Cli.csproj");
    }

    private static string DatasetsDirectory()
    {
        return Path.Combine(RepositoryRoot(), "datasets");
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
                $"awt-movement-cli-{Guid.NewGuid():N}");
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
