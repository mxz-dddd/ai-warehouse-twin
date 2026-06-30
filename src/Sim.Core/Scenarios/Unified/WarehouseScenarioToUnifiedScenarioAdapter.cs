using Sim.Core.Domain;
using Sim.Core.Scenarios;

namespace Sim.Core.Scenarios.Unified;

public static class WarehouseScenarioToUnifiedScenarioAdapter
{
    public static WarehouseUnifiedScenario Convert(WarehouseScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var initialInventory = BuildInitialInventory(scenario);
        var operations = BuildOperations(scenario)
            .OrderBy(operation => operation.RequestedAtMs)
            .ThenBy(operation => operation.OperationType)
            .ThenBy(operation => operation.OperationId, StringComparer.Ordinal)
            .ToArray();

        return new WarehouseUnifiedScenario(
            scenario.ScenarioId,
            scenario.Seed,
            initialInventory,
            operations);
    }

    private static SortedDictionary<string, decimal> BuildInitialInventory(
        WarehouseScenario scenario)
    {
        var initialInventory = new SortedDictionary<string, decimal>(
            StringComparer.Ordinal);

        if (scenario.OutboundScenario is not null)
        {
            foreach (var item in scenario.OutboundScenario.InitialInventory
                         .Where(item => item.Status == InventoryStatus.Available))
            {
                AddInventory(initialInventory, item.SkuId, item.Quantity);
            }
        }

        if (scenario.EachPickScenario is not null)
        {
            foreach (var item in scenario.EachPickScenario.InitialInventory
                         .Where(item => item.Status == InventoryStatus.Available))
            {
                AddInventory(initialInventory, item.SkuId, item.Quantity);
            }
        }

        return initialInventory;
    }

    private static IEnumerable<WarehouseUnifiedOperation> BuildOperations(
        WarehouseScenario scenario)
    {
        if (scenario.InboundScenario is not null)
        {
            foreach (var receipt in scenario.InboundScenario.Receipts
                         .OrderBy(receipt => receipt.ArrivesAtMs)
                         .ThenBy(receipt => receipt.ReceiptId, StringComparer.Ordinal))
            {
                yield return new WarehouseUnifiedOperation(
                    $"inbound:{receipt.ReceiptId}",
                    WarehouseUnifiedOperationType.Inbound,
                    receipt.ArrivesAtMs,
                    ResourceIdForInboundReceipt(),
                    CoarseInboundDurationMs(scenario.InboundScenario),
                    receipt.SkuId,
                    receipt.Quantity);
            }
        }

        if (scenario.OutboundScenario is not null)
        {
            foreach (var order in scenario.OutboundScenario.Orders
                         .OrderBy(order => order.ReleasedAtMs)
                         .ThenBy(order => order.OrderId, StringComparer.Ordinal))
            {
                yield return new WarehouseUnifiedOperation(
                    $"outbound:{order.OrderId}",
                    WarehouseUnifiedOperationType.Outbound,
                    order.ReleasedAtMs,
                    order.DockLocationId,
                    CoarseOutboundDurationMs(scenario.OutboundScenario),
                    order.SkuId,
                    -order.Quantity);
            }
        }

        if (scenario.EachPickScenario is not null)
        {
            foreach (var order in scenario.EachPickScenario.Orders
                         .OrderBy(order => order.ReleasedAtMs)
                         .ThenBy(order => order.OrderId, StringComparer.Ordinal))
            {
                yield return new WarehouseUnifiedOperation(
                    $"each_pick:{order.OrderId}",
                    WarehouseUnifiedOperationType.EachPick,
                    order.ReleasedAtMs,
                    order.PickStationId,
                    CoarseEachPickDurationMs(scenario.EachPickScenario),
                    order.SkuId,
                    -order.Quantity);
            }
        }
    }

    private static string ResourceIdForInboundReceipt()
    {
        return "dock-1";
    }

    private static long CoarseInboundDurationMs(InboundScenario scenario)
    {
        return scenario.Parameters.UnloadDurationMs +
               scenario.Parameters.PutawayTotalDurationMs;
    }

    private static long CoarseOutboundDurationMs(OutboundScenario scenario)
    {
        return scenario.Parameters.PickTotalDurationMs +
               scenario.Parameters.LoadTotalDurationMs;
    }

    private static long CoarseEachPickDurationMs(EachPickScenario scenario)
    {
        return scenario.Parameters.ToteBindDurationMs +
               scenario.Parameters.TravelToStationDurationMs +
               scenario.Parameters.PickServiceDurationMs +
               scenario.Parameters.MoveToStagingDurationMs;
    }

    private static void AddInventory(
        IDictionary<string, decimal> inventory,
        string skuId,
        decimal quantity)
    {
        inventory[skuId] = inventory.TryGetValue(skuId, out var current)
            ? current + quantity
            : quantity;
    }
}
