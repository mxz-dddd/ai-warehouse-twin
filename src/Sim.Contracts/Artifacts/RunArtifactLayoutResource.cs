namespace Sim.Contracts.Artifacts;

public sealed record RunArtifactLayoutResource
{
    public RunArtifactLayoutResource(
        string resourceId,
        string nodeId,
        decimal x,
        decimal y)
    {
        EnsureNotEmpty(resourceId, nameof(resourceId));
        EnsureNotEmpty(nodeId, nameof(nodeId));

        ResourceId = resourceId;
        NodeId = nodeId;
        X = x;
        Y = y;
    }

    public string ResourceId { get; }

    public string NodeId { get; }

    public decimal X { get; }

    public decimal Y { get; }

    private static void EnsureNotEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"RunArtifact layout resource {name} cannot be empty.",
                name);
        }
    }
}
