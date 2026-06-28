using Sim.Core.Domain;

namespace Sim.Core.Scenarios;

public sealed record WarehouseScenarioPositionPoint
{
    public WarehouseScenarioPositionPoint(
        string nodeId,
        decimal x,
        decimal y)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new DomainRuleViolationException(
                "Scenario position point node id cannot be empty.");
        }

        NodeId = nodeId;
        X = x;
        Y = y;
    }

    public string NodeId { get; }

    public decimal X { get; }

    public decimal Y { get; }
}
