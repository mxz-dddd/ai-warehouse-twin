namespace Sim.Contracts.Artifacts;

public sealed record ComparisonArtifactKpiDelta
{
    public decimal BaselineValue { get; init; }

    public decimal CandidateValue { get; init; }

    public decimal Delta { get; init; }

    public bool LowerIsBetter { get; init; }
}
