using Sim.Core.Domain;

namespace Sim.Core.Movement;

public sealed record MovementArtifactInputAdapterOptions
{
    public MovementArtifactInputAdapterOptions(
        string sourceRunArtifact,
        string graphSource,
        string generatorVersion,
        string? runId = null)
    {
        if (string.IsNullOrWhiteSpace(sourceRunArtifact))
        {
            throw new DomainRuleViolationException(
                "MovementArtifact input adapter SourceRunArtifact cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(graphSource))
        {
            throw new DomainRuleViolationException(
                "MovementArtifact input adapter GraphSource cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(generatorVersion))
        {
            throw new DomainRuleViolationException(
                "MovementArtifact input adapter GeneratorVersion cannot be empty.");
        }

        SourceRunArtifact = sourceRunArtifact;
        GraphSource = graphSource;
        GeneratorVersion = generatorVersion;
        RunId = runId;
    }

    public string SourceRunArtifact { get; }

    public string GraphSource { get; }

    public string GeneratorVersion { get; }

    public string? RunId { get; }
}
