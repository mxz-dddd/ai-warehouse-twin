using Sim.Core.Processes.Inbound;
using Sim.Core.Scenarios;
using Xunit;

namespace Sim.Core.Tests.Processes.Inbound;

public sealed class InboundQueueingTests
{
    [Fact]
    public void Run_QueuesSecondReceipt_WhenSingleDockIsBusy()
    {
        var scenario = new InboundScenario(
            "dock-queue",
            seed: 123,
            [Receipt("receipt-1"), Receipt("receipt-2")],
            new InboundProcessParameters(100, 10, 0),
            dockCount: 1,
            forkliftCount: 2);

        var result = new InboundScenarioRunner().Run(scenario);

        Assert.Equal(2, result.CompletedReceipts);
        Assert.Equal(10m, result.TotalQuantityAvailable);
        Assert.Equal(210, result.FinishedAtMs);
        Assert.Equal(
            "0|0|inbound.receipt_arrived.receipt-1|InboundReceiptArrived\n" +
            "1|0|inbound.receipt_arrived.receipt-2|InboundReceiptArrived\n" +
            "2|100|inbound.unload_completed.receipt-1|InboundUnloadCompleted\n" +
            "3|110|inbound.putaway_completed.receipt-1|InboundPutawayCompleted\n" +
            "4|200|inbound.unload_completed.receipt-2|InboundUnloadCompleted\n" +
            "5|210|inbound.putaway_completed.receipt-2|InboundPutawayCompleted",
            result.EventLogText);
    }

    [Fact]
    public void Run_QueuesSecondReceipt_WhenSingleForkliftIsBusy()
    {
        var scenario = new InboundScenario(
            "forklift-queue",
            seed: 123,
            [Receipt("receipt-1"), Receipt("receipt-2")],
            new InboundProcessParameters(100, 100, 0),
            dockCount: 2,
            forkliftCount: 1);

        var result = new InboundScenarioRunner().Run(scenario);

        Assert.Equal(2, result.CompletedReceipts);
        Assert.Equal(300, result.FinishedAtMs);
        Assert.Contains("2|100|inbound.unload_completed.receipt-1|InboundUnloadCompleted", result.EventLogText);
        Assert.Contains("3|100|inbound.unload_completed.receipt-2|InboundUnloadCompleted", result.EventLogText);
        Assert.Contains("4|200|inbound.putaway_completed.receipt-1|InboundPutawayCompleted", result.EventLogText);
        Assert.Contains("5|300|inbound.putaway_completed.receipt-2|InboundPutawayCompleted", result.EventLogText);
    }

    private static InboundReceipt Receipt(string receiptId)
    {
        return new InboundReceipt(
            receiptId,
            "warehouse-1",
            $"sku-{receiptId}",
            5m,
            "stage-1",
            $"loc-{receiptId}",
            0);
    }
}
