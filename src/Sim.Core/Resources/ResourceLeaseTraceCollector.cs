using System.Collections.ObjectModel;
using Sim.Core.Domain;

namespace Sim.Core.Resources;

public sealed class ResourceLeaseTraceCollector
{
    private readonly List<ResourceLeaseTimelineEntry> _entries = [];

    public IReadOnlyList<ResourceLeaseTimelineEntry> Timeline =>
        new ReadOnlyCollection<ResourceLeaseTimelineEntry>(
            _entries
                .OrderBy(entry => entry.StartedAtMs)
                .ThenBy(entry => entry.OperationId, StringComparer.Ordinal)
                .ThenBy(entry => entry.StageType, StringComparer.Ordinal)
                .ThenBy(entry => entry.ResourceId, StringComparer.Ordinal)
                .ToArray());

    internal void Record(
        ResourceLease lease,
        long finishedAtMs,
        string operationType,
        string stageType)
    {
        ArgumentNullException.ThrowIfNull(lease);

        if (finishedAtMs <= lease.AcquiredAtMs)
        {
            return;
        }

        _entries.Add(new ResourceLeaseTimelineEntry(
            lease.TaskId,
            operationType,
            stageType,
            lease.Resource.ResourceId,
            lease.RequestedAtMs,
            lease.AcquiredAtMs,
            finishedAtMs));
    }
}

public sealed class ResourceLeaseTraceContext
{
    public ResourceLeaseTraceContext(
        ResourceLeaseTraceCollector collector,
        string operationType,
        string stageType)
    {
        Collector = collector ?? throw new ArgumentNullException(nameof(collector));

        if (string.IsNullOrWhiteSpace(operationType))
        {
            throw new DomainRuleViolationException(
                "Resource lease trace operation type cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(stageType))
        {
            throw new DomainRuleViolationException(
                "Resource lease trace stage type cannot be empty.");
        }

        OperationType = operationType;
        StageType = stageType;
    }

    internal ResourceLeaseTraceCollector Collector { get; }

    internal string OperationType { get; }

    internal string StageType { get; }
}
