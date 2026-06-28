namespace Sim.Contracts.Artifacts;

public sealed record RunArtifactPositionTimelineEntry
{
    public RunArtifactPositionTimelineEntry(
        string operationId,
        string operationType,
        string stageType,
        string resourceId,
        long atMs,
        string eventType,
        string nodeId,
        decimal x,
        decimal y)
    {
        EnsureNotEmpty(operationId, nameof(operationId));
        EnsureNotEmpty(operationType, nameof(operationType));
        EnsureNotEmpty(stageType, nameof(stageType));
        EnsureNotEmpty(resourceId, nameof(resourceId));
        EnsureNotEmpty(nodeId, nameof(nodeId));

        if (atMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(atMs),
                atMs,
                "RunArtifact position timeline time cannot be negative.");
        }

        if (eventType is not ("start" or "finish"))
        {
            throw new ArgumentException(
                $"RunArtifact position timeline event type must be start or finish. EventType: {eventType}.",
                nameof(eventType));
        }

        OperationId = operationId;
        OperationType = operationType;
        StageType = stageType;
        ResourceId = resourceId;
        AtMs = atMs;
        EventType = eventType;
        NodeId = nodeId;
        X = x;
        Y = y;
    }

    public string OperationId { get; }

    public string OperationType { get; }

    public string StageType { get; }

    public string ResourceId { get; }

    public long AtMs { get; }

    public string EventType { get; }

    public string NodeId { get; }

    public decimal X { get; }

    public decimal Y { get; }

    private static void EnsureNotEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"RunArtifact position timeline {name} cannot be empty.",
                name);
        }
    }
}
