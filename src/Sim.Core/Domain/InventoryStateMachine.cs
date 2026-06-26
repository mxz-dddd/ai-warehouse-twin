namespace Sim.Core.Domain;

public static class InventoryStateMachine
{
    private static readonly HashSet<(InventoryStatus From, InventoryStatus To)> AllowedTransitions = new()
    {
        (InventoryStatus.Expected, InventoryStatus.Received),
        (InventoryStatus.Received, InventoryStatus.QcHold),
        (InventoryStatus.Received, InventoryStatus.Available),
        (InventoryStatus.QcHold, InventoryStatus.Available),
        (InventoryStatus.Available, InventoryStatus.Allocated),
        (InventoryStatus.Allocated, InventoryStatus.Picking),
        (InventoryStatus.Picking, InventoryStatus.Picked),
        (InventoryStatus.Picked, InventoryStatus.Staged),
        (InventoryStatus.Picked, InventoryStatus.Consolidating),
        (InventoryStatus.Consolidating, InventoryStatus.Staged),
        (InventoryStatus.Staged, InventoryStatus.Loaded),
        (InventoryStatus.Loaded, InventoryStatus.Shipped),
    };

    public static bool CanTransition(InventoryStatus from, InventoryStatus to)
    {
        return AllowedTransitions.Contains((from, to));
    }

    public static void EnsureCanTransition(InventoryStatus from, InventoryStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new DomainRuleViolationException(
                $"Invalid inventory status transition from {from} to {to}.");
        }
    }
}
