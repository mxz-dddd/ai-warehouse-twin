using System;
using AIWarehouseTwin.Simulation;
using NUnit.Framework;

namespace AIWarehouseTwin.Tests
{
    public sealed class ScenarioBuilderTests
    {
        [Test]
        public void Build_returns_run_artifact_for_default_config()
        {
            var artifact = ScenarioBuilder.Build(new WarehouseConfig());

            Assert.That(artifact, Is.Not.Null);
            Assert.That(artifact.SchemaVersion, Is.EqualTo("run-artifact.v1"));
            Assert.That(artifact.ArtifactKind, Is.EqualTo("warehouse-simulation-run"));
            Assert.That(artifact.ScenarioId, Is.EqualTo("unity-demo-40x20-r3-sku200-w5-f2-o50"));
            Assert.That(artifact.KpiSummary.TotalCompletedWorkItems, Is.EqualTo(50));
            Assert.That(artifact.Layout.Resources, Has.Count.EqualTo(7));
        }

        [Test]
        public void Build_is_deterministic_for_same_config()
        {
            var cfg = new WarehouseConfig();

            var first = ScenarioBuilder.Build(cfg);
            var second = ScenarioBuilder.Build(cfg);

            Assert.That(second.ScenarioId, Is.EqualTo(first.ScenarioId));
            Assert.That(second.FinishedAtMs, Is.EqualTo(first.FinishedAtMs));
            Assert.That(second.KpiSummary, Is.EqualTo(first.KpiSummary));
            Assert.That(second.EventLog, Is.EqualTo(first.EventLog));
            Assert.That(second.Layout.Resources, Is.EqualTo(first.Layout.Resources));
        }

        [Test]
        public void Build_rejects_null_config()
        {
            Assert.Throws<ArgumentNullException>(() => ScenarioBuilder.Build(null));
        }

        [TestCase("lengthM")]
        [TestCase("widthM")]
        [TestCase("shelfRows")]
        [TestCase("skuCount")]
        [TestCase("workerCount")]
        [TestCase("forkliftCount")]
        [TestCase("orderCount")]
        public void Build_rejects_invalid_config_values(string fieldName)
        {
            var cfg = new WarehouseConfig();
            switch (fieldName)
            {
                case "lengthM":
                    cfg.lengthM = 0f;
                    break;
                case "widthM":
                    cfg.widthM = 0f;
                    break;
                case "shelfRows":
                    cfg.shelfRows = 0;
                    break;
                case "skuCount":
                    cfg.skuCount = 0;
                    break;
                case "workerCount":
                    cfg.workerCount = 0;
                    break;
                case "forkliftCount":
                    cfg.forkliftCount = -1;
                    break;
                case "orderCount":
                    cfg.orderCount = 0;
                    break;
            }

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ScenarioBuilder.Build(cfg));

            Assert.That(ex.ParamName, Is.EqualTo(fieldName));
        }
    }
}
