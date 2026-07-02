using AIWarehouseTwin.UI.Showcase;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public static class ABComparePanel
    {
        public static void RefreshUi(AbShowcaseViewModel model, VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            root.Clear();

            if (model == null)
            {
                root.Add(CreateMutedLabel("ComparisonArtifact unavailable: model is missing."));
                return;
            }

            root.Add(CreateHeader(model));
            root.Add(CreateMutedLabel(model.SourceLabel));
            root.Add(CreateMutedLabel(model.EvidenceLabel));

            if (!model.IsAvailable)
            {
                root.Add(CreateMutedLabel(model.UnavailableReason));
                return;
            }

            if (model.KpiRows.Count == 0)
            {
                root.Add(CreateMutedLabel("No KPI deltas available."));
                return;
            }

            foreach (var row in model.KpiRows)
            {
                root.Add(CreateKpiRow(row));
            }
        }

        private static VisualElement CreateHeader(AbShowcaseViewModel model)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.marginBottom = 4;

            var baseline = CreateTitleLabel($"{model.Baseline.DisplayLabel}: {model.Baseline.ScenarioId}");
            var candidate = CreateTitleLabel($"{model.Candidate.DisplayLabel}: {model.Candidate.ScenarioId}");
            candidate.style.unityTextAlign = TextAnchor.MiddleRight;

            container.Add(baseline);
            container.Add(candidate);
            return container;
        }

        private static VisualElement CreateKpiRow(AbShowcaseKpiRow row)
        {
            var container = new VisualElement();
            container.style.marginTop = 8;
            container.style.paddingTop = 6;
            container.style.borderTopColor = new Color(0.19f, 0.22f, 0.26f);
            container.style.borderTopWidth = 1;

            container.Add(CreateTitleLabel(row.MetricName));
            container.Add(CreateValueLine("Baseline", row.BaselineDisplay));
            container.Add(CreateValueLine("Candidate", row.CandidateDisplay));
            container.Add(CreateValueLine("Delta", row.DeltaDisplay));
            container.Add(CreateValueLine("Improvement", row.ImprovementDisplay));
            container.Add(CreateMutedLabel($"{row.DirectionLabel} | {row.TrendLabel}"));
            return container;
        }

        private static VisualElement CreateValueLine(string labelText, string valueText)
        {
            var line = new VisualElement();
            line.style.flexDirection = FlexDirection.Row;
            line.style.justifyContent = Justify.SpaceBetween;

            line.Add(CreateMutedLabel(labelText));

            var value = new Label(valueText);
            value.style.unityTextAlign = TextAnchor.MiddleRight;
            line.Add(value);
            return line;
        }

        private static Label CreateTitleLabel(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private static Label CreateMutedLabel(string text)
        {
            var label = new Label(text);
            label.style.color = new StyleColor(new Color(0.67f, 0.71f, 0.75f));
            return label;
        }
    }
}
