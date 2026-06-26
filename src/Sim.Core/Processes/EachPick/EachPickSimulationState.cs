using Sim.Core.Domain;
using Sim.Core.Resources;

namespace Sim.Core.Processes.EachPick;

public sealed class EachPickSimulationState
{
    private readonly Dictionary<string, EachPickOrder> _orders;
    private readonly Dictionary<string, EachPickInventoryItem> _inventory;
    private readonly Dictionary<string, Tote> _totes = [];
    private readonly HashSet<string> _completedOrderIds = [];
    private readonly Dictionary<string, long> _startedAtMsByOrderId = [];
    private readonly Dictionary<string, long> _completedAtMsByOrderId = [];

    public EachPickSimulationState(
        IReadOnlyList<EachPickOrder> orders,
        IReadOnlyList<EachPickInventoryItem> initialInventory,
        ResourcePool stationPool,
        ResourcePool workerPool,
        EachPickProcessParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentNullException.ThrowIfNull(initialInventory);

        if (orders.Count == 0)
        {
            throw new DomainRuleViolationException("EachPickSimulationState requires at least one order.");
        }

        if (initialInventory.Count == 0)
        {
            throw new DomainRuleViolationException("EachPickSimulationState requires at least one inventory item.");
        }

        StationPool = stationPool ?? throw new ArgumentNullException(nameof(stationPool));
        WorkerPool = workerPool ?? throw new ArgumentNullException(nameof(workerPool));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

        _orders = new Dictionary<string, EachPickOrder>();
        foreach (var order in orders)
        {
            if (!_orders.TryAdd(order.OrderId, order))
            {
                throw new DomainRuleViolationException(
                    $"Duplicate each-pick order id. OrderId: {order.OrderId}.");
            }
        }

        _inventory = new Dictionary<string, EachPickInventoryItem>();
        foreach (var item in initialInventory)
        {
            if (!_inventory.TryAdd(item.InventoryUnitId, item))
            {
                throw new DomainRuleViolationException(
                    $"Duplicate each-pick inventory unit id. InventoryUnitId: {item.InventoryUnitId}.");
            }
        }
    }

    public IReadOnlyDictionary<string, EachPickOrder> Orders => _orders;

    public IReadOnlyDictionary<string, EachPickInventoryItem> Inventory => _inventory;

    public IReadOnlyDictionary<string, Tote> Totes => _totes;

    public ResourcePool StationPool { get; }

    public ResourcePool WorkerPool { get; }

    public EachPickProcessParameters Parameters { get; }

    public IReadOnlySet<string> CompletedOrderIds => _completedOrderIds;

    public IReadOnlyDictionary<string, long> StartedAtMsByOrderId => _startedAtMsByOrderId;

    public IReadOnlyDictionary<string, long> CompletedAtMsByOrderId => _completedAtMsByOrderId;

    public EachPickOrder GetOrder(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new DomainRuleViolationException("Each-pick order id cannot be empty.");
        }

        if (_orders.TryGetValue(orderId, out var order))
        {
            return order;
        }

        throw new DomainRuleViolationException($"Unknown each-pick order id. OrderId: {orderId}.");
    }

    public EachPickInventoryItem GetInventory(string inventoryUnitId)
    {
        if (string.IsNullOrWhiteSpace(inventoryUnitId))
        {
            throw new DomainRuleViolationException("Each-pick inventory unit id cannot be empty.");
        }

        if (_inventory.TryGetValue(inventoryUnitId, out var item))
        {
            return item;
        }

        throw new DomainRuleViolationException(
            $"Unknown each-pick inventory unit id. InventoryUnitId: {inventoryUnitId}.");
    }

    public void UpsertInventory(EachPickInventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _inventory[item.InventoryUnitId] = item;
    }

    public void RegisterTote(Tote tote)
    {
        ArgumentNullException.ThrowIfNull(tote);

        if (!_totes.TryAdd(tote.ToteId, tote))
        {
            throw new DomainRuleViolationException($"Duplicate tote id. ToteId: {tote.ToteId}.");
        }
    }

    public void MarkStarted(string orderId, long startedAtMs)
    {
        GetOrder(orderId);
        EnsureNonNegativeTime(startedAtMs, nameof(startedAtMs));

        _startedAtMsByOrderId[orderId] = startedAtMs;
    }

    public void MarkCompleted(string orderId, long completedAtMs)
    {
        GetOrder(orderId);
        EnsureNonNegativeTime(completedAtMs, nameof(completedAtMs));

        if (_startedAtMsByOrderId.TryGetValue(orderId, out var startedAtMs) &&
            completedAtMs < startedAtMs)
        {
            throw new DomainRuleViolationException(
                $"Each-pick completion cannot be earlier than start. OrderId: {orderId}, StartedAtMs: {startedAtMs}, CompletedAtMs: {completedAtMs}.");
        }

        _completedAtMsByOrderId[orderId] = completedAtMs;
        _completedOrderIds.Add(orderId);
    }

    private static void EnsureNonNegativeTime(long timeMs, string name)
    {
        if (timeMs < 0)
        {
            throw new DomainRuleViolationException($"{name} cannot be negative. Value: {timeMs}.");
        }
    }
}
