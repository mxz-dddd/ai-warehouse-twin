using System.Text.Json;
using Sim.Contracts.Artifacts;

namespace Sim.Report;

public static class RunArtifactLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    public static RunArtifact Load(string path)
    {
        return Deserialize(File.ReadAllText(path));
    }

    public static RunArtifact Deserialize(string json)
    {
        var artifact = JsonSerializer.Deserialize<RunArtifact>(json, Options);
        if (artifact is null)
        {
            throw new InvalidDataException("run-artifact JSON deserialized to null.");
        }

        return artifact;
    }
}
