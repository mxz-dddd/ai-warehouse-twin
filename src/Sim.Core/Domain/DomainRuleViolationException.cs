namespace Sim.Core.Domain;

public sealed class DomainRuleViolationException : InvalidOperationException
{
    public DomainRuleViolationException(string message)
        : base(message)
    {
    }
}
