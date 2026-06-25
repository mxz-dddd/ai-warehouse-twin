using Sim.Core.Processes.Inbound;
using Sim.Core.Scenarios;
using Xunit;

namespace Sim.Core.Tests.Processes.Inbound;

public sealed class InboundDeterminismTests
{
    [Fact]
    public void Run_ProducesSameResult_ForSameScenario()
    {
        var scenario = new InboundScenario(
            "deterministic-inbound",
            seed: 987,
            [Receipt("receipt-1", 0), Receipt("receipt-2", 25)],
            new InboundProcessParameters(100, 50, 25),
            dockCount: 1,
            forkliftCount: 1);

        var first = new InboundScenarioRunner().Run(scenario);
        var second = new InboundScenarioRunner().Run(scenario);

        Assert.Equal(first.EventLogText, second.EventLogText);
        Assert.Equal(first.CompletedReceipts, second.CompletedReceipts);
        Assert.Equal(first.TotalQuantityAvailable, second.TotalQuantityAvailable);
        Assert.Equal(first.FinishedAtMs, second.FinishedAtMs);
    }

    private static InboundReceipt Receipt(string receiptId, long arrivesAtMs)
    {
        return new InboundReceipt(
            receiptId,
            "warehouse-1",
            $"sku-{receiptId}",
            5m,
            "stage-1",
            $"loc-{receiptId}",
            arrivesAtMs);
    }
}
