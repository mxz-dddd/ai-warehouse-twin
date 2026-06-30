using System.Text.Json.Serialization;

namespace Sim.Contracts.Artifacts;

public sealed record ComparisonArtifact
{
    public const string CurrentSchemaVersion = "comparison_artifact.v1";

    public ComparisonArtifact(
        string schemaVersion,
        ComparisonArtifactScenarioSummary baseline,
        ComparisonArtifactScenarioSummary candidate,
        IReadOnlyList<ComparisonArtifactDelta> deltas,
        IReadOnlyDictionary<string, ComparisonArtifactKpiDelta>? kpiDeltas = null,
        IReadOnlyDictionary<string, decimal>? improvementPct = null)
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
        KpiDeltas = kpiDeltas is null
            ? null
            : new Dictionary<string, ComparisonArtifactKpiDelta>(
                kpiDeltas,
                StringComparer.Ordinal);
        ImprovementPct = improvementPct is null
            ? null
            : new Dictionary<string, decimal>(
                improvementPct,
                StringComparer.Ordinal);
    }

    public string SchemaVersion { get; }

    public ComparisonArtifactScenarioSummary Baseline { get; }

    public ComparisonArtifactScenarioSummary Candidate { get; }

    public IReadOnlyList<ComparisonArtifactDelta> Deltas { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, ComparisonArtifactKpiDelta>? KpiDeltas { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, decimal>? ImprovementPct { get; }
}
