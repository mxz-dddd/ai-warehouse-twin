using System.Collections.ObjectModel;
using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Unified;

public sealed class WarehouseUnifiedLayout
{
    private readonly IReadOnlyDictionary<string, WarehouseUnifiedPositionPoint>
        _resourcePositions;

    public WarehouseUnifiedLayout(
        IReadOnlyDictionary<string, WarehouseUnifiedPositionPoint> resourcePositions)
    {
        ArgumentNullException.ThrowIfNull(resourcePositions);

        var positions = new SortedDictionary<string, WarehouseUnifiedPositionPoint>(
            StringComparer.Ordinal);

        foreach (var entry in resourcePositions)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new DomainRuleViolationException(
                    "Unified layout resource id cannot be empty.");
            }

            positions[entry.Key] = entry.Value ?? throw new DomainRuleViolationException(
                $"Unified layout position cannot be null. ResourceId: {entry.Key}.");
        }

        _resourcePositions =
            new ReadOnlyDictionary<string, WarehouseUnifiedPositionPoint>(positions);
    }

    public WarehouseUnifiedPositionPoint GetResourcePosition(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Unified layout resource id cannot be empty.");
        }

        if (!_resourcePositions.TryGetValue(resourceId, out var position))
        {
            throw new DomainRuleViolationException(
                $"Unified layout does not contain resource. ResourceId: {resourceId}.");
        }

        return position;
    }
}
