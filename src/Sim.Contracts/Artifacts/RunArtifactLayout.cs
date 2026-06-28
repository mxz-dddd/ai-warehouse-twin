namespace Sim.Contracts.Artifacts;

public sealed record RunArtifactLayout
{
    public static RunArtifactLayout Empty { get; } = new()
    {
        Resources = Array.Empty<RunArtifactLayoutResource>(),
    };

    public IReadOnlyList<RunArtifactLayoutResource> Resources { get; init; } =
        Array.Empty<RunArtifactLayoutResource>();
}
