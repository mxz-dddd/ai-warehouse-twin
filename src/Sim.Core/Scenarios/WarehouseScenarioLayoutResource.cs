using Sim.Core.Domain;

namespace Sim.Core.Scenarios;

public sealed record WarehouseScenarioLayoutResource
{
    public WarehouseScenarioLayoutResource(
        string resourceId,
        WarehouseScenarioPositionPoint position)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Scenario layout resource id cannot be empty.");
        }

        ResourceId = resourceId;
        Position = position ?? throw new DomainRuleViolationException(
            $"Scenario layout resource position cannot be null. ResourceId: {resourceId}.");
    }

    public string ResourceId { get; }

    public WarehouseScenarioPositionPoint Position { get; }
}
