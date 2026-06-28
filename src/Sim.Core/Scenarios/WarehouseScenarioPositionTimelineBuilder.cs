using Sim.Core.Resources;

namespace Sim.Core.Scenarios;

public static class WarehouseScenarioPositionTimelineBuilder
{
    public static IReadOnlyList<WarehouseScenarioPositionTimelineEntry> Build(
        IReadOnlyList<ResourceLeaseTimelineEntry> resourceLeaseTimeline,
        WarehouseScenarioLayout layout)
    {
        ArgumentNullException.ThrowIfNull(resourceLeaseTimeline);
        ArgumentNullException.ThrowIfNull(layout);

        return resourceLeaseTimeline
            .SelectMany(lease => new[]
            {
                new WarehouseScenarioPositionTimelineEntry(
                    lease.OperationId,
                    lease.OperationType,
                    lease.StageType,
                    lease.ResourceId,
                    lease.StartedAtMs,
                    "start",
                    layout.GetResourcePosition(lease.ResourceId)),
                new WarehouseScenarioPositionTimelineEntry(
                    lease.OperationId,
                    lease.OperationType,
                    lease.StageType,
                    lease.ResourceId,
                    lease.FinishedAtMs,
                    "finish",
                    layout.GetResourcePosition(lease.ResourceId)),
            })
            .OrderBy(entry => entry.AtMs)
            .ThenBy(entry => entry.OperationType, StringComparer.Ordinal)
            .ThenBy(entry => entry.OperationId, StringComparer.Ordinal)
            .ThenBy(entry => entry.StageType, StringComparer.Ordinal)
            .ThenBy(entry => entry.ResourceId, StringComparer.Ordinal)
            .ThenBy(entry => entry.EventType, StringComparer.Ordinal)
            .ToArray();
    }
}
