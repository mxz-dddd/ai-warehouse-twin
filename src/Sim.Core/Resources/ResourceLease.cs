using Sim.Core.Domain;

namespace Sim.Core.Resources;

public sealed record ResourceLease
{
    public ResourceLease(
        ResourceUnit resource,
        string requestId,
        string taskId,
        long acquiredAtMs,
        long? requestedAtMs = null)
    {
        Resource = resource ?? throw new DomainRuleViolationException("ResourceLease Resource cannot be null.");

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new DomainRuleViolationException("ResourceLease RequestId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(taskId))
        {
            throw new DomainRuleViolationException("ResourceLease TaskId cannot be empty.");
        }

        if (acquiredAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"ResourceLease AcquiredAtMs cannot be negative. AcquiredAtMs: {acquiredAtMs}.");
        }

        var actualRequestedAtMs = requestedAtMs ?? acquiredAtMs;
        if (actualRequestedAtMs < 0 || actualRequestedAtMs > acquiredAtMs)
        {
            throw new DomainRuleViolationException(
                $"Resource lease requested time must be non-negative and no later than acquisition. RequestedAtMs: {actualRequestedAtMs}, AcquiredAtMs: {acquiredAtMs}.");
        }

        RequestId = requestId;
        TaskId = taskId;
        AcquiredAtMs = acquiredAtMs;
        RequestedAtMs = actualRequestedAtMs;
    }

    public ResourceUnit Resource { get; }

    public string RequestId { get; }

    public string TaskId { get; }

    public long AcquiredAtMs { get; }

    public long RequestedAtMs { get; }
}
