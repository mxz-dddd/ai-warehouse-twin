using System;
using System.Linq;
using AIWarehouseTwin.UI;
using AIWarehouseTwin.UI.Showcase;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.Tests
{
    public sealed class ABComparePanelTests
    {
        [Test]
        public void RefreshUi_uses_view_model_display_fields_without_recomputing_delta()
        {
            var model = new AbShowcaseViewModel(
                true,
                string.Empty,
                true,
                "fixture source",
                "fixture evidence",
                new AbShowcaseScenarioSummary("Baseline", "baseline", "base-scenario"),
                new AbShowcaseScenarioSummary("Optimized", "candidate", "candidate-scenario"),
                new[]
                {
                    new AbShowcaseKpiRow(
                        "total_work_item_throughput_per_hour",
                        1,
                        2,
                        999,
                        123,
                        false,
                        "higher_is_better",
                        "baseline-display",
                        "candidate-display",
                        "delta-display-from-model",
                        "improvement-display-from-model",
                        "Higher is better",
                        "Improvement",
                        "fixture source")
                },
                1);
            var root = new VisualElement();

            ABComparePanel.RefreshUi(model, root);

            var labels = Labels(root);
            Assert.That(labels, Has.Member("delta-display-from-model"));
            Assert.That(labels, Has.Member("improvement-display-from-model"));
            Assert.That(labels, Has.No.Member("+100%"));
            Assert.That(labels, Has.No.Member("999"));
        }

        [Test]
        public void RefreshUi_unavailable_model_renders_reason_and_evidence()
        {
            var model = new AbShowcaseViewModel(
                false,
                "ComparisonArtifact unavailable: not found",
                true,
                "fixture source",
                "Demo only - fixture evidence",
                new AbShowcaseScenarioSummary("Baseline", "baseline", string.Empty),
                new AbShowcaseScenarioSummary("Optimized", "candidate", string.Empty),
                Array.Empty<AbShowcaseKpiRow>(),
                0);
            var root = new VisualElement();

            ABComparePanel.RefreshUi(model, root);

            var labels = Labels(root);
            Assert.That(labels, Has.Member("ComparisonArtifact unavailable: not found"));
            Assert.That(labels, Has.Member("Demo only - fixture evidence"));
        }

        [Test]
        public void RefreshUi_empty_available_model_renders_empty_delta_fallback()
        {
            var model = new AbShowcaseViewModel(
                true,
                string.Empty,
                true,
                "fixture source",
                "fixture evidence",
                new AbShowcaseScenarioSummary("Baseline", "baseline", "base"),
                new AbShowcaseScenarioSummary("Optimized", "candidate", "candidate"),
                Array.Empty<AbShowcaseKpiRow>(),
                0);
            var root = new VisualElement();

            ABComparePanel.RefreshUi(model, root);

            Assert.That(Labels(root), Has.Member("No KPI deltas available."));
        }

        private static string[] Labels(VisualElement root) =>
            root.Query<Label>().ToList().Select(label => label.text).ToArray();
    }
}
