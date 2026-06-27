using System.Globalization;
using Sim.Core.Domain;
using Sim.Core.Scenarios.Inventory;

namespace Sim.Core.Scenarios.Unified;

public sealed class WarehouseUnifiedOperationRunner
{
    public WarehouseUnifiedOperationResult Run(
        IReadOnlyDictionary<string, decimal> initialInventory,
        IEnumerable<WarehouseUnifiedOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(initialInventory);
        ArgumentNullException.ThrowIfNull(operations);

        var ledger = CreateLedger(initialInventory);
        var orderedOperations = operations
            .OrderBy(operation => operation.RequestedAtMs)
            .ThenBy(operation => operation.OperationId, StringComparer.Ordinal)
            .ToArray();

        if (orderedOperations.Length == 0)
        {
            throw new DomainRuleViolationException(
                "Warehouse unified operation runner requires at least one operation.");
        }

        EnsureUniqueOperationIds(orderedOperations);

        var operationsByOwnerId = orderedOperations.ToDictionary(
            ToOwnerId,
            StringComparer.Ordinal);
        var intervals = new List<WarehouseUnifiedOperationInterval>();
        var timelineEvents = new List<TimelineEvent>();

        foreach (var resourceGroup in orderedOperations
                     .GroupBy(operation => operation.ResourceId, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var sequence = 0;
            var resourceTimeline = new WarehouseSharedResourceTimelineRunner()
                .RunCapacityOne(
                    resourceGroup.Key,
                    resourceGroup.Select(operation =>
                        new WarehouseSharedResourceWorkItem(
                            ToFlowName(operation.OperationType),
                            operation.OperationId,
                            operation.ResourceId,
                            operation.RequestedAtMs,
                            operation.DurationMs,
                            sequence++)));

            AddResourceEvents(resourceTimeline.EventLogText, timelineEvents);

            foreach (var allocation in resourceTimeline.Allocations)
            {
                var operation = operationsByOwnerId[allocation.OwnerId];
                intervals.Add(new WarehouseUnifiedOperationInterval(
                    operation.OperationId,
                    operation.OperationType,
                    allocation.ResourceId,
                    allocation.StartedAtMs,
                    allocation.FinishedAtMs));
            }
        }

        ApplyInventoryMutations(
            ledger,
            orderedOperations,
            intervals,
            timelineEvents);

        var orderedIntervals = intervals
            .OrderBy(interval => interval.StartedAtMs)
            .ThenBy(interval => interval.OperationId, StringComparer.Ordinal)
            .ToArray();

        var eventLogText = string.Join(
            "\n",
            timelineEvents
                .OrderBy(entry => entry.OccurredAtMs)
                .ThenBy(entry => entry.Phase)
                .ThenBy(entry => entry.Text, StringComparer.Ordinal)
                .Select(entry => entry.Text));

        var operationTelemetry = BuildOperationTelemetry(
            orderedOperations,
            orderedIntervals);

        return new WarehouseUnifiedOperationResult(
            orderedIntervals,
            operationTelemetry,
            ledger.Snapshot(),
            eventLogText);
    }

    private static WarehouseInventoryLedger CreateLedger(
        IReadOnlyDictionary<string, decimal> initialInventory)
    {
        var ledger = new WarehouseInventoryLedger();

        foreach (var entry in initialInventory.OrderBy(
                     entry => entry.Key,
                     StringComparer.Ordinal))
        {
            ledger.Add(entry.Key, entry.Value);
        }

        return ledger;
    }

    private static void EnsureUniqueOperationIds(
        IEnumerable<WarehouseUnifiedOperation> operations)
    {
        var operationIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var operation in operations)
        {
            if (!operationIds.Add(operation.OperationId))
            {
                throw new DomainRuleViolationException(
                    $"Warehouse unified operation id must be unique. OperationId: {operation.OperationId}.");
            }
        }
    }

    private static void AddResourceEvents(
        string eventLogText,
        ICollection<TimelineEvent> timelineEvents)
    {
        foreach (var line in eventLogText.Split('\n'))
        {
            var separatorIndex = line.IndexOf('|');
            var occurredAtMs = long.Parse(
                line.AsSpan(0, separatorIndex),
                CultureInfo.InvariantCulture);
            var eventTypeEnd = line.IndexOf('|', separatorIndex + 1);
            var eventType = line[(separatorIndex + 1)..eventTypeEnd];

            timelineEvents.Add(new TimelineEvent(
                occurredAtMs,
                ResourceEventPhase(eventType),
                line));
        }
    }

