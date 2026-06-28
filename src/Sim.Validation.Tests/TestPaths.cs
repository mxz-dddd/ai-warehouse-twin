namespace Sim.Validation.Tests;

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

    public static string DatasetsDir() => Path.Combine(RepoRoot(), "datasets");

    public static string TemplatesDir() => Path.Combine(DatasetsDir(), "templates");

    public static string ValidCasesDir() => Path.Combine(DatasetsDir(), "validation-cases", "valid");

    public static string InvalidCasesDir() => Path.Combine(DatasetsDir(), "validation-cases", "invalid");

    public static string NormalizeNewlines(string value)
        => value.Replace("\r\n", "\n").Replace("\r", "\n");
}
