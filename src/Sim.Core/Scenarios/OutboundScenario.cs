using Sim.Core.Domain;
using Sim.Core.Processes.Outbound;

namespace Sim.Core.Scenarios;

public sealed record OutboundScenario
{
    public OutboundScenario(
        string scenarioId,
        int seed,
        IReadOnlyList<OutboundOrder> orders,
        IReadOnlyList<OutboundInventoryItem> initialInventory,
        OutboundProcessParameters parameters,
        int workerCount,
        int dockCount)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new DomainRuleViolationException("OutboundScenario ScenarioId cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(orders);
        if (orders.Count == 0)
        {
            throw new DomainRuleViolationException("OutboundScenario requires at least one order.");
        }

        ArgumentNullException.ThrowIfNull(initialInventory);
        if (initialInventory.Count == 0)
        {
            throw new DomainRuleViolationException("OutboundScenario requires at least one inventory item.");
        }

        if (workerCount <= 0)
        {
            throw new DomainRuleViolationException($"WorkerCount must be greater than zero. WorkerCount: {workerCount}.");
        }

        if (dockCount <= 0)
        {
            throw new DomainRuleViolationException($"DockCount must be greater than zero. DockCount: {dockCount}.");
        }

        ScenarioId = scenarioId;
        Seed = seed;
        Orders = orders;
        InitialInventory = initialInventory;
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        WorkerCount = workerCount;
        DockCount = dockCount;
    }

    public string ScenarioId { get; }

    public int Seed { get; }

    public IReadOnlyList<OutboundOrder> Orders { get; }

    public IReadOnlyList<OutboundInventoryItem> InitialInventory { get; }

    public OutboundProcessParameters Parameters { get; }

    public int WorkerCount { get; }

    public int DockCount { get; }
}
