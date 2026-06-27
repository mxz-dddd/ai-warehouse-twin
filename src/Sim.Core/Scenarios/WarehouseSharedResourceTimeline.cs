using Sim.Core.Domain;
using Sim.Core.Resources;

namespace Sim.Core.Scenarios;

public sealed record WarehouseSharedResourceWorkItem
{
    public WarehouseSharedResourceWorkItem(
        string flowName,
        string workItemId,
        string resourceId,
        long requestedAtMs,
        long serviceDurationMs,
        int sequence)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            throw new DomainRuleViolationException(
                "Warehouse shared resource work item FlowName cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(workItemId))
        {
            throw new DomainRuleViolationException(
                "Warehouse shared resource work item WorkItemId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Warehouse shared resource work item ResourceId cannot be empty.");
        }

        if (requestedAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Warehouse shared resource request time cannot be negative. RequestedAtMs: {requestedAtMs}.");
        }

        if (serviceDurationMs <= 0)
        {
            throw new DomainRuleViolationException(
                $"Warehouse shared resource service duration must be positive. ServiceDurationMs: {serviceDurationMs}.");
        }

        if (sequence < 0)
        {
            throw new DomainRuleViolationException(
                $"Warehouse shared resource sequence cannot be negative. Sequence: {sequence}.");
        }

        FlowName = flowName;
        WorkItemId = workItemId;
        ResourceId = resourceId;
        RequestedAtMs = requestedAtMs;
        ServiceDurationMs = serviceDurationMs;
        Sequence = sequence;
    }

    public string FlowName { get; }

    public string WorkItemId { get; }

    public string ResourceId { get; }

    public long RequestedAtMs { get; }

    public long ServiceDurationMs { get; }

    public int Sequence { get; }

    public string OwnerId => $"{FlowName}:{WorkItemId}";
}

public sealed record WarehouseSharedResourceAllocation(
    string ResourceId,
    string OwnerId,
    long StartedAtMs,
    long FinishedAtMs);

public sealed record WarehouseSharedResourceTimelineResult(
    IReadOnlyList<WarehouseSharedResourceAllocation> Allocations,
    string EventLogText);

public sealed class WarehouseSharedResourceTimelineRunner
{
    public WarehouseSharedResourceTimelineResult RunCapacityOne(
        string resourceId,
        IEnumerable<WarehouseSharedResourceWorkItem> workItems)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Warehouse shared resource timeline ResourceId cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(workItems);

        var orderedWorkItems = workItems
            .OrderBy(item => item.RequestedAtMs)
            .ThenBy(item => item.Sequence)
            .ThenBy(item => item.WorkItemId, StringComparer.Ordinal)
            .ToArray();

        if (orderedWorkItems.Length == 0)
        {
            throw new DomainRuleViolationException(
                "Warehouse shared resource timeline requires at least one work item.");
        }

        if (orderedWorkItems.Any(item => item.ResourceId != resourceId))
        {
            throw new DomainRuleViolationException(
                $"All warehouse timeline work items must request resource {resourceId}.");
        }

        var workItemsByOwnerId = orderedWorkItems.ToDictionary(
            item => item.OwnerId,
            StringComparer.Ordinal);

        var pool = new ResourcePool(
            $"warehouse-shared-{resourceId}",
            ResourceType.Dock,
            [new ResourceUnit(resourceId, ResourceType.Dock, resourceId)]);

        var allocations = new List<WarehouseSharedResourceAllocation>();
        var eventLogLines = new List<string>();
        ActiveService? activeService = null;

        foreach (var workItem in orderedWorkItems)
        {
            while (activeService is not null &&
                   activeService.FinishedAtMs <= workItem.RequestedAtMs)
            {
                activeService = ReleaseAndStartNext(
                    pool,
                    activeService,
                    workItemsByOwnerId,
                    allocations,
                    eventLogLines);
            }

            eventLogLines.Add(
                $"{workItem.RequestedAtMs}|resource.requested|resource_id={resourceId}|owner={workItem.OwnerId}");

            var request = new ResourceRequest(
                $"warehouse-shared-{workItem.Sequence}-{workItem.OwnerId}",
                workItem.OwnerId,
                workItem.RequestedAtMs);

            var lease = pool.AcquireOrQueue(request, workItem.RequestedAtMs);
            if (lease is null)
            {
                eventLogLines.Add(
                    $"{workItem.RequestedAtMs}|resource.queued|resource_id={resourceId}|owner={workItem.OwnerId}");
                continue;
            }

            activeService = StartService(
                lease,
                workItem,
                workItem.RequestedAtMs,
                allocations,
                eventLogLines);
        }

        while (activeService is not null)
        {
            activeService = ReleaseAndStartNext(
                pool,
                activeService,
                workItemsByOwnerId,
                allocations,
                eventLogLines);
        }

        return new WarehouseSharedResourceTimelineResult(
            allocations.ToArray(),
            string.Join("\n", eventLogLines));
    }

    private static ActiveService? ReleaseAndStartNext(
        ResourcePool pool,
        ActiveService activeService,
        IReadOnlyDictionary<string, WarehouseSharedResourceWorkItem> workItemsByOwnerId,
        ICollection<WarehouseSharedResourceAllocation> allocations,
        ICollection<string> eventLogLines)
    {
        eventLogLines.Add(
            $"{activeService.FinishedAtMs}|resource.released|resource_id={activeService.Lease.Resource.ResourceId}|owner={activeService.WorkItem.OwnerId}");

        var nextLease = pool.Release(
            activeService.Lease,
            activeService.FinishedAtMs);

        if (nextLease is null)
        {
            return null;
        }

        var nextWorkItem = workItemsByOwnerId[nextLease.TaskId];
        return StartService(
            nextLease,
            nextWorkItem,
            activeService.FinishedAtMs,
            allocations,
            eventLogLines);
    }

    private static ActiveService StartService(
        ResourceLease lease,
        WarehouseSharedResourceWorkItem workItem,
        long startedAtMs,
        ICollection<WarehouseSharedResourceAllocation> allocations,
        ICollection<string> eventLogLines)
    {
        var finishedAtMs = checked(startedAtMs + workItem.ServiceDurationMs);

        allocations.Add(new WarehouseSharedResourceAllocation(
            lease.Resource.ResourceId,
            workItem.OwnerId,
            startedAtMs,
            finishedAtMs));

        eventLogLines.Add(
            $"{startedAtMs}|resource.acquired|resource_id={lease.Resource.ResourceId}|owner={workItem.OwnerId}");

        return new ActiveService(lease, workItem, finishedAtMs);
    }

    private sealed record ActiveService(
        ResourceLease Lease,
        WarehouseSharedResourceWorkItem WorkItem,
        long FinishedAtMs);
}
