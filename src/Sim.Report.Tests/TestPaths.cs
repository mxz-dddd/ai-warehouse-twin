namespace Sim.Report.Tests;

internal static class TestPaths
{
    public static string RepoRoot()
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

    public static string ArtifactPath()
    {
        return Path.Combine(
            RepoRoot(), "datasets", "sample-small-warehouse", "artifacts", "run-artifact.v1.json");
    }

    public static string GoldenReportPath()
    {
        return Path.Combine(
            RepoRoot(), "datasets", "sample-small-warehouse", "artifacts", "run-artifact.v1.report.md");
    }

    public static string NormalizeNewlines(string value)
    {
        return value.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
