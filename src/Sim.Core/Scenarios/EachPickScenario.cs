using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;

namespace Sim.Core.Scenarios;

public sealed record EachPickScenario
{
    public EachPickScenario(
        string scenarioId,
        int seed,
        IReadOnlyList<EachPickOrder> orders,
        IReadOnlyList<EachPickInventoryItem> initialInventory,
        EachPickProcessParameters parameters,
        int stationCount,
        int workerCount)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new DomainRuleViolationException("EachPickScenario ScenarioId cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(orders);
        if (orders.Count == 0)
        {
            throw new DomainRuleViolationException("EachPickScenario requires at least one order.");
        }

        ArgumentNullException.ThrowIfNull(initialInventory);
        if (initialInventory.Count == 0)
        {
            throw new DomainRuleViolationException("EachPickScenario requires at least one inventory item.");
        }

        if (stationCount <= 0)
        {
            throw new DomainRuleViolationException(
                $"StationCount must be greater than zero. StationCount: {stationCount}.");
        }

        if (workerCount <= 0)
        {
            throw new DomainRuleViolationException(
                $"WorkerCount must be greater than zero. WorkerCount: {workerCount}.");
        }

        ScenarioId = scenarioId;
        Seed = seed;
        Orders = orders;
        InitialInventory = initialInventory;
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        StationCount = stationCount;
        WorkerCount = workerCount;
    }

    public string ScenarioId { get; }

    public int Seed { get; }

    public IReadOnlyList<EachPickOrder> Orders { get; }

    public IReadOnlyList<EachPickInventoryItem> InitialInventory { get; }

    public EachPickProcessParameters Parameters { get; }

    public int StationCount { get; }

    public int WorkerCount { get; }
}
