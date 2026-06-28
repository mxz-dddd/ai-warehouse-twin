namespace Sim.Contracts.Artifacts;

public sealed record ComparisonArtifactDelta
{
    public ComparisonArtifactDelta(
        string metricName,
        decimal baselineValue,
        decimal candidateValue,
        decimal delta,
        decimal? deltaPercent,
        string direction)
    {
        EnsureNotEmpty(metricName, nameof(metricName));
        EnsureNotEmpty(direction, nameof(direction));

        MetricName = metricName;
        BaselineValue = baselineValue;
        CandidateValue = candidateValue;
        Delta = delta;
        DeltaPercent = deltaPercent;
        Direction = direction;
    }

    public string MetricName { get; }

    public decimal BaselineValue { get; }

    public decimal CandidateValue { get; }

    public decimal Delta { get; }

    public decimal? DeltaPercent { get; }

    public string Direction { get; }

    private static void EnsureNotEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"ComparisonArtifact delta {name} cannot be empty.",
                name);
        }
    }
}
