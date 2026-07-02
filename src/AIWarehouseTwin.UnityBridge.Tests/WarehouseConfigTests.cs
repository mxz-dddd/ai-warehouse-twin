using AIWarehouseTwin.Simulation;
using Xunit;

namespace AIWarehouseTwin.UnityBridge.Tests;

public sealed class WarehouseConfigTests
{
    [Fact]
    public void Defaults_match_demo_seam_baseline()
    {
        var cfg = new WarehouseConfig();

        Assert.Equal(40f, cfg.lengthM);
        Assert.Equal(20f, cfg.widthM);
        Assert.Equal(3, cfg.shelfRows);
        Assert.Equal(200, cfg.skuCount);
        Assert.Equal(5, cfg.workerCount);
        Assert.Equal(2, cfg.forkliftCount);
        Assert.Equal(50, cfg.orderCount);
    }
}
