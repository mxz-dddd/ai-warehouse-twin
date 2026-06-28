namespace Sim.Contracts.Artifacts;

public sealed record ComparisonArtifact
{
    public const string CurrentSchemaVersion = "comparison_artifact.v1";

    public ComparisonArtifact(
        string schemaVersion,
        ComparisonArtifactScenarioSummary baseline,
        ComparisonArtifactScenarioSummary candidate,
        IReadOnlyList<ComparisonArtifactDelta> deltas)
    {
        if (string.IsNullOrWhiteSpace(schemaVersion))
        {
            throw new ArgumentException(
                "ComparisonArtifact schema version cannot be empty.",
                nameof(schemaVersion));
        }

        SchemaVersion = schemaVersion;
        Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        Deltas = deltas?.ToArray() ?? throw new ArgumentNullException(nameof(deltas));
    }

    public string SchemaVersion { get; }

    public ComparisonArtifactScenarioSummary Baseline { get; }

    public ComparisonArtifactScenarioSummary Candidate { get; }

    public IReadOnlyList<ComparisonArtifactDelta> Deltas { get; }
}
