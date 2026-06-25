namespace Sim.Core.Domain;

public enum InventoryStatus
{
    Expected,
    Received,
    QcHold,
    Available,
    Allocated,
    Picking,
    Picked,
    Consolidating,
    Staged,
    Loaded,
    Shipped
}
