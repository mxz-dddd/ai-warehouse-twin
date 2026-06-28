using System.Text.Json;
using Sim.Contracts.Artifacts;

namespace Sim.Report;

public static class ComparisonArtifactLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    public static ComparisonArtifact Load(string path)
    {
        return Deserialize(File.ReadAllText(path));
    }

    public static ComparisonArtifact Deserialize(string json)
    {
        var artifact = JsonSerializer.Deserialize<ComparisonArtifact>(
            json,
            Options);
        if (artifact is null)
        {
            throw new InvalidDataException(
                "comparison-artifact JSON deserialized to null.");
        }

        if (artifact.SchemaVersion != ComparisonArtifact.CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Unsupported schema_version '{artifact.SchemaVersion}', expected '{ComparisonArtifact.CurrentSchemaVersion}'.");
        }

        if (artifact.Baseline is null)
        {
            throw new InvalidDataException(
                "comparison-artifact baseline must not be null.");
        }

        if (artifact.Candidate is null)
        {
            throw new InvalidDataException(
                "comparison-artifact candidate must not be null.");
        }

        if (string.IsNullOrWhiteSpace(artifact.Baseline.ScenarioId))
        {
            throw new InvalidDataException(
                "comparison-artifact baseline scenario_id must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(artifact.Candidate.ScenarioId))
        {
            throw new InvalidDataException(
                "comparison-artifact candidate scenario_id must not be empty.");
        }

        if (artifact.Deltas is null)
        {
            throw new InvalidDataException(
                "comparison-artifact deltas must not be null.");
        }

        return artifact;
    }
}
