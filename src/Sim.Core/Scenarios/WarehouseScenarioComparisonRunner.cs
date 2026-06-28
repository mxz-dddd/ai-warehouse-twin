namespace Sim.Core.Scenarios;

public sealed class WarehouseScenarioComparisonRunner
{
    public WarehouseScenarioComparisonResult Compare(
        WarehouseScenario baseline,
        WarehouseScenario candidate)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);

        var runner = new WarehouseScenarioRunner();
        var baselineResult = runner.Run(baseline);
        var candidateResult = runner.Run(candidate);
        var baselineMetrics =
            WarehouseScenarioComparisonMetrics.FromRunResult(baselineResult);
        var candidateMetrics =
            WarehouseScenarioComparisonMetrics.FromRunResult(candidateResult);

        return new WarehouseScenarioComparisonResult(
            baseline.ScenarioId,
            candidate.ScenarioId,
            baselineResult,
            candidateResult,
            baselineMetrics,
            candidateMetrics,
            BuildDeltas(baselineMetrics, candidateMetrics));
    }

    private static WarehouseScenarioComparisonDelta[] BuildDeltas(
        WarehouseScenarioComparisonMetrics baseline,
        WarehouseScenarioComparisonMetrics candidate)
    {
        return
        [
            Delta("finished_at_ms", baseline.FinishedAtMs, candidate.FinishedAtMs),
            Delta("completed_receipts", baseline.CompletedReceipts, candidate.CompletedReceipts),
            Delta("completed_outbound_orders", baseline.CompletedOutboundOrders, candidate.CompletedOutboundOrders),
            Delta("completed_each_pick_orders", baseline.CompletedEachPickOrders, candidate.CompletedEachPickOrders),
            Delta("total_quantity_received", baseline.TotalQuantityReceived, candidate.TotalQuantityReceived),
            Delta("total_quantity_shipped", baseline.TotalQuantityShipped, candidate.TotalQuantityShipped),
            Delta("total_quantity_picked", baseline.TotalQuantityPicked, candidate.TotalQuantityPicked),
            Delta("inbound_receipt_throughput_per_hour", baseline.InboundReceiptThroughputPerHour, candidate.InboundReceiptThroughputPerHour),
            Delta("outbound_order_throughput_per_hour", baseline.OutboundOrderThroughputPerHour, candidate.OutboundOrderThroughputPerHour),
            Delta("each_pick_order_throughput_per_hour", baseline.EachPickOrderThroughputPerHour, candidate.EachPickOrderThroughputPerHour),
            Delta("total_work_item_throughput_per_hour", baseline.TotalWorkItemThroughputPerHour, candidate.TotalWorkItemThroughputPerHour),
        ];
    }

    private static WarehouseScenarioComparisonDelta Delta(
        string metricName,
        decimal baselineValue,
        decimal candidateValue)
    {
        return new WarehouseScenarioComparisonDelta(
            metricName,
            baselineValue,
            candidateValue);
    }
}
