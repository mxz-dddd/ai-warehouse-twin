using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Samples;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseKpiSummaryTests
{
    [Fact]
    public void FromRunResult_ComputesStableSampleWarehouseKpis()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var result = new WarehouseScenarioRunner().Run(scenario);

        var kpi = WarehouseKpiSummary.FromRunResult(result);

        Assert.Equal(210, kpi.TotalDurationMs);
        Assert.Equal(3, kpi.TotalCompletedWorkItems);
        Assert.Equal(10, kpi.EventLogLineCount);

        Assert.True(kpi.ReceiptThroughputPerHour > 17142m);
        Assert.True(kpi.ReceiptThroughputPerHour < 17143m);

        Assert.True(kpi.OutboundOrderThroughputPerHour > 17142m);
        Assert.True(kpi.OutboundOrderThroughputPerHour < 17143m);

        Assert.True(kpi.EachPickOrderThroughputPerHour > 17142m);
        Assert.True(kpi.EachPickOrderThroughputPerHour < 17143m);

        Assert.True(kpi.TotalWorkItemThroughputPerHour > 51428m);
        Assert.True(kpi.TotalWorkItemThroughputPerHour < 51429m);
    }

    [Fact]
    public void WarehouseRunResult_ExposesKpiSummary()
    {
        var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
        var result = new WarehouseScenarioRunner().Run(scenario);

        Assert.Equal(
            WarehouseKpiSummary.FromRunResult(result),
            result.KpiSummary);
    }
}
