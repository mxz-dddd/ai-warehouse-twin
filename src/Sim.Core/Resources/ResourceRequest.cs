using Sim.Core.Domain;

namespace Sim.Core.Resources;

public sealed record ResourceRequest
{
    public ResourceRequest(string requestId, string taskId, long requestedAtMs)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new DomainRuleViolationException("ResourceRequest RequestId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(taskId))
        {
            throw new DomainRuleViolationException("ResourceRequest TaskId cannot be empty.");
        }

        if (requestedAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"ResourceRequest RequestedAtMs cannot be negative. RequestedAtMs: {requestedAtMs}.");
        }

        RequestId = requestId;
        TaskId = taskId;
        RequestedAtMs = requestedAtMs;
    }

    public string RequestId { get; }

    public string TaskId { get; }

    public long RequestedAtMs { get; }
}
