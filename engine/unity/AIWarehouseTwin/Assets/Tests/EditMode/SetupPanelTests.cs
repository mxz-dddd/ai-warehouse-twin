using AIWarehouseTwin.Simulation;
using AIWarehouseTwin.UI;
using NUnit.Framework;
using Sim.Contracts.Artifacts;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.Tests
{
    public sealed class SetupPanelTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CurrentConfig_starts_with_default_values()
        {
            var panel = CreatePanel();

            var cfg = panel.CurrentConfig;

            Assert.That(cfg.lengthM, Is.EqualTo(40f).Within(0.001f));
            Assert.That(cfg.widthM, Is.EqualTo(20f).Within(0.001f));
            Assert.That(cfg.shelfRows, Is.EqualTo(3));
            Assert.That(cfg.skuCount, Is.EqualTo(200));
            Assert.That(cfg.workerCount, Is.EqualTo(5));
            Assert.That(cfg.forkliftCount, Is.EqualTo(2));
            Assert.That(cfg.orderCount, Is.EqualTo(50));
        }

        [Test]
        public void SetParameters_updates_generated_config()
        {
            var panel = CreatePanel();

            panel.SetParameters(60f, 30f, 4, 300, 8, 3, 120);

            var cfg = panel.CurrentConfig;
            Assert.That(cfg.lengthM, Is.EqualTo(60f).Within(0.001f));
            Assert.That(cfg.widthM, Is.EqualTo(30f).Within(0.001f));
            Assert.That(cfg.shelfRows, Is.EqualTo(4));
            Assert.That(cfg.skuCount, Is.EqualTo(300));
            Assert.That(cfg.workerCount, Is.EqualTo(8));
            Assert.That(cfg.forkliftCount, Is.EqualTo(3));
            Assert.That(cfg.orderCount, Is.EqualTo(120));
        }

        [Test]
        public void ValidateCurrentConfig_rejects_too_many_shelf_rows_for_width()
        {
            var panel = CreatePanel();

            panel.SetParameters(40f, 9f, 3, 200, 5, 2, 50);

            Assert.That(panel.ValidateCurrentConfig(), Is.False);
            Assert.That(panel.RunScenario(), Is.False);
            Assert.That(panel.LastValidationMessage, Is.EqualTo(SetupPanel.TooManyShelfRowsMessage));
        }

        [TestCase(0f, 20f, 3, 200, 5, 2, 50)]
        [TestCase(40f, 0f, 3, 200, 5, 2, 50)]
        [TestCase(40f, 20f, 0, 200, 5, 2, 50)]
        [TestCase(40f, 20f, 3, 0, 5, 2, 50)]
        [TestCase(40f, 20f, 3, 200, 0, 2, 50)]
        [TestCase(40f, 20f, 3, 200, 5, -1, 50)]
        [TestCase(40f, 20f, 3, 200, 5, 2, 0)]
        public void ValidateCurrentConfig_rejects_invalid_quantity_parameters(
            float lengthM,
            float widthM,
            int shelfRows,
            int skuCount,
            int workerCount,
            int forkliftCount,
            int orderCount)
        {
            var panel = CreatePanel();

            panel.SetParameters(lengthM, widthM, shelfRows, skuCount, workerCount, forkliftCount, orderCount);

            Assert.That(panel.ValidateCurrentConfig(), Is.False);
        }

        [Test]
        public void ResetToDefaults_restores_default_values()
        {
            var panel = CreatePanel();
            panel.SetParameters(60f, 30f, 4, 300, 8, 3, 120);

            panel.ResetToDefaults();

            Assert.That(panel.CurrentConfig.lengthM, Is.EqualTo(40f).Within(0.001f));
            Assert.That(panel.CurrentConfig.widthM, Is.EqualTo(20f).Within(0.001f));
            Assert.That(panel.CurrentConfig.shelfRows, Is.EqualTo(3));
            Assert.That(panel.StatusText, Is.EqualTo(SetupPanel.ReadyStatusText));
        }

        [Test]
        public void ToggleCollapsed_changes_collapsed_state()
        {
            var panel = CreatePanel();

            panel.ToggleCollapsed();

            Assert.That(panel.IsCollapsed, Is.True);
        }

        [Test]
        public void Sync_methods_without_bound_controls_do_not_throw()
        {
            var panel = CreatePanel();

            Assert.DoesNotThrow(panel.SyncFromUi);
            Assert.DoesNotThrow(panel.SyncToUi);
        }

        [Test]
        public void Bound_ui_fields_round_trip_config_values()
        {
            var panel = CreatePanel();
            var rootElement = BuildUi(out var length, out var width, out var shelfRows);
            panel.Bind(rootElement);

            length.value = 55f;
            width.value = 25f;
            shelfRows.value = 4;
            panel.SyncFromUi();

            Assert.That(panel.CurrentConfig.lengthM, Is.EqualTo(55f).Within(0.001f));
            Assert.That(panel.CurrentConfig.widthM, Is.EqualTo(25f).Within(0.001f));
            Assert.That(panel.CurrentConfig.shelfRows, Is.EqualTo(4));
        }

        [Test]
        public void RunScenario_invokes_injected_builder_and_callback()
        {
            var panel = CreatePanel();
            var buildCount = 0;
            RunArtifact callbackArtifact = null;
            panel.BuildScenario = cfg =>
            {
                buildCount++;
                Assert.That(cfg.orderCount, Is.EqualTo(77));
                return FakeArtifact("setup-panel-test");
            };
            panel.OnScenarioBuilt = artifact => callbackArtifact = artifact;
            panel.SetParameters(40f, 20f, 3, 200, 5, 2, 77);

            var result = panel.RunScenario();

            Assert.That(result, Is.True);
            Assert.That(buildCount, Is.EqualTo(1));
            Assert.That(callbackArtifact, Is.Not.Null);
            Assert.That(callbackArtifact.ScenarioId, Is.EqualTo("setup-panel-test"));
            Assert.That(panel.StatusText, Is.EqualTo(SetupPanel.CompleteStatusText));
        }

        [Test]
        public void RunScenario_disables_and_restores_bound_run_button()
        {
            var panel = CreatePanel();
            var rootElement = BuildUi(out _, out _, out _);
            var runButton = rootElement.Q<Button>("run-button");
            var observedDisabled = false;
            panel.Bind(rootElement);
            panel.BuildScenario = _ =>
            {
                observedDisabled = !runButton.enabledSelf;
                return FakeArtifact("button-state");
            };

            panel.RunScenario();

            Assert.That(observedDisabled, Is.True);
            Assert.That(runButton.enabledSelf, Is.True);
        }

        [Test]
        public void RunScenario_does_not_build_when_validation_fails()
        {
            var panel = CreatePanel();
            var buildCount = 0;
            panel.BuildScenario = _ =>
            {
                buildCount++;
                return FakeArtifact("should-not-build");
            };
            panel.SetParameters(40f, 9f, 3, 200, 5, 2, 50);

            var result = panel.RunScenario();

            Assert.That(result, Is.False);
            Assert.That(buildCount, Is.EqualTo(0));
            Assert.That(panel.StatusText, Is.EqualTo(SetupPanel.TooManyShelfRowsMessage));
        }

        private SetupPanel CreatePanel()
        {
            root = new GameObject("SetupPanelTests");
            return root.AddComponent<SetupPanel>();
        }

        private static VisualElement BuildUi(
            out FloatField length,
            out FloatField width,
            out IntegerField shelfRows)
        {
            var rootElement = new VisualElement();
            length = new FloatField { name = "length-field" };
            width = new FloatField { name = "width-field" };
            shelfRows = new IntegerField { name = "shelf-rows-field" };
            rootElement.Add(length);
            rootElement.Add(width);
            rootElement.Add(shelfRows);
            rootElement.Add(new IntegerField { name = "sku-count-field" });
            rootElement.Add(new IntegerField { name = "worker-count-field" });
            rootElement.Add(new IntegerField { name = "forklift-count-field" });
            rootElement.Add(new IntegerField { name = "order-count-field" });
            rootElement.Add(new Button { name = "run-button" });
            rootElement.Add(new Button { name = "reset-button" });
            rootElement.Add(new Label { name = "status-label" });
            return rootElement;
        }

        private static RunArtifact FakeArtifact(string scenarioId)
        {
            return new RunArtifact
            {
                SchemaVersion = RunArtifact.CurrentSchemaVersion,
                ArtifactKind = RunArtifact.CurrentArtifactKind,
                ScenarioId = scenarioId,
                Layout = RunArtifactLayout.Empty
            };
        }
    }
}
