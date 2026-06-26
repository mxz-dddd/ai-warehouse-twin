using Sim.Core.Domain;

namespace Sim.Core.Processes.EachPick;

public sealed record Tote
{
    public Tote(string toteId, string orderId, string status)
    {
        if (string.IsNullOrWhiteSpace(toteId))
        {
            throw new DomainRuleViolationException("Tote ToteId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new DomainRuleViolationException("Tote OrderId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new DomainRuleViolationException("Tote Status cannot be empty.");
        }

        ToteId = toteId;
        OrderId = orderId;
        Status = status;
    }

    public string ToteId { get; }

    public string OrderId { get; }

    public string Status { get; }
}
