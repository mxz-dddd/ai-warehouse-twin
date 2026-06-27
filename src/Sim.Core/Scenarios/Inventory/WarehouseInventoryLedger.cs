using System.Collections.ObjectModel;
using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Inventory;

public sealed class WarehouseInventoryLedger
{
    private readonly Dictionary<string, InventoryQuantity> _availableBySku =
        new(StringComparer.Ordinal);

    public void Add(string skuId, decimal quantity)
    {
        ValidateSkuId(skuId);
        ValidatePositiveQuantity(quantity);

        var addedQuantity = new InventoryQuantity(quantity);
        _availableBySku[skuId] = _availableBySku.TryGetValue(skuId, out var current)
            ? current.Add(addedQuantity)
            : addedQuantity;
    }

    public void Remove(string skuId, decimal quantity)
    {
        ValidateSkuId(skuId);
        ValidatePositiveQuantity(quantity);

        var current = _availableBySku.TryGetValue(skuId, out var available)
            ? available
            : new InventoryQuantity(0m);

        _availableBySku[skuId] = current.Subtract(new InventoryQuantity(quantity));
    }

    public decimal GetAvailable(string skuId)
    {
        ValidateSkuId(skuId);

        return _availableBySku.TryGetValue(skuId, out var quantity)
            ? quantity.Value
            : 0m;
    }

    public IReadOnlyDictionary<string, decimal> Snapshot()
    {
        var snapshot = new SortedDictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var entry in _availableBySku)
        {
            snapshot.Add(entry.Key, entry.Value.Value);
        }

        return new ReadOnlyDictionary<string, decimal>(snapshot);
    }

    private static void ValidateSkuId(string skuId)
    {
        if (string.IsNullOrWhiteSpace(skuId))
        {
            throw new DomainRuleViolationException(
                "Warehouse inventory ledger SkuId cannot be empty.");
        }
    }

    private static void ValidatePositiveQuantity(decimal quantity)
    {
        if (quantity <= 0m)
        {
            throw new DomainRuleViolationException(
                $"Warehouse inventory ledger quantity must be greater than zero. Quantity: {quantity}.");
        }
    }
}