    private static WarehouseUnifiedOperationTelemetry[] BuildOperationTelemetry(
        IReadOnlyList<WarehouseUnifiedOperation> operations,
        IReadOnlyList<WarehouseUnifiedOperationInterval> intervals)
    {
        var operationsById = operations.ToDictionary(
            operation => operation.OperationId,
            StringComparer.Ordinal);

        return intervals
            .OrderBy(interval => interval.StartedAtMs)
            .ThenBy(interval => interval.OperationId, StringComparer.Ordinal)
            .Select(interval =>
            {
                var operation = operationsById[interval.OperationId];
                var waitingTimeMs = interval.StartedAtMs - operation.RequestedAtMs;

                if (waitingTimeMs < 0)
                {
                    throw new InvalidOperationException(
                        $"Warehouse unified operation cannot start before it was requested. OperationId: {operation.OperationId}, RequestedAtMs: {operation.RequestedAtMs}, StartedAtMs: {interval.StartedAtMs}.");
                }

                return new WarehouseUnifiedOperationTelemetry(
                    operation.OperationId,
                    operation.OperationType,
                    operation.ResourceId,
                    operation.RequestedAtMs,
                    interval.StartedAtMs,
                    interval.FinishedAtMs,
                    waitingTimeMs,
                    operation.DurationMs,
                    operation.SkuId,
                    operation.InventoryDelta);
            })
            .ToArray();
    }

    private static void ApplyInventoryMutations(
        WarehouseInventoryLedger ledger,
        IReadOnlyList<WarehouseUnifiedOperation> operations,
        IReadOnlyList<WarehouseUnifiedOperationInterval> intervals,
        ICollection<TimelineEvent> timelineEvents)
    {
        var intervalsByOperationId = intervals.ToDictionary(
            interval => interval.OperationId,
            StringComparer.Ordinal);

        var mutations = operations
            .Select(operation =>
            {
                var interval = intervalsByOperationId[operation.OperationId];
                var isInbound =
                    operation.OperationType == WarehouseUnifiedOperationType.Inbound;

                return new InventoryMutation(
                    operation,
                    isInbound ? interval.FinishedAtMs : interval.StartedAtMs,
                    isInbound);
            })
            .OrderBy(mutation => mutation.OccurredAtMs)
            .ThenByDescending(mutation => mutation.IsInbound)
            .ThenBy(mutation => mutation.Operation.OperationId, StringComparer.Ordinal);

        foreach (var mutation in mutations)
        {
            var operation = mutation.Operation;
            var absoluteQuantity = Math.Abs(operation.InventoryDelta);

            if (mutation.IsInbound)
            {
                ledger.Add(operation.SkuId, absoluteQuantity);
            }
            else
            {
                ledger.Remove(operation.SkuId, absoluteQuantity);
            }

            var eventType = mutation.IsInbound
                ? "inventory.added"
                : "inventory.removed";
            var line =
                $"{mutation.OccurredAtMs}|{eventType}|sku_id={operation.SkuId}|quantity={absoluteQuantity.ToString(CultureInfo.InvariantCulture)}|operation={operation.OperationId}";

            timelineEvents.Add(new TimelineEvent(
                mutation.OccurredAtMs,
                mutation.IsInbound ? 1 : 5,
                line));
        }
    }

    private static int ResourceEventPhase(string eventType)
    {
        return eventType switch
        {
            "resource.released" => 0,
            "resource.requested" => 2,
            "resource.queued" => 3,
            "resource.acquired" => 4,
            _ => throw new InvalidOperationException(
                $"Unsupported warehouse shared resource event type: {eventType}.")
        };
    }

    private static string ToOwnerId(WarehouseUnifiedOperation operation)
    {
        return $"{ToFlowName(operation.OperationType)}:{operation.OperationId}";
    }

    private static string ToFlowName(WarehouseUnifiedOperationType operationType)
    {
        return operationType switch
        {
            WarehouseUnifiedOperationType.Inbound => "inbound",
            WarehouseUnifiedOperationType.Outbound => "outbound",
            WarehouseUnifiedOperationType.EachPick => "each_pick",
            _ => throw new ArgumentOutOfRangeException(
                nameof(operationType),
                operationType,
                "Unsupported warehouse unified operation type.")
        };
    }

    private sealed record InventoryMutation(
        WarehouseUnifiedOperation Operation,
        long OccurredAtMs,
        bool IsInbound);

    private sealed record TimelineEvent(
        long OccurredAtMs,
        int Phase,
        string Text);
}
