namespace Sim.Contracts.Artifacts;

public sealed record RunArtifactKpiBottleneck
{
    public int Rank { get; init; }

    public required string ResourceId { get; init; }

    public required string ResourceType { get; init; }

    public decimal AvgWaitMs { get; init; }

    public decimal TotalWaitMs { get; init; }

    public decimal Utilization { get; init; }
}
