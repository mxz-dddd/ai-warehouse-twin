namespace Sim.Contracts.Artifacts;

public sealed record ComparisonArtifactScenarioSummary
{
    public ComparisonArtifactScenarioSummary(
        string scenarioId,
        ComparisonArtifactMetrics metrics)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException(
                "ComparisonArtifact scenario id cannot be empty.",
                nameof(scenarioId));
        }

        ScenarioId = scenarioId;
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public string ScenarioId { get; }

    public ComparisonArtifactMetrics Metrics { get; }
}
