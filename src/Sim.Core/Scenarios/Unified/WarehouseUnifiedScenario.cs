using System.Collections.ObjectModel;
using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Unified;

public sealed record WarehouseUnifiedScenario
{
    public WarehouseUnifiedScenario(
        string scenarioId,
        int seed,
        IReadOnlyDictionary<string, decimal> initialInventory,
        IEnumerable<WarehouseUnifiedOperation> operations)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new DomainRuleViolationException(
                "Warehouse unified scenario ScenarioId cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(initialInventory);
        ArgumentNullException.ThrowIfNull(operations);

        var operationList = operations.ToArray();
        if (operationList.Length == 0)
        {
            throw new DomainRuleViolationException(
                "Warehouse unified scenario requires at least one operation.");
        }

        ScenarioId = scenarioId;
        Seed = seed;
        InitialInventory = new ReadOnlyDictionary<string, decimal>(
            new SortedDictionary<string, decimal>(
                initialInventory.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value,
                    StringComparer.Ordinal),
                StringComparer.Ordinal));
        Operations = operationList;
    }

    public string ScenarioId { get; }

    public int Seed { get; }

    public IReadOnlyDictionary<string, decimal> InitialInventory { get; }

    public IReadOnlyList<WarehouseUnifiedOperation> Operations { get; }
}
