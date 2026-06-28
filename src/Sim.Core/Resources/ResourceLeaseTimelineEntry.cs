using Sim.Core.Domain;

namespace Sim.Core.Resources;

public sealed record ResourceLeaseTimelineEntry
{
    public ResourceLeaseTimelineEntry(
        string operationId,
        string operationType,
        string stageType,
        string resourceId,
        long requestedAtMs,
        long startedAtMs,
        long finishedAtMs)
    {
        EnsureNotEmpty(operationId, nameof(operationId));
        EnsureNotEmpty(operationType, nameof(operationType));
        EnsureNotEmpty(stageType, nameof(stageType));
        EnsureNotEmpty(resourceId, nameof(resourceId));

        if (requestedAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Resource lease requested time cannot be negative. RequestedAtMs: {requestedAtMs}.");
        }

        if (startedAtMs < requestedAtMs)
        {
            throw new DomainRuleViolationException(
                $"Resource lease start cannot precede its request. RequestedAtMs: {requestedAtMs}, StartedAtMs: {startedAtMs}.");
        }

        if (finishedAtMs <= startedAtMs)
        {
            throw new DomainRuleViolationException(
                $"Resource lease finish must be after its start. StartedAtMs: {startedAtMs}, FinishedAtMs: {finishedAtMs}.");
        }

        OperationId = operationId;
        OperationType = operationType;
        StageType = stageType;
        ResourceId = resourceId;
        RequestedAtMs = requestedAtMs;
        StartedAtMs = startedAtMs;
        FinishedAtMs = finishedAtMs;
    }

    public string OperationId { get; }

    public string OperationType { get; }

    public string StageType { get; }

    public string ResourceId { get; }

    public long RequestedAtMs { get; }

    public long StartedAtMs { get; }

    public long FinishedAtMs { get; }

    public long DurationMs => FinishedAtMs - StartedAtMs;

    private static void EnsureNotEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleViolationException(
                $"Resource lease timeline {name} cannot be empty.");
        }
    }
}
