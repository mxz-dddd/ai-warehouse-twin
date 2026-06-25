using Sim.Core.Domain;

namespace Sim.Core.Resources;

public sealed record ResourceUnit
{
    public ResourceUnit(string resourceId, ResourceType resourceType, string name)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new DomainRuleViolationException("ResourceUnit ResourceId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleViolationException("ResourceUnit Name cannot be empty.");
        }

        ResourceId = resourceId;
        ResourceType = resourceType;
        Name = name;
    }

    public string ResourceId { get; }

    public ResourceType ResourceType { get; }

    public string Name { get; }
}
