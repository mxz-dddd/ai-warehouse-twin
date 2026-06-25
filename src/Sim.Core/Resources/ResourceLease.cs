using Sim.Core.Domain;

namespace Sim.Core.Resources;

public sealed record ResourceLease
{
    public ResourceLease(ResourceUnit resource, string requestId, string taskId, long acquiredAtMs)
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

        RequestId = requestId;
        TaskId = taskId;
        AcquiredAtMs = acquiredAtMs;
    }

    public ResourceUnit Resource { get; }

    public string RequestId { get; }

    public string TaskId { get; }

    public long AcquiredAtMs { get; }
}
