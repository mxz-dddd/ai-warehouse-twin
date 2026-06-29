using System.Text.Json;
using WarehouseTwin.Contracts;

namespace Sim.Report;

public static class MovementArtifactLoader
{
    public const string CurrentSchemaVersion = "movement-artifact.v1";
    public const string CurrentArtifactKind = "warehouse-movement";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    public static MovementArtifact Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"MovementArtifact JSON file was not found: {path}",
                path);
        }

        return Deserialize(File.ReadAllText(path));
    }

    public static MovementArtifact Deserialize(string json)
    {
        MovementArtifact? artifact;
        try
        {
            artifact = JsonSerializer.Deserialize<MovementArtifact>(json, Options);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "MovementArtifact JSON is invalid.",
                exception);
        }

        if (artifact is null)
        {
            throw new InvalidDataException("MovementArtifact JSON deserialized to null.");
        }

        if (artifact.schema_version != CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Unsupported schema_version '{artifact.schema_version}', expected '{CurrentSchemaVersion}'.");
        }

        if (artifact.artifact_kind != CurrentArtifactKind)
        {
            throw new InvalidDataException(
                $"Unsupported artifact_kind '{artifact.artifact_kind}', expected '{CurrentArtifactKind}'.");
        }

        return artifact;
    }
}
