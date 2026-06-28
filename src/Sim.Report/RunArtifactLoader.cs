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

        if (artifact.SchemaVersion != RunArtifact.CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Unsupported schema_version '{artifact.SchemaVersion}', expected '{RunArtifact.CurrentSchemaVersion}'.");
        }

        if (artifact.ArtifactKind != RunArtifact.CurrentArtifactKind)
        {
            throw new InvalidDataException(
                $"Unsupported artifact_kind '{artifact.ArtifactKind}', expected '{RunArtifact.CurrentArtifactKind}'.");
        }

        return artifact;
    }
}
