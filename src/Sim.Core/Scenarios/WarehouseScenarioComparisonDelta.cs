using Sim.Core.Domain;

namespace Sim.Core.Scenarios;

public sealed record WarehouseScenarioComparisonDelta
{
    private const string Increase = "increase";
    private const string Decrease = "decrease";
    private const string Unchanged = "unchanged";

    public WarehouseScenarioComparisonDelta(
        string metricName,
        decimal baselineValue,
        decimal candidateValue)
    {
        if (string.IsNullOrWhiteSpace(metricName))
        {
            throw new DomainRuleViolationException(
                "Warehouse scenario comparison metric name cannot be empty.");
        }

        MetricName = metricName;
        BaselineValue = baselineValue;
        CandidateValue = candidateValue;
        Delta = candidateValue - baselineValue;
        DeltaPercent = baselineValue == 0m
            ? null
            : Delta / baselineValue * 100m;
        Direction = Delta switch
        {
            > 0m => Increase,
            < 0m => Decrease,
            _ => Unchanged,
        };
    }

    public string MetricName { get; }

    public decimal BaselineValue { get; }

    public decimal CandidateValue { get; }

    public decimal Delta { get; }

    public decimal? DeltaPercent { get; }

    public string Direction { get; }
}
