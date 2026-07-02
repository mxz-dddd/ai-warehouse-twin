using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public static class KpiHudPanel
    {
        public static void RefreshUi(RunArtifactPlayerState state, VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            root.Clear();

            if (state == null || state.KpiHudRows == null || state.KpiHudRows.Length == 0)
            {
                root.Add(CreateMutedLabel("No KPI data"));
                return;
            }

            foreach (var section in state.KpiHudRows.GroupBy(row => row.Section))
            {
                var sectionElement = new VisualElement();
                sectionElement.style.marginBottom = 10;
                sectionElement.Add(CreateSectionLabel(section.Key));

                foreach (var row in section)
                {
                    sectionElement.Add(CreateRow(row));
                }

                root.Add(sectionElement);
            }
        }

        private static VisualElement CreateRow(KpiSummaryRow row)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.marginBottom = 3;

            var label = CreateMutedLabel(row.Label);
            label.style.flexGrow = 1;
            label.style.marginRight = 8;

            var value = new Label(row.Value);
            value.style.unityTextAlign = TextAnchor.MiddleRight;
            value.style.flexShrink = 0;

            container.Add(label);
            container.Add(value);
            return container;
        }

        private static Label CreateSectionLabel(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 4;
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
