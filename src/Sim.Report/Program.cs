using System.Text.Json;
using Sim.Report;

try
{
    if (args.Length == 1)
    {
        var loaded = RunArtifactLoader.Load(args[0]);
        Console.Out.Write(MarkdownReportRenderer.Render(loaded));
        return 0;
    }

    if (args.Length == 3 && args[1] == "-o")
    {
        var loaded = RunArtifactLoader.Load(args[0]);
        File.WriteAllText(args[2], MarkdownReportRenderer.Render(loaded));
        Console.WriteLine($"Wrote report: {args[2]}");
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
return 1;
