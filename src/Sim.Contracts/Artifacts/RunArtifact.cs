namespace Sim.Contracts.Artifacts;

public sealed record RunArtifact
{
    public const string CurrentSchemaVersion = "run-artifact.v1";
    public const string CurrentArtifactKind = "warehouse-simulation-run";

    public required string SchemaVersion { get; init; }

    public required string ArtifactKind { get; init; }

    public required string ScenarioId { get; init; }

    public int Seed { get; init; }

    public long StartedAtMs { get; init; }

    public long FinishedAtMs { get; init; }

    public long FinalWorldTimeMs { get; init; }

    public required RunArtifactKpiSummary KpiSummary { get; init; }

    public RunArtifactLayout Layout { get; init; } = RunArtifactLayout.Empty;

    public IReadOnlyList<RunArtifactPositionTimelineEntry> PositionTimeline { get; init; } =
        Array.Empty<RunArtifactPositionTimelineEntry>();

    public required IReadOnlyList<string> EventLog { get; init; }
}
