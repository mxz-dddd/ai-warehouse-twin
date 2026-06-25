using Sim.Core.Processes.Inbound;
using Sim.Core.Scenarios;
using Xunit;

namespace Sim.Core.Tests.Processes.Inbound;

public sealed class InboundScenarioRunnerTests
{
    [Fact]
    public void Run_CompletesSingleReceiptToAvailable()
    {
        var scenario = new InboundScenario(
            "single-inbound",
            seed: 123,
            [Receipt("receipt-1", quantity: 7m, arrivesAtMs: 10)],
            new InboundProcessParameters(100, 50, 50),
            dockCount: 1,
            forkliftCount: 1);

        var result = new InboundScenarioRunner().Run(scenario);

        Assert.Equal(1, result.CompletedReceipts);
        Assert.Equal(7m, result.TotalQuantityAvailable);
        Assert.Equal(10, result.StartedAtMs);
        Assert.Equal(210, result.FinishedAtMs);
        Assert.Equal("AVAILABLE", result.FinalWorldState.Entities["receipt:receipt-1"].Status);
        Assert.Contains("inbound.receipt_arrived.receipt-1", result.EventLogText);
        Assert.Contains("inbound.unload_completed.receipt-1", result.EventLogText);
        Assert.Contains("inbound.putaway_completed.receipt-1", result.EventLogText);
    }

    private static InboundReceipt Receipt(
        string receiptId,
        decimal quantity = 5m,
        long arrivesAtMs = 0)
    {
        return new InboundReceipt(
            receiptId,
            "warehouse-1",
            $"sku-{receiptId}",
            quantity,
            "stage-1",
            $"loc-{receiptId}",
            arrivesAtMs);
    }
}
