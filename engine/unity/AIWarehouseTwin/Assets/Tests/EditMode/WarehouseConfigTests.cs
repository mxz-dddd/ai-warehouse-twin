using AIWarehouseTwin.Simulation;
using NUnit.Framework;

namespace AIWarehouseTwin.Tests
{
    public sealed class WarehouseConfigTests
    {
        [Test]
        public void Defaults_match_demo_seam_baseline()
        {
            var cfg = new WarehouseConfig();

            Assert.That(cfg.lengthM, Is.EqualTo(40f));
            Assert.That(cfg.widthM, Is.EqualTo(20f));
            Assert.That(cfg.shelfRows, Is.EqualTo(3));
            Assert.That(cfg.skuCount, Is.EqualTo(200));
            Assert.That(cfg.workerCount, Is.EqualTo(5));
            Assert.That(cfg.forkliftCount, Is.EqualTo(2));
            Assert.That(cfg.orderCount, Is.EqualTo(50));
        }
    }
}
