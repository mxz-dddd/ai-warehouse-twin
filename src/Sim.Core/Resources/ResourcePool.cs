using Sim.Core.Domain;

namespace Sim.Core.Resources;

public sealed class ResourcePool
{
    private readonly Dictionary<string, ResourceUnit> _resourcesById;
    private readonly Queue<ResourceRequest> _waitingQueue = new();
    private readonly HashSet<string> _waitingRequestIds = [];
    private readonly Dictionary<string, ResourceLease> _busyLeasesByResourceId = [];
    private readonly Dictionary<string, long> _completedBusyTimeByResourceId = [];
    private readonly SortedSet<string> _availableResourceIds =
        new(StringComparer.Ordinal);
    private readonly ResourceLeaseTraceContext? _traceContext;

    public ResourcePool(
        string poolId,
        ResourceType resourceType,
        IEnumerable<ResourceUnit> resources,
        ResourceLeaseTraceContext? traceContext = null)
    {
        if (string.IsNullOrWhiteSpace(poolId))
        {
            throw new DomainRuleViolationException("ResourcePool PoolId cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(resources);

        var resourceList = resources.ToList();
        if (resourceList.Count == 0)
        {
            throw new DomainRuleViolationException("ResourcePool must contain at least one resource.");
        }

        PoolId = poolId;
        ResourceType = resourceType;
        _traceContext = traceContext;
        _resourcesById = new Dictionary<string, ResourceUnit>();

        foreach (var resource in resourceList)
        {
            ArgumentNullException.ThrowIfNull(resource);

            if (resource.ResourceType != resourceType)
            {
                throw new DomainRuleViolationException(
                    $"Resource {resource.ResourceId} has type {resource.ResourceType}, expected {resourceType}.");
            }

            if (!_resourcesById.TryAdd(resource.ResourceId, resource))
            {
                throw new DomainRuleViolationException(
                    $"Duplicate resource id in pool {poolId}. ResourceId: {resource.ResourceId}.");
            }

            _completedBusyTimeByResourceId[resource.ResourceId] = 0;
            _availableResourceIds.Add(resource.ResourceId);
        }
    }

    public string PoolId { get; }

    public ResourceType ResourceType { get; }

    public int Capacity => _resourcesById.Count;

    public int AvailableCount => _availableResourceIds.Count;

    public int BusyCount => _busyLeasesByResourceId.Count;

    public int WaitingCount => _waitingQueue.Count;

    public ResourceLease? TryAcquire(ResourceRequest request, long nowMs)
    {
        ValidateRequestAndTime(request, nowMs);

        if (_availableResourceIds.Count == 0)
        {
            return null;
        }

        var resourceId = _availableResourceIds.Min!;
        _availableResourceIds.Remove(resourceId);
        var resource = _resourcesById[resourceId];

        return Acquire(resource, request, nowMs);
    }

    public ResourceLease? AcquireOrQueue(ResourceRequest request, long nowMs)
    {
        ValidateRequestAndTime(request, nowMs);

        var lease = TryAcquire(request, nowMs);
        if (lease is not null)
        {
            return lease;
        }

        if (!_waitingRequestIds.Add(request.RequestId))
        {
            throw new DomainRuleViolationException(
                $"Resource request is already waiting in pool {PoolId}. RequestId: {request.RequestId}.");
        }

        _waitingQueue.Enqueue(request);
        return null;
    }

    public ResourceLease? Release(ResourceLease lease, long nowMs)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ValidateTime(nowMs);

        if (nowMs < lease.AcquiredAtMs)
        {
            throw new DomainRuleViolationException(
                $"Resource release time cannot be earlier than acquisition time. AcquiredAtMs: {lease.AcquiredAtMs}, ReleaseAtMs: {nowMs}.");
        }

        if (!_resourcesById.ContainsKey(lease.Resource.ResourceId))
        {
            throw new DomainRuleViolationException(
                $"Resource {lease.Resource.ResourceId} does not belong to pool {PoolId}.");
        }

        if (!_busyLeasesByResourceId.TryGetValue(lease.Resource.ResourceId, out var activeLease))
        {
            throw new DomainRuleViolationException(
                $"Resource {lease.Resource.ResourceId} is not currently busy in pool {PoolId}.");
        }

        if (activeLease.RequestId != lease.RequestId || activeLease.TaskId != lease.TaskId)
        {
            throw new DomainRuleViolationException(
                $"Lease does not match active lease for resource {lease.Resource.ResourceId} in pool {PoolId}.");
        }

        _busyLeasesByResourceId.Remove(lease.Resource.ResourceId);
        _completedBusyTimeByResourceId[lease.Resource.ResourceId] += nowMs - lease.AcquiredAtMs;
        _traceContext?.Collector.Record(
            lease,
            nowMs,
            _traceContext.OperationType,
            _traceContext.StageType);

        if (_waitingQueue.TryDequeue(out var nextRequest))
        {
            _waitingRequestIds.Remove(nextRequest.RequestId);
            return Acquire(lease.Resource, nextRequest, nowMs);
        }

        _availableResourceIds.Add(lease.Resource.ResourceId);
        return null;
    }

    public ResourcePoolSnapshot Snapshot(long nowMs)
    {
        ValidateTime(nowMs);

        var totalBusyTimeMs = CalculateTotalBusyTime(nowMs);
        var utilization = nowMs == 0
            ? 0m
            : totalBusyTimeMs / (decimal)(Capacity * nowMs);

        if (utilization < 0m)
        {
            utilization = 0m;
        }
        else if (utilization > 1m)
        {
            utilization = 1m;
        }

        return new ResourcePoolSnapshot(
            PoolId,
            ResourceType,
            Capacity,
            AvailableCount,
            BusyCount,
            WaitingCount,
            utilization,
            totalBusyTimeMs);
    }

    private ResourceLease Acquire(ResourceUnit resource, ResourceRequest request, long nowMs)
    {
        var lease = new ResourceLease(
            resource,
            request.RequestId,
            request.TaskId,
            nowMs,
            request.RequestedAtMs);
        _busyLeasesByResourceId.Add(resource.ResourceId, lease);
        return lease;
    }

    private long CalculateTotalBusyTime(long nowMs)
    {
        var total = _completedBusyTimeByResourceId.Values.Sum();
        foreach (var lease in _busyLeasesByResourceId.Values)
        {
            if (nowMs < lease.AcquiredAtMs)
            {
                throw new DomainRuleViolationException(
                    $"Snapshot time cannot be earlier than active lease acquisition time. AcquiredAtMs: {lease.AcquiredAtMs}, NowMs: {nowMs}.");
            }

            total += nowMs - lease.AcquiredAtMs;
        }

        return total;
    }

    private static void ValidateRequestAndTime(ResourceRequest request, long nowMs)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTime(nowMs);
    }

    private static void ValidateTime(long nowMs)
    {
        if (nowMs < 0)
        {
            throw new DomainRuleViolationException(
                $"ResourcePool time cannot be negative. NowMs: {nowMs}.");
        }
    }
}
