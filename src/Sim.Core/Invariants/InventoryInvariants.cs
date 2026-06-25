using Sim.Core.Domain;

namespace Sim.Core.Invariants;

public static class InventoryInvariants
{
    public static void EnsureNonNegative(decimal quantity)
    {
        if (quantity < 0)
        {
            throw new DomainRuleViolationException(
                $"Inventory quantity must be non-negative. Quantity: {quantity}.");
        }
    }

    public static void EnsureConserved(decimal beforeTotal, decimal afterTotal)
    {
        if (beforeTotal != afterTotal)
        {
            throw new DomainRuleViolationException(
                $"Inventory total must be conserved. Before: {beforeTotal}, after: {afterTotal}.");
        }
    }
}
