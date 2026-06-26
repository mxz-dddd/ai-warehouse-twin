using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Processes.Inbound;
using Sim.Core.Processes.Outbound;

namespace Sim.Core.Scenarios.Samples;

public static class WarehouseSampleScenarioFactory
{
    public static WarehouseScenario CreateSmallWarehouse()
    {
        return new WarehouseScenario(
            "sample-small-warehouse",
            seed: 20240627,
            inboundScenario: CreateInboundScenario(),
            outboundScenario: CreateOutboundScenario(),
            eachPickScenario: CreateEachPickScenario());
    }

    private static InboundScenario CreateInboundScenario()
    {
        return new InboundScenario(
            "sample-small-warehouse.inbound",
            seed: 101,
            [
                new InboundReceipt(
                    "receipt-1",
                    "warehouse-1",
                    "sku-inbound-1",
                    7m,
                    "inbound-stage-1",
                    "reserve-1",
                    arrivesAtMs: 10)
            ],
            new InboundProcessParameters(100, 50, 50),
            dockCount: 1,
            forkliftCount: 1);
    }

    private static OutboundScenario CreateOutboundScenario()
    {
        return new OutboundScenario(
            "sample-small-warehouse.outbound",
            seed: 202,
            [
                new OutboundOrder(
                    "order-1",
                    "warehouse-1",
                    "sku-outbound-1",
                    8m,
                    "pick-1",
                    "outbound-stage-1",
                    "dock-1",
                    releasedAtMs: 20)
            ],
            [
                new OutboundInventoryItem(
                    "inv-outbound-1",
                    "sku-outbound-1",
                    8m,
                    "pick-1",
                    InventoryStatus.Available)
            ],
            new OutboundProcessParameters(50, 50, 25, 75),
            workerCount: 1,
            dockCount: 1);
    }

    private static EachPickScenario CreateEachPickScenario()
    {
        return new EachPickScenario(
            "sample-small-warehouse.each-pick",
            seed: 303,
            [
                new EachPickOrder(
                    "each-order-1",
                    "warehouse-1",
                    "sku-each-1",
                    9m,
                    "pick-face-1",
                    "station-1",
                    "each-pick-stage-1",
                    releasedAtMs: 30)
            ],
            [
                new EachPickInventoryItem(
                    "inv-each-1",
                    "sku-each-1",
                    9m,
                    "pick-face-1",
                    InventoryStatus.Available)
            ],
            new EachPickProcessParameters(
                toteBindDurationMs: 10,
                travelToStationDurationMs: 20,
                pickServiceDurationMs: 30,
                moveToStagingDurationMs: 40),
            stationCount: 1,
            workerCount: 1);
    }
}
