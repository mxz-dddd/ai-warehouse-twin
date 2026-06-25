using Sim.Core.Domain;
using Sim.Core.Invariants;
using Sim.Core.Resources;

namespace Sim.Core.Processes.Inbound;

public sealed class InboundSimulationState
{
    private readonly Dictionary<string, InboundReceipt> _receipts;
    private readonly Dictionary<string, string> _requestIdToReceiptId = [];
    private readonly Dictionary<string, ResourceLease> _dockLeasesByReceiptId = [];
    private readonly Dictionary<string, ResourceLease> _forkliftLeasesByReceiptId = [];
    private readonly HashSet<string> _completedReceiptIds = [];
    private readonly Dictionary<string, long> _startedAtByReceiptId = [];
    private readonly Dictionary<string, long> _completedAtByReceiptId = [];

    public InboundSimulationState(
        IEnumerable<InboundReceipt> receipts,
        ResourcePool dockPool,
        ResourcePool forkliftPool,
        InboundProcessParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(receipts);

        _receipts = receipts.ToDictionary(receipt => receipt.ReceiptId);
        if (_receipts.Count == 0)
        {
            throw new DomainRuleViolationException("InboundSimulationState requires at least one receipt.");
        }

        DockPool = dockPool ?? throw new ArgumentNullException(nameof(dockPool));
        ForkliftPool = forkliftPool ?? throw new ArgumentNullException(nameof(forkliftPool));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

        InventoryItems = _receipts.Values.ToDictionary(
            receipt => receipt.ReceiptId,
            receipt => new InboundInventoryItem(
                $"inventory.{receipt.ReceiptId}",
                receipt.SkuId,
                receipt.Quantity,
                receipt.StagingLocationId,
                InventoryStatus.Expected));
    }

    public IReadOnlyDictionary<string, InboundReceipt> Receipts => _receipts;

    public Dictionary<string, InboundInventoryItem> InventoryItems { get; }

    public ResourcePool DockPool { get; }

    public ResourcePool ForkliftPool { get; }

    public InboundProcessParameters Parameters { get; }

    public IReadOnlySet<string> CompletedReceiptIds => _completedReceiptIds;

    public IReadOnlyDictionary<string, long> StartedAtByReceiptId => _startedAtByReceiptId;

    public IReadOnlyDictionary<string, long> CompletedAtByReceiptId => _completedAtByReceiptId;

    public string DockRequestId(string receiptId) => $"dock.{receiptId}";

    public string ForkliftRequestId(string receiptId) => $"forklift.{receiptId}";

    public void RegisterRequest(string requestId, string receiptId)
    {
        if (!_receipts.ContainsKey(receiptId))
        {
            throw new DomainRuleViolationException($"Unknown receipt id. ReceiptId: {receiptId}.");
        }

        _requestIdToReceiptId[requestId] = receiptId;
    }

    public string ReceiptIdForRequest(string requestId)
    {
        if (_requestIdToReceiptId.TryGetValue(requestId, out var receiptId))
        {
            return receiptId;
        }

        throw new DomainRuleViolationException($"Unknown inbound resource request. RequestId: {requestId}.");
    }

    public void MarkStarted(string receiptId, long startedAtMs)
    {
        _startedAtByReceiptId.TryAdd(receiptId, startedAtMs);
    }

    public void StoreDockLease(string receiptId, ResourceLease lease)
    {
        _dockLeasesByReceiptId[receiptId] = lease;
    }

    public ResourceLease TakeDockLease(string receiptId)
    {
        return TakeLease(_dockLeasesByReceiptId, receiptId, "dock");
    }

    public void StoreForkliftLease(string receiptId, ResourceLease lease)
    {
        _forkliftLeasesByReceiptId[receiptId] = lease;
    }

    public ResourceLease TakeForkliftLease(string receiptId)
    {
        return TakeLease(_forkliftLeasesByReceiptId, receiptId, "forklift");
    }

    public void MarkReceived(string receiptId)
    {
        var receipt = Receipts[receiptId];
        var current = InventoryItems[receiptId];
        InventoryStateMachine.EnsureCanTransition(current.Status, InventoryStatus.Received);
        InventoryInvariants.EnsureConserved(current.Quantity, receipt.Quantity);

        InventoryItems[receiptId] = new InboundInventoryItem(
            current.InventoryUnitId,
            current.SkuId,
            current.Quantity,
            receipt.StagingLocationId,
            InventoryStatus.Received);
    }

    public void MarkAvailable(string receiptId, long completedAtMs)
    {
        var receipt = Receipts[receiptId];
        var current = InventoryItems[receiptId];
        InventoryStateMachine.EnsureCanTransition(current.Status, InventoryStatus.Available);
        InventoryInvariants.EnsureConserved(current.Quantity, receipt.Quantity);

        InventoryItems[receiptId] = new InboundInventoryItem(
            current.InventoryUnitId,
            current.SkuId,
            current.Quantity,
            receipt.TargetLocationId,
            InventoryStatus.Available);

        _completedReceiptIds.Add(receiptId);
        _completedAtByReceiptId[receiptId] = completedAtMs;
    }

    public decimal TotalAvailableQuantity()
    {
        return InventoryItems.Values
            .Where(item => item.Status == InventoryStatus.Available)
            .Sum(item => item.Quantity);
    }

    private static ResourceLease TakeLease(
        Dictionary<string, ResourceLease> leasesByReceiptId,
        string receiptId,
        string resourceName)
    {
        if (leasesByReceiptId.Remove(receiptId, out var lease))
        {
            return lease;
        }

        throw new DomainRuleViolationException(
            $"Receipt does not have an active {resourceName} lease. ReceiptId: {receiptId}.");
    }
}
