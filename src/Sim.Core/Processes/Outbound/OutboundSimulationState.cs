using Sim.Core.Domain;
using Sim.Core.Invariants;
using Sim.Core.Resources;

namespace Sim.Core.Processes.Outbound;

public sealed class OutboundSimulationState
{
    private readonly Dictionary<string, OutboundOrder> _orders;
    private readonly Dictionary<string, string> _requestIdToOrderId = [];
    private readonly Dictionary<string, string> _inventoryUnitIdByOrderId = [];
    private readonly Dictionary<string, ResourceLease> _workerLeasesByOrderId = [];
    private readonly Dictionary<string, ResourceLease> _dockLeasesByOrderId = [];
    private readonly HashSet<string> _completedOrderIds = [];
    private readonly Dictionary<string, long> _startedAtByOrderId = [];
    private readonly Dictionary<string, long> _completedAtByOrderId = [];

    public OutboundSimulationState(
        IEnumerable<OutboundOrder> orders,
        IEnumerable<OutboundInventoryItem> initialInventory,
        ResourcePool workerPool,
        ResourcePool dockPool,
        OutboundProcessParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentNullException.ThrowIfNull(initialInventory);

        _orders = orders.ToDictionary(order => order.OrderId);
        if (_orders.Count == 0)
        {
            throw new DomainRuleViolationException("OutboundSimulationState requires at least one order.");
        }

        InventoryItems = initialInventory.ToDictionary(item => item.InventoryUnitId);
        if (InventoryItems.Count == 0)
        {
            throw new DomainRuleViolationException("OutboundSimulationState requires at least one inventory item.");
        }

        WorkerPool = workerPool ?? throw new ArgumentNullException(nameof(workerPool));
        DockPool = dockPool ?? throw new ArgumentNullException(nameof(dockPool));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    public IReadOnlyDictionary<string, OutboundOrder> Orders => _orders;

    public Dictionary<string, OutboundInventoryItem> InventoryItems { get; }

    public ResourcePool WorkerPool { get; }

    public ResourcePool DockPool { get; }

    public OutboundProcessParameters Parameters { get; }

    public IReadOnlySet<string> CompletedOrderIds => _completedOrderIds;

    public IReadOnlyDictionary<string, long> StartedAtByOrderId => _startedAtByOrderId;

    public IReadOnlyDictionary<string, long> CompletedAtByOrderId => _completedAtByOrderId;

    public string WorkerRequestId(string orderId) => $"worker.{orderId}";

    public string DockRequestId(string orderId) => $"dock.{orderId}";

    public void RegisterRequest(string requestId, string orderId)
    {
        if (!_orders.ContainsKey(orderId))
        {
            throw new DomainRuleViolationException($"Unknown order id. OrderId: {orderId}.");
        }

        _requestIdToOrderId[requestId] = orderId;
    }

    public string OrderIdForRequest(string requestId)
    {
        if (_requestIdToOrderId.TryGetValue(requestId, out var orderId))
        {
            return orderId;
        }

        throw new DomainRuleViolationException($"Unknown outbound resource request. RequestId: {requestId}.");
    }

    public void MarkStarted(string orderId, long startedAtMs)
    {
        _startedAtByOrderId.TryAdd(orderId, startedAtMs);
    }

    public void AllocateInventory(string orderId)
    {
        var order = Orders[orderId];
        var candidate = InventoryItems.Values
            .Where(item =>
                item.SkuId == order.SkuId &&
                item.LocationId == order.SourceLocationId &&
                item.Status == InventoryStatus.Available &&
                item.Quantity == order.Quantity)
            .OrderBy(item => item.InventoryUnitId)
            .FirstOrDefault();

        if (candidate is null)
        {
            throw new DomainRuleViolationException(
                $"Insufficient AVAILABLE inventory for order {orderId}. SkuId: {order.SkuId}, Quantity: {order.Quantity}.");
        }

        InventoryInvariants.EnsureNonNegative(candidate.Quantity - order.Quantity);
        InventoryStateMachine.EnsureCanTransition(candidate.Status, InventoryStatus.Allocated);
        InventoryItems[candidate.InventoryUnitId] = Replace(candidate, candidate.LocationId, InventoryStatus.Allocated);
        _inventoryUnitIdByOrderId[orderId] = candidate.InventoryUnitId;
    }

    public void MarkPicking(string orderId)
    {
        Transition(orderId, InventoryStatus.Allocated, InventoryStatus.Picking, CurrentItem(orderId).LocationId);
    }

    public void MarkPickedAndStaged(string orderId)
    {
        var order = Orders[orderId];
        Transition(orderId, InventoryStatus.Picking, InventoryStatus.Picked, CurrentItem(orderId).LocationId);
        Transition(orderId, InventoryStatus.Picked, InventoryStatus.Staged, order.StagingLocationId);
    }

    public void MarkShipped(string orderId, long completedAtMs)
    {
        var order = Orders[orderId];
        Transition(orderId, InventoryStatus.Staged, InventoryStatus.Loaded, order.DockLocationId);
        Transition(orderId, InventoryStatus.Loaded, InventoryStatus.Shipped, order.DockLocationId);
        _completedOrderIds.Add(orderId);
        _completedAtByOrderId[orderId] = completedAtMs;
    }

    public void StoreWorkerLease(string orderId, ResourceLease lease)
    {
        _workerLeasesByOrderId[orderId] = lease;
    }

    public ResourceLease TakeWorkerLease(string orderId)
    {
        return TakeLease(_workerLeasesByOrderId, orderId, "worker");
    }

    public void StoreDockLease(string orderId, ResourceLease lease)
    {
        _dockLeasesByOrderId[orderId] = lease;
    }

    public ResourceLease TakeDockLease(string orderId)
    {
        return TakeLease(_dockLeasesByOrderId, orderId, "dock");
    }

    public decimal TotalShippedQuantity()
    {
        return InventoryItems.Values
            .Where(item => item.Status == InventoryStatus.Shipped)
            .Sum(item => item.Quantity);
    }

    public OutboundInventoryItem InventoryForOrder(string orderId)
    {
        return CurrentItem(orderId);
    }

    private void Transition(
        string orderId,
        InventoryStatus expectedFrom,
        InventoryStatus to,
        string locationId)
    {
        var current = CurrentItem(orderId);
        if (current.Status != expectedFrom)
        {
            throw new DomainRuleViolationException(
                $"Unexpected inventory status for order {orderId}. Expected: {expectedFrom}, Actual: {current.Status}.");
        }

        InventoryStateMachine.EnsureCanTransition(current.Status, to);
        InventoryInvariants.EnsureConserved(current.Quantity, Orders[orderId].Quantity);
        InventoryItems[current.InventoryUnitId] = Replace(current, locationId, to);
    }

    private OutboundInventoryItem CurrentItem(string orderId)
    {
        if (!_inventoryUnitIdByOrderId.TryGetValue(orderId, out var inventoryUnitId))
        {
            throw new DomainRuleViolationException($"Order has no allocated inventory. OrderId: {orderId}.");
        }

        return InventoryItems[inventoryUnitId];
    }

    private static OutboundInventoryItem Replace(
        OutboundInventoryItem item,
        string locationId,
        InventoryStatus status)
    {
        return new OutboundInventoryItem(
            item.InventoryUnitId,
            item.SkuId,
            item.Quantity,
            locationId,
            status);
    }

    private static ResourceLease TakeLease(
        Dictionary<string, ResourceLease> leasesByOrderId,
        string orderId,
        string resourceName)
    {
        if (leasesByOrderId.Remove(orderId, out var lease))
        {
            return lease;
        }

        throw new DomainRuleViolationException(
            $"Order does not have an active {resourceName} lease. OrderId: {orderId}.");
    }
}
