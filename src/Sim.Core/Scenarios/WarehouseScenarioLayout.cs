using System.Collections.ObjectModel;
using Sim.Core.Domain;
using Sim.Core.Resources;

namespace Sim.Core.Scenarios;

public sealed class WarehouseScenarioLayout
{
    private readonly IReadOnlyDictionary<string, WarehouseScenarioPositionPoint>
        _resourcePositions;

    private WarehouseScenarioLayout(
        IReadOnlyList<WarehouseScenarioLayoutResource> resources)
    {
        Resources = new ReadOnlyCollection<WarehouseScenarioLayoutResource>(
            resources.ToArray());
        _resourcePositions = new ReadOnlyDictionary<string, WarehouseScenarioPositionPoint>(
            resources.ToDictionary(
                resource => resource.ResourceId,
                resource => resource.Position,
                StringComparer.Ordinal));
    }

    public IReadOnlyList<WarehouseScenarioLayoutResource> Resources { get; }

    public static WarehouseScenarioLayout FromResourceLeases(
        IReadOnlyList<ResourceLeaseTimelineEntry> resourceLeaseTimeline)
    {
        ArgumentNullException.ThrowIfNull(resourceLeaseTimeline);

        var resources = resourceLeaseTimeline
            .Select(entry => entry.ResourceId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select((resourceId, index) => new WarehouseScenarioLayoutResource(
                resourceId,
                new WarehouseScenarioPositionPoint(resourceId, index, 0m)))
            .ToArray();

        return new WarehouseScenarioLayout(resources);
    }

    public WarehouseScenarioPositionPoint GetResourcePosition(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Scenario layout resource id cannot be empty.");
        }

        if (!_resourcePositions.TryGetValue(resourceId, out var position))
        {
            throw new DomainRuleViolationException(
                $"Scenario layout does not contain resource. ResourceId: {resourceId}.");
        }

        return position;
    }
}
