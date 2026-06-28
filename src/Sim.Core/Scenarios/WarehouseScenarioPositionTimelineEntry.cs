using Sim.Core.Domain;

namespace Sim.Core.Scenarios;

public sealed record WarehouseScenarioPositionTimelineEntry
{
    public WarehouseScenarioPositionTimelineEntry(
        string operationId,
        string operationType,
        string stageType,
        string resourceId,
        long atMs,
        string eventType,
        WarehouseScenarioPositionPoint position)
    {
        EnsureNotEmpty(operationId, nameof(operationId));
        EnsureNotEmpty(operationType, nameof(operationType));
        EnsureNotEmpty(stageType, nameof(stageType));
        EnsureNotEmpty(resourceId, nameof(resourceId));

        if (atMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Scenario position timeline time cannot be negative. AtMs: {atMs}.");
        }

        if (eventType is not ("start" or "finish"))
        {
            throw new DomainRuleViolationException(
                $"Scenario position timeline event type must be start or finish. EventType: {eventType}.");
        }

        OperationId = operationId;
        OperationType = operationType;
        StageType = stageType;
        ResourceId = resourceId;
        AtMs = atMs;
        EventType = eventType;
        Position = position ?? throw new DomainRuleViolationException(
            "Scenario position timeline position cannot be null.");
    }

    public string OperationId { get; }

    public string OperationType { get; }

    public string StageType { get; }

    public string ResourceId { get; }

    public long AtMs { get; }

    public string EventType { get; }

    public WarehouseScenarioPositionPoint Position { get; }

    private static void EnsureNotEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleViolationException(
                $"Scenario position timeline {name} cannot be empty.");
        }
    }
}
