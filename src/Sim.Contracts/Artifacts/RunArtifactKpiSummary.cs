using System.Text.Json.Serialization;

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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? OrderCycleP50Ms { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? OrderCycleP90Ms { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? OrderCycleP95Ms { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? AvgWaitMs { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, decimal>? ResourceUtilization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<RunArtifactKpiBottleneck>? Bottlenecks { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, decimal>? TravelDistanceMByActorType { get; init; }
}
