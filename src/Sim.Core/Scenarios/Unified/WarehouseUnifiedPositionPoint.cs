using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Unified;

public sealed record WarehouseUnifiedPositionPoint
{
    public WarehouseUnifiedPositionPoint(
        string nodeId,
        decimal x,
        decimal y)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new DomainRuleViolationException(
                "Unified position point node id cannot be empty.");
        }

        NodeId = nodeId;
        X = x;
        Y = y;
    }

    public string NodeId { get; }

    public decimal X { get; }

    public decimal Y { get; }
}
