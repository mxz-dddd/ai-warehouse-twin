using Sim.Core.Domain;
using Sim.Core.Processes.Inbound;

namespace Sim.Core.Scenarios;

public sealed record InboundScenario
{
    public InboundScenario(
        string scenarioId,
        int seed,
        IReadOnlyList<InboundReceipt> receipts,
        InboundProcessParameters parameters,
        int dockCount,
        int forkliftCount)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new DomainRuleViolationException("InboundScenario ScenarioId cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(receipts);
        if (receipts.Count == 0)
        {
            throw new DomainRuleViolationException("InboundScenario requires at least one receipt.");
        }

        if (dockCount <= 0)
        {
            throw new DomainRuleViolationException($"DockCount must be greater than zero. DockCount: {dockCount}.");
        }

        if (forkliftCount <= 0)
        {
            throw new DomainRuleViolationException($"ForkliftCount must be greater than zero. ForkliftCount: {forkliftCount}.");
        }

        ScenarioId = scenarioId;
        Seed = seed;
        Receipts = receipts;
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        DockCount = dockCount;
        ForkliftCount = forkliftCount;
    }

    public string ScenarioId { get; }

    public int Seed { get; }

    public IReadOnlyList<InboundReceipt> Receipts { get; }

    public InboundProcessParameters Parameters { get; }

    public int DockCount { get; }

    public int ForkliftCount { get; }
}
