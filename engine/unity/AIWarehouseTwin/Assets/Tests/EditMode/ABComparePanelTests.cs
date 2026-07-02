using System;
using System.Linq;
using AIWarehouseTwin.UI;
using AIWarehouseTwin.UI.Showcase;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.Tests
{
    public sealed class ABComparePanelTests
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
        public void Default_snapshot_formats_as_safe_compare_text()
        {
            var panel = CreatePanel();

            panel.RefreshNow();

            Assert.That(panel.BaselineCompletionRateText, Is.EqualTo("0.0%"));
            Assert.That(panel.BaselineAverageWaitText, Is.EqualTo("0.0 min"));
            Assert.That(panel.OptimizedCompletionRateText, Is.EqualTo("0.0%"));
            Assert.That(panel.OptimizedAverageWaitText, Is.EqualTo("0.0 min"));
            Assert.That(panel.ImprovementText, Is.EqualTo("+0.0%"));
            Assert.That(panel.HonestyNoteText, Is.EqualTo(ABComparePanel.ZeroImprovementHonestyNote));
            Assert.That(panel.HonestyNoteVisible, Is.True);
        }

        [Test]
        public void SetSnapshot_formats_baseline_and_optimized_completion_rates()
        {
            var panel = CreatePanel();

            panel.SetSnapshot(SampleSnapshot());

            Assert.That(panel.BaselineCompletionRateText, Is.EqualTo("72.0%"));
            Assert.That(panel.OptimizedCompletionRateText, Is.EqualTo("84.0%"));
        }

        [Test]
        public void SetSnapshot_formats_baseline_and_optimized_average_wait_minutes()
        {
            var panel = CreatePanel();

            panel.SetSnapshot(SampleSnapshot());

            Assert.That(panel.BaselineAverageWaitText, Is.EqualTo("4.1 min"));
            Assert.That(panel.OptimizedAverageWaitText, Is.EqualTo("3.2 min"));
        }

        [Test]
        public void SetSnapshot_formats_improvement_percent()
        {
            var panel = CreatePanel();

            panel.SetSnapshot(SampleSnapshot());

            Assert.That(panel.ImprovementText, Is.EqualTo("+15.0%"));
        }

        [Test]
        public void Zero_improvement_shows_honesty_note()
        {
            var panel = CreatePanel();

            panel.SetSnapshot(new ABCompareSnapshot(
                baselineCompletionRate: 0.72f,
                baselineAverageWaitMinutes: 4.1f,
                optimizedCompletionRate: 0.72f,
                optimizedAverageWaitMinutes: 4.1f,
                improvementPercent: 0f));

            Assert.That(panel.HonestyNoteText, Is.EqualTo(ABComparePanel.ZeroImprovementHonestyNote));
            Assert.That(panel.HonestyNoteVisible, Is.True);
        }

        [Test]
        public void Positive_improvement_uses_optional_honesty_note()
        {
            var panel = CreatePanel();

            panel.SetSnapshot(SampleSnapshot("仿真样本仍需扩展"));

            Assert.That(panel.HonestyNoteText, Is.EqualTo("仿真样本仍需扩展"));
            Assert.That(panel.HonestyNoteVisible, Is.True);
        }

        [Test]
        public void Bind_pushes_snapshot_text_to_ui_labels()
        {
            var panel = CreatePanel();
            var rootElement = BuildUi(
                out var baselineCompletion,
                out var baselineWait,
                out var optimizedCompletion,
                out var optimizedWait,
                out var improvement,
                out var honesty,
                out _,
                out _,
                out _);

            panel.Bind(rootElement);
            panel.SetSnapshot(SampleSnapshot());

            Assert.That(baselineCompletion.text, Is.EqualTo("72.0%"));
            Assert.That(baselineWait.text, Is.EqualTo("4.1 min"));
            Assert.That(optimizedCompletion.text, Is.EqualTo("84.0%"));
            Assert.That(optimizedWait.text, Is.EqualTo("3.2 min"));
            Assert.That(improvement.text, Is.EqualTo("+15.0%"));
            Assert.That(honesty.text, Is.EqualTo(string.Empty));
            Assert.That(honesty.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void ShowBaseline_triggers_mode_changed_to_a()
        {
            var panel = CreatePanel();
            ABCompareMode? changedMode = null;
            panel.OnModeChanged = mode => changedMode = mode;

            panel.ShowBaseline();

            Assert.That(panel.CurrentMode, Is.EqualTo(ABCompareMode.A));
            Assert.That(changedMode, Is.EqualTo(ABCompareMode.A));
        }

        [Test]
        public void ShowOptimized_triggers_mode_changed_to_b()
        {
            var panel = CreatePanel();
            ABCompareMode? changedMode = null;
            panel.OnModeChanged = mode => changedMode = mode;

            panel.ShowOptimized();

            Assert.That(panel.CurrentMode, Is.EqualTo(ABCompareMode.B));
            Assert.That(changedMode, Is.EqualTo(ABCompareMode.B));
        }

        [Test]
        public void ShowSideBySide_triggers_mode_changed_to_side_by_side()
        {
            var panel = CreatePanel();
            ABCompareMode? changedMode = null;
            panel.OnModeChanged = mode => changedMode = mode;

            panel.ShowSideBySide();

            Assert.That(panel.CurrentMode, Is.EqualTo(ABCompareMode.SideBySide));
            Assert.That(changedMode, Is.EqualTo(ABCompareMode.SideBySide));
        }

        [Test]
        public void RefreshNow_without_bound_controls_does_not_throw()
        {
            var panel = CreatePanel();
            panel.SnapshotProvider = SampleSnapshot;

            Assert.DoesNotThrow(() => panel.RefreshNow());
            Assert.That(panel.BaselineCompletionRateText, Is.EqualTo("72.0%"));
        }

        [Test]
        public void Multiple_mode_switches_do_not_throw_and_keep_final_mode()
        {
            var panel = CreatePanel();

            Assert.DoesNotThrow(() =>
            {
                panel.ShowBaseline();
                panel.ShowOptimized();
                panel.ShowSideBySide();
                panel.ShowOptimized();
            });

            Assert.That(panel.CurrentMode, Is.EqualTo(ABCompareMode.B));
        }

        [Test]
        public void FromShowcaseViewModel_reuses_ab_showcase_kpi_rows_when_available()
        {
            var model = new AbShowcaseViewModel(
                isAvailable: true,
                unavailableReason: string.Empty,
                isMock: true,
                sourceLabel: "test",
                evidenceLabel: "not enough samples",
                baseline: new AbShowcaseScenarioSummary("Baseline", "baseline", "a"),
                candidate: new AbShowcaseScenarioSummary("Optimized", "candidate", "b"),
                kpiRows: new[]
                {
                    new AbShowcaseKpiRow(
                        metricName: "completion_rate",
                        baselineValue: 0.72d,
                        candidateValue: 0.84d,
                        delta: 0.12d,
                        improvementPct: 15d,
                        lowerIsBetter: false,
                        direction: "higher_is_better",
                        baselineDisplay: "0.72",
                        candidateDisplay: "0.84",
                        deltaDisplay: "+0.12",
                        improvementDisplay: "+15.0%",
                        directionLabel: "Higher is better",
                        trendLabel: "Improvement",
                        sourceLabel: "test"),
                    new AbShowcaseKpiRow(
                        metricName: "avg_wait_ms",
                        baselineValue: 246000d,
                        candidateValue: 192000d,
                        delta: -54000d,
                        improvementPct: 21.95d,
                        lowerIsBetter: true,
                        direction: "lower_is_better",
                        baselineDisplay: "246000",
                        candidateDisplay: "192000",
                        deltaDisplay: "-54000",
                        improvementDisplay: "+22.0%",
                        directionLabel: "Lower is better",
                        trendLabel: "Improvement",
                        sourceLabel: "test")
                },
                deltaCount: 2);

            var snapshot = ABCompareSnapshot.FromShowcaseViewModel(model);

            Assert.That(snapshot.BaselineCompletionRate, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(snapshot.OptimizedCompletionRate, Is.EqualTo(0.84f).Within(0.001f));
            Assert.That(snapshot.BaselineAverageWaitMinutes, Is.EqualTo(4.1f).Within(0.001f));
            Assert.That(snapshot.OptimizedAverageWaitMinutes, Is.EqualTo(3.2f).Within(0.001f));
            Assert.That(snapshot.ImprovementPercent, Is.EqualTo(15f).Within(0.001f));
            Assert.That(snapshot.HonestyNote, Is.EqualTo("not enough samples"));
        }

        [Test]
        public void RefreshUi_uses_view_model_display_fields_without_recomputing_delta()
        {
            var rootElement = new VisualElement();
            var model = new AbShowcaseViewModel(
                isAvailable: true,
                unavailableReason: string.Empty,
                isMock: false,
                sourceLabel: "source-label",
                evidenceLabel: "evidence-label",
                baseline: new AbShowcaseScenarioSummary("Baseline", "baseline", "baseline-scenario"),
                candidate: new AbShowcaseScenarioSummary("Optimized", "candidate", "candidate-scenario"),
                kpiRows: new[]
                {
                    new AbShowcaseKpiRow(
                        metricName: "total_work_item_throughput_per_hour",
                        baselineValue: 100d,
                        candidateValue: 200d,
                        delta: 999d,
                        improvementPct: 100d,
                        lowerIsBetter: false,
                        direction: "higher_is_better",
                        baselineDisplay: "baseline-display-from-model",
                        candidateDisplay: "candidate-display-from-model",
                        deltaDisplay: "delta-display-from-model",
                        improvementDisplay: "improvement-display-from-model",
                        directionLabel: "direction-display-from-model",
                        trendLabel: "trend-display-from-model",
                        sourceLabel: "row-source")
                },
                deltaCount: 1);

            ABComparePanel.RefreshUi(model, rootElement);

            var labels = RenderedLabels(rootElement);
            Assert.That(labels, Does.Contain("baseline-display-from-model"));
            Assert.That(labels, Does.Contain("candidate-display-from-model"));
            Assert.That(labels, Does.Contain("delta-display-from-model"));
            Assert.That(labels, Does.Contain("improvement-display-from-model"));
            Assert.That(labels, Does.Contain("direction-display-from-model | trend-display-from-model"));
            Assert.That(labels, Does.Not.Contain("999"));
            Assert.That(labels, Does.Not.Contain("+100.0%"));
        }

        [Test]
        public void RefreshUi_unavailable_model_renders_reason_and_evidence()
        {
            var rootElement = new VisualElement();
            var model = new AbShowcaseViewModel(
                isAvailable: false,
                unavailableReason: "fixture missing",
                isMock: true,
                sourceLabel: "source-label",
                evidenceLabel: "evidence-label",
                baseline: new AbShowcaseScenarioSummary("Baseline", "baseline", string.Empty),
                candidate: new AbShowcaseScenarioSummary("Optimized", "candidate", string.Empty),
                kpiRows: Array.Empty<AbShowcaseKpiRow>(),
                deltaCount: 0);

            ABComparePanel.RefreshUi(model, rootElement);

            var labels = RenderedLabels(rootElement);
            Assert.That(labels, Does.Contain("source-label"));
            Assert.That(labels, Does.Contain("evidence-label"));
            Assert.That(labels, Does.Contain("fixture missing"));
        }

        [Test]
        public void RefreshUi_empty_available_model_renders_empty_delta_fallback()
        {
            var rootElement = new VisualElement();
            var model = new AbShowcaseViewModel(
                isAvailable: true,
                unavailableReason: string.Empty,
                isMock: false,
                sourceLabel: "source-label",
                evidenceLabel: "evidence-label",
                baseline: new AbShowcaseScenarioSummary("Baseline", "baseline", "baseline"),
                candidate: new AbShowcaseScenarioSummary("Optimized", "candidate", "candidate"),
                kpiRows: Array.Empty<AbShowcaseKpiRow>(),
                deltaCount: 0);

            ABComparePanel.RefreshUi(model, rootElement);

            Assert.That(RenderedLabels(rootElement), Does.Contain("No KPI deltas available."));
        }

        private ABComparePanel CreatePanel()
        {
            root = new GameObject("ABComparePanelTests");
            return root.AddComponent<ABComparePanel>();
        }

        private static ABCompareSnapshot SampleSnapshot()
        {
            return SampleSnapshot(string.Empty);
        }

        private static ABCompareSnapshot SampleSnapshot(string note)
        {
            return new ABCompareSnapshot(
                baselineCompletionRate: 0.72f,
                baselineAverageWaitMinutes: 4.1f,
                optimizedCompletionRate: 0.84f,
                optimizedAverageWaitMinutes: 3.2f,
                improvementPercent: 15f,
                honestyNote: note);
        }

        private static VisualElement BuildUi(
            out Label baselineCompletion,
            out Label baselineWait,
            out Label optimizedCompletion,
            out Label optimizedWait,
            out Label improvement,
            out Label honesty,
            out Button baselineButton,
            out Button optimizedButton,
            out Button sideBySideButton)
        {
            var rootElement = new VisualElement();
            baselineCompletion = new Label { name = ABComparePanel.BaselineCompletionRateLabelName };
            baselineWait = new Label { name = ABComparePanel.BaselineAverageWaitLabelName };
            optimizedCompletion = new Label { name = ABComparePanel.OptimizedCompletionRateLabelName };
            optimizedWait = new Label { name = ABComparePanel.OptimizedAverageWaitLabelName };
            improvement = new Label { name = ABComparePanel.ImprovementLabelName };
            honesty = new Label { name = ABComparePanel.HonestyNoteLabelName };
            baselineButton = new Button { name = ABComparePanel.BaselineButtonName };
            optimizedButton = new Button { name = ABComparePanel.OptimizedButtonName };
            sideBySideButton = new Button { name = ABComparePanel.SideBySideButtonName };

            rootElement.Add(baselineCompletion);
            rootElement.Add(baselineWait);
            rootElement.Add(optimizedCompletion);
            rootElement.Add(optimizedWait);
            rootElement.Add(improvement);
            rootElement.Add(honesty);
            rootElement.Add(baselineButton);
            rootElement.Add(optimizedButton);
            rootElement.Add(sideBySideButton);
            return rootElement;
        }

        private static string[] RenderedLabels(VisualElement rootElement)
        {
            return rootElement.Query<Label>().ToList().Select(label => label.text).ToArray();
        }
    }
}
