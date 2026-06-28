using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Unified;

public sealed record WarehouseUnifiedPositionTimelineEntry
{
    public WarehouseUnifiedPositionTimelineEntry(
        string operationId,
        string resourceId,
        long atMs,
        WarehouseUnifiedPositionPoint position,
        string eventType)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            throw new DomainRuleViolationException(
                "Unified position timeline operation id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException(
                "Unified position timeline resource id cannot be empty.");
        }

        if (atMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Unified position timeline time cannot be negative. AtMs: {atMs}.");
        }

        if (eventType is not ("start" or "finish"))
        {
            throw new DomainRuleViolationException(
                $"Unified position timeline event type must be start or finish. EventType: {eventType}.");
        }

        OperationId = operationId;
        ResourceId = resourceId;
        AtMs = atMs;
        Position = position ?? throw new DomainRuleViolationException(
            "Unified position timeline position cannot be null.");
        EventType = eventType;
    }

    public string OperationId { get; }

    public string ResourceId { get; }

    public long AtMs { get; }

    public WarehouseUnifiedPositionPoint Position { get; }

    public string EventType { get; }
}
