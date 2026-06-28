namespace Sim.Core.Scenarios;

public sealed record WarehouseScenarioComparisonMetrics
{
    public WarehouseScenarioComparisonMetrics(
        long finishedAtMs,
        int completedReceipts,
        int completedOutboundOrders,
        int completedEachPickOrders,
        decimal totalQuantityReceived,
        decimal totalQuantityShipped,
        decimal totalQuantityPicked,
        decimal inboundReceiptThroughputPerHour,
        decimal outboundOrderThroughputPerHour,
        decimal eachPickOrderThroughputPerHour,
        decimal totalWorkItemThroughputPerHour)
    {
        FinishedAtMs = finishedAtMs;
        CompletedReceipts = completedReceipts;
        CompletedOutboundOrders = completedOutboundOrders;
        CompletedEachPickOrders = completedEachPickOrders;
        TotalQuantityReceived = totalQuantityReceived;
        TotalQuantityShipped = totalQuantityShipped;
        TotalQuantityPicked = totalQuantityPicked;
        InboundReceiptThroughputPerHour = inboundReceiptThroughputPerHour;
        OutboundOrderThroughputPerHour = outboundOrderThroughputPerHour;
        EachPickOrderThroughputPerHour = eachPickOrderThroughputPerHour;
        TotalWorkItemThroughputPerHour = totalWorkItemThroughputPerHour;
    }

    public long FinishedAtMs { get; }

    public int CompletedReceipts { get; }

    public int CompletedOutboundOrders { get; }

    public int CompletedEachPickOrders { get; }

    public decimal TotalQuantityReceived { get; }

    public decimal TotalQuantityShipped { get; }

    public decimal TotalQuantityPicked { get; }

    public decimal InboundReceiptThroughputPerHour { get; }

    public decimal OutboundOrderThroughputPerHour { get; }

    public decimal EachPickOrderThroughputPerHour { get; }

    public decimal TotalWorkItemThroughputPerHour { get; }

    public static WarehouseScenarioComparisonMetrics FromRunResult(
        WarehouseRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var kpi = result.KpiSummary;

        return new WarehouseScenarioComparisonMetrics(
            result.FinishedAtMs,
            result.CompletedReceipts,
            result.CompletedOutboundOrders,
            result.CompletedEachPickOrders,
            result.TotalQuantityAvailable,
            result.TotalQuantityShipped,
            result.TotalQuantityPicked,
            kpi.ReceiptThroughputPerHour,
            kpi.OutboundOrderThroughputPerHour,
            kpi.EachPickOrderThroughputPerHour,
            kpi.TotalWorkItemThroughputPerHour);
    }
}
