using System.Text.Json;
using Sim.Report;

// Extract optional --movement-artifact <path> before processing positional args.
string? movementArtifactPath = null;
var positional = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--movement-artifact" && i + 1 < args.Length)
    {
        movementArtifactPath = args[++i];
    }
    else
    {
        positional.Add(args[i]);
    }
}

try
{
    if (positional.Count == 1)
    {
        var loaded = RunArtifactLoader.Load(positional[0]);
        var report = MarkdownReportRenderer.Render(loaded);
        report += RenderProvenance(movementArtifactPath);
        Console.Out.Write(report);
        return 0;
    }

    if (positional.Count == 3 && positional[1] == "-o")
    {
        var loaded = RunArtifactLoader.Load(positional[0]);
        var report = MarkdownReportRenderer.Render(loaded);
        report += RenderProvenance(movementArtifactPath);
        File.WriteAllText(positional[2], report);
        Console.WriteLine($"Wrote report: {positional[2]}");
        return 0;
    }
}
catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

Console.Error.WriteLine("Usage:");
Console.Error.WriteLine("  dotnet run --project src/Sim.Report -- <run-artifact.json>");
Console.Error.WriteLine("  dotnet run --project src/Sim.Report -- <run-artifact.json> -o <report.md>");
Console.Error.WriteLine("  dotnet run --project src/Sim.Report -- <run-artifact.json> [--movement-artifact <movement-artifact.json>]");
return 1;

static string RenderProvenance(string? path)
{
    if (path is null) return string.Empty;
    var artifact = MovementArtifactLoader.Load(path);
    return "\n" + MovementProvenanceRenderer.Render(artifact);
}
