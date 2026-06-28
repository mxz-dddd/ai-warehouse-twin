using Sim.Core.Domain;

namespace Sim.Core.Scenarios;

public sealed record WarehouseScenarioComparisonResult
{
    public WarehouseScenarioComparisonResult(
        string baselineScenarioId,
        string candidateScenarioId,
        WarehouseRunResult baselineResult,
        WarehouseRunResult candidateResult,
        WarehouseScenarioComparisonMetrics baselineMetrics,
        WarehouseScenarioComparisonMetrics candidateMetrics,
        IReadOnlyList<WarehouseScenarioComparisonDelta> deltas)
    {
        if (string.IsNullOrWhiteSpace(baselineScenarioId))
        {
            throw new DomainRuleViolationException(
                "Warehouse scenario comparison baseline scenario id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(candidateScenarioId))
        {
            throw new DomainRuleViolationException(
                "Warehouse scenario comparison candidate scenario id cannot be empty.");
        }

        BaselineScenarioId = baselineScenarioId;
        CandidateScenarioId = candidateScenarioId;
        BaselineResult = baselineResult ?? throw new ArgumentNullException(nameof(baselineResult));
        CandidateResult = candidateResult ?? throw new ArgumentNullException(nameof(candidateResult));
        BaselineMetrics = baselineMetrics ?? throw new ArgumentNullException(nameof(baselineMetrics));
        CandidateMetrics = candidateMetrics ?? throw new ArgumentNullException(nameof(candidateMetrics));
        Deltas = deltas?.ToArray() ?? throw new ArgumentNullException(nameof(deltas));
    }

    public string BaselineScenarioId { get; }

    public string CandidateScenarioId { get; }

    public WarehouseRunResult BaselineResult { get; }

    public WarehouseRunResult CandidateResult { get; }

    public WarehouseScenarioComparisonMetrics BaselineMetrics { get; }

    public WarehouseScenarioComparisonMetrics CandidateMetrics { get; }

    public IReadOnlyList<WarehouseScenarioComparisonDelta> Deltas { get; }
}
