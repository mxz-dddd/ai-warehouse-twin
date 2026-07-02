using System;
using AIWarehouseTwin.UI;
using NUnit.Framework;
using Sim.Contracts.Artifacts;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.Tests
{
    public sealed class KpiHudPanelTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Default_snapshot_formats_as_safe_hud_text()
        {
            var panel = CreatePanel();

            panel.RefreshNow();

            Assert.That(panel.CompletionRateText, Is.EqualTo("0.0%"));
            Assert.That(panel.AverageWaitText, Is.EqualTo("0.0 min"));
            Assert.That(panel.ProcessedOrdersText, Is.EqualTo("0 / 0"));
            Assert.That(panel.PathEfficiencyText, Is.EqualTo("0.0%"));
            Assert.That(panel.SimulationTimeText, Is.EqualTo("00:00 / 00:00"));
        }

        [Test]
        public void RefreshNow_formats_snapshot_values_for_hud_labels()
        {
            var panel = CreatePanel();
            var rootElement = BuildUi(
                out var completionRate,
                out var averageWait,
                out var processedOrders,
                out var pathEfficiency,
                out var simulationTime,
                out _);
            panel.Bind(rootElement);
            panel.SnapshotProvider = () => new KpiHudSnapshot(
                completedRate: 0.873f,
                averageWaitMinutes: 2.4f,
                processedOrders: 43,
                totalOrders: 50,
                pathEfficiency: 0.925f,
                simulationTime: 125f,
                totalDuration: 600f);

            panel.RefreshNow();

            Assert.That(completionRate.text, Is.EqualTo("87.3%"));
            Assert.That(averageWait.text, Is.EqualTo("2.4 min"));
            Assert.That(processedOrders.text, Is.EqualTo("43 / 50"));
            Assert.That(pathEfficiency.text, Is.EqualTo("92.5%"));
            Assert.That(simulationTime.text, Is.EqualTo("02:05 / 10:00"));
        }

        [TestCase(0.873f, "87.3%")]
        [TestCase(1f, "100.0%")]
        [TestCase(-1f, "0.0%")]
        public void Completion_rate_formatting_is_stable(float value, string expected)
        {
            Assert.That(KpiHudPanel.FormatCompletionRate(value), Is.EqualTo(expected));
        }

        [Test]
        public void Average_wait_formatting_uses_minutes()
        {
            Assert.That(KpiHudPanel.FormatAverageWaitMinutes(2.4f), Is.EqualTo("2.4 min"));
        }

        [Test]
        public void Processed_orders_formatting_uses_processed_and_total()
        {
            Assert.That(KpiHudPanel.FormatProcessedOrders(43, 50), Is.EqualTo("43 / 50"));
        }

        [Test]
        public void Path_efficiency_formatting_uses_percent()
        {
            Assert.That(KpiHudPanel.FormatPathEfficiency(0.925f), Is.EqualTo("92.5%"));
        }

        [Test]
        public void Simulation_time_formatting_uses_clock_times()
        {
            Assert.That(KpiHudPanel.FormatSimulationTime(125f, 600f), Is.EqualTo("02:05 / 10:00"));
            Assert.That(KpiHudPanel.FormatSimulationTime(3661f, 7200f), Is.EqualTo("1:01:01 / 2:00:00"));
        }

        [Test]
        public void Speed_label_comes_from_injected_playback()
        {
            var panel = CreatePanel();
            var rootElement = BuildUi(out _, out _, out _, out _, out _, out var speedButton);
            var playback = new FakePlayback { SpeedLabel = "5×" };
            panel.Bind(rootElement);
            panel.PlaybackControls = playback;

            panel.RefreshNow();

            Assert.That(panel.SpeedLabelText, Is.EqualTo("5×"));
            Assert.That(speedButton.text, Is.EqualTo("5×"));
        }

        [Test]
        public void CycleSpeed_invokes_injected_playback_and_refreshes_label()
        {
            var panel = CreatePanel();
            var rootElement = BuildUi(out _, out _, out _, out _, out _, out var speedButton);
            var playback = new FakePlayback { SpeedLabel = "1×", NextSpeedLabel = "10×" };
            panel.Bind(rootElement);
            panel.PlaybackControls = playback;

            panel.CycleSpeed();

            Assert.That(playback.CycleCount, Is.EqualTo(1));
            Assert.That(panel.SpeedLabelText, Is.EqualTo("10×"));
            Assert.That(speedButton.text, Is.EqualTo("10×"));
        }

        [Test]
        public void RefreshNow_without_bound_controls_does_not_throw()
        {
            var panel = CreatePanel();
            panel.SnapshotProvider = () => new KpiHudSnapshot(0.5f, 1.5f, 5, 10, 0.8f, 30f, 60f);

            Assert.DoesNotThrow(() => panel.RefreshNow());
            Assert.That(panel.CompletionRateText, Is.EqualTo("50.0%"));
        }

        [Test]
        public void Null_data_source_uses_safe_default_snapshot()
        {
            var panel = CreatePanel();
            panel.SnapshotProvider = null;

            panel.RefreshNow();

            Assert.That(panel.CurrentSnapshot, Is.EqualTo(KpiHudSnapshot.Default));
            Assert.That(panel.ProcessedOrdersText, Is.EqualTo("0 / 0"));
        }

        [Test]
        public void RunArtifact_snapshot_maps_contract_kpi_without_scene_dependency()
        {
            var artifact = new RunArtifact
            {
                SchemaVersion = RunArtifact.CurrentSchemaVersion,
                ArtifactKind = RunArtifact.CurrentArtifactKind,
                ScenarioId = "kpi-hud-test",
                FinalWorldTimeMs = 125000,
                KpiSummary = new RunArtifactKpiSummary
                {
                    TotalCompletedWorkItems = 43,
                    TotalDurationMs = 600000
                },
                EventLog = Array.Empty<string>()
            };

            var snapshot = KpiHudSnapshot.FromRunArtifact(
                artifact,
                totalOrders: 50,
                pathEfficiency: 0.925f);

            Assert.That(snapshot.CompletedRate, Is.EqualTo(0.86f).Within(0.001f));
            Assert.That(snapshot.AverageWaitMinutes, Is.EqualTo(0f).Within(0.001f));
            Assert.That(snapshot.ProcessedOrders, Is.EqualTo(43));
            Assert.That(snapshot.TotalOrders, Is.EqualTo(50));
            Assert.That(snapshot.PathEfficiency, Is.EqualTo(0.925f).Within(0.001f));
            Assert.That(snapshot.SimulationTime, Is.EqualTo(125f).Within(0.001f));
            Assert.That(snapshot.TotalDuration, Is.EqualTo(600f).Within(0.001f));
        }

        private KpiHudPanel CreatePanel()
        {
            root = new GameObject("KpiHudPanelTests");
            return root.AddComponent<KpiHudPanel>();
        }

        private static VisualElement BuildUi(
            out Label completionRate,
            out Label averageWait,
            out Label processedOrders,
            out Label pathEfficiency,
            out Label simulationTime,
            out Button speedButton)
        {
            var rootElement = new VisualElement();
            completionRate = new Label { name = KpiHudPanel.CompletionRateLabelName };
            averageWait = new Label { name = KpiHudPanel.AverageWaitLabelName };
            processedOrders = new Label { name = KpiHudPanel.ProcessedOrdersLabelName };
            pathEfficiency = new Label { name = KpiHudPanel.PathEfficiencyLabelName };
            simulationTime = new Label { name = KpiHudPanel.SimulationTimeLabelName };
            speedButton = new Button { name = KpiHudPanel.SpeedButtonName };

            rootElement.Add(completionRate);
            rootElement.Add(averageWait);
            rootElement.Add(processedOrders);
            rootElement.Add(pathEfficiency);
            rootElement.Add(simulationTime);
            rootElement.Add(speedButton);

            return rootElement;
        }

        private sealed class FakePlayback : KpiHudPanel.IPlaybackControls
        {
            public string SpeedLabel { get; set; } = KpiHudPanel.DefaultSpeedLabel;

            public string NextSpeedLabel { get; set; }

            public int CycleCount { get; private set; }

            public void CycleSpeed()
            {
                CycleCount++;
                if (!string.IsNullOrEmpty(NextSpeedLabel))
                {
                    SpeedLabel = NextSpeedLabel;
                }
            }
        }
    }
}
