namespace Sim.Contracts.Artifacts;

public sealed record RunArtifactKpiSummary
{
    public long TotalDurationMs { get; init; }

    public int TotalCompletedWorkItems { get; init; }

    public int EventLogLineCount { get; init; }

    public decimal ReceiptThroughputPerHour { get; init; }

    public decimal OutboundOrderThroughputPerHour { get; init; }

    public decimal EachPickOrderThroughputPerHour { get; init; }

    public decimal TotalWorkItemThroughputPerHour { get; init; }
}
