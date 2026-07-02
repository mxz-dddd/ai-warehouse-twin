using System;
using System.Globalization;
using AIWarehouseTwin.UI.Showcase;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public enum ABCompareMode
    {
        A,
        B,
        SideBySide
    }

    public readonly struct ABCompareSnapshot
    {
        public static ABCompareSnapshot Default { get; } = new(
            baselineCompletionRate: 0f,
            baselineAverageWaitMinutes: 0f,
            optimizedCompletionRate: 0f,
            optimizedAverageWaitMinutes: 0f,
            improvementPercent: 0f,
            honestyNote: string.Empty);

        public ABCompareSnapshot(
            float baselineCompletionRate,
            float baselineAverageWaitMinutes,
            float optimizedCompletionRate,
            float optimizedAverageWaitMinutes,
            float improvementPercent,
            string honestyNote = "")
        {
            BaselineCompletionRate = SanitizeRatio(baselineCompletionRate);
            BaselineAverageWaitMinutes = SanitizeNonNegative(baselineAverageWaitMinutes);
            OptimizedCompletionRate = SanitizeRatio(optimizedCompletionRate);
            OptimizedAverageWaitMinutes = SanitizeNonNegative(optimizedAverageWaitMinutes);
            ImprovementPercent = SanitizeSigned(improvementPercent);
            HonestyNote = honestyNote ?? string.Empty;
        }

        public float BaselineCompletionRate { get; }

        public float BaselineAverageWaitMinutes { get; }

        public float OptimizedCompletionRate { get; }

        public float OptimizedAverageWaitMinutes { get; }

        public float ImprovementPercent { get; }

        public string HonestyNote { get; }

        public static ABCompareSnapshot FromShowcaseViewModel(AbShowcaseViewModel model)
        {
            if (model == null || !model.IsAvailable)
            {
                return Default;
            }

            var completionRow = FindRow(model, "completion_rate", "completed", "throughput");
            var waitRow = FindRow(model, "avg_wait", "average_wait", "wait", "order_cycle_p50_ms");
            var improvementRow = completionRow ?? waitRow;

            return new ABCompareSnapshot(
                baselineCompletionRate: completionRow != null ? ToRate(completionRow.BaselineValue) : 0f,
                baselineAverageWaitMinutes: waitRow != null ? ToMinutes(waitRow.BaselineValue) : 0f,
                optimizedCompletionRate: completionRow != null ? ToRate(completionRow.CandidateValue) : 0f,
                optimizedAverageWaitMinutes: waitRow != null ? ToMinutes(waitRow.CandidateValue) : 0f,
                improvementPercent: improvementRow?.ImprovementPct.HasValue == true
                    ? (float)improvementRow.ImprovementPct.Value
                    : 0f,
                honestyNote: model.EvidenceLabel);
        }

        private static AbShowcaseKpiRow FindRow(AbShowcaseViewModel model, params string[] tokens)
        {
            foreach (var row in model.KpiRows)
            {
                var metricName = row.MetricName ?? string.Empty;
                foreach (var token in tokens)
                {
                    if (metricName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return row;
                    }
                }
            }

            return null;
        }

        private static float ToRate(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0f;
            }

            return (float)(value > 1d ? value / 100d : value);
        }

        private static float ToMinutes(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0f;
            }

            return (float)(value > 1000d ? value / 60000d : value);
        }

        private static float SanitizeRatio(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp01(value);
        }

        private static float SanitizeNonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : Mathf.Max(0f, value);
        }

        private static float SanitizeSigned(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }
    }

    public class ABComparePanel : MonoBehaviour
    {
        public const string BaselineCompletionRateLabelName = "ab-baseline-completion-rate-label";
        public const string BaselineAverageWaitLabelName = "ab-baseline-average-wait-label";
        public const string OptimizedCompletionRateLabelName = "ab-optimized-completion-rate-label";
        public const string OptimizedAverageWaitLabelName = "ab-optimized-average-wait-label";
        public const string ImprovementLabelName = "ab-improvement-label";
        public const string HonestyNoteLabelName = "ab-honesty-note-label";
        public const string BaselineButtonName = "ab-mode-a-button";
        public const string OptimizedButtonName = "ab-mode-b-button";
        public const string SideBySideButtonName = "ab-mode-side-by-side-button";
        public const string ZeroImprovementHonestyNote = "优化差异待进一步仿真迭代";

        [SerializeField] private UIDocument document;

        private Label baselineCompletionRateLabel;
        private Label baselineAverageWaitLabel;
        private Label optimizedCompletionRateLabel;
        private Label optimizedAverageWaitLabel;
        private Label improvementLabel;
        private Label honestyNoteLabel;
        private Button baselineButton;
        private Button optimizedButton;
        private Button sideBySideButton;

        public Action<ABCompareMode> OnModeChanged { get; set; }

        public Func<ABCompareSnapshot> SnapshotProvider { get; set; }

        public ABCompareSnapshot CurrentSnapshot { get; private set; } = ABCompareSnapshot.Default;

        public ABCompareMode CurrentMode { get; private set; } = ABCompareMode.SideBySide;

        public string BaselineCompletionRateText { get; private set; } = FormatCompletionRate(0f);

        public string BaselineAverageWaitText { get; private set; } = FormatAverageWaitMinutes(0f);

        public string OptimizedCompletionRateText { get; private set; } = FormatCompletionRate(0f);

        public string OptimizedAverageWaitText { get; private set; } = FormatAverageWaitMinutes(0f);

        public string ImprovementText { get; private set; } = FormatImprovementPercent(0f);

        public string HonestyNoteText { get; private set; } = ZeroImprovementHonestyNote;

        public bool HonestyNoteVisible { get; private set; } = true;

        private void Awake()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            if (document != null)
            {
                Bind(document.rootVisualElement);
            }

            RefreshNow();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        public void Bind(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            UnbindButtons();

            baselineCompletionRateLabel = root.Q<Label>(BaselineCompletionRateLabelName);
            baselineAverageWaitLabel = root.Q<Label>(BaselineAverageWaitLabelName);
            optimizedCompletionRateLabel = root.Q<Label>(OptimizedCompletionRateLabelName);
            optimizedAverageWaitLabel = root.Q<Label>(OptimizedAverageWaitLabelName);
            improvementLabel = root.Q<Label>(ImprovementLabelName);
            honestyNoteLabel = root.Q<Label>(HonestyNoteLabelName);
            baselineButton = root.Q<Button>(BaselineButtonName);
            optimizedButton = root.Q<Button>(OptimizedButtonName);
            sideBySideButton = root.Q<Button>(SideBySideButtonName);

            if (baselineButton != null) baselineButton.clicked += ShowBaseline;
            if (optimizedButton != null) optimizedButton.clicked += ShowOptimized;
            if (sideBySideButton != null) sideBySideButton.clicked += ShowSideBySide;

            PushStateToUi();
        }

        public void RefreshNow()
        {
            if (SnapshotProvider != null)
            {
                SetSnapshot(SnapshotProvider.Invoke());
                return;
            }

            SetSnapshot(CurrentSnapshot);
        }

        public void SetSnapshot(ABCompareSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
            BaselineCompletionRateText = FormatCompletionRate(snapshot.BaselineCompletionRate);
            BaselineAverageWaitText = FormatAverageWaitMinutes(snapshot.BaselineAverageWaitMinutes);
            OptimizedCompletionRateText = FormatCompletionRate(snapshot.OptimizedCompletionRate);
            OptimizedAverageWaitText = FormatAverageWaitMinutes(snapshot.OptimizedAverageWaitMinutes);
            ImprovementText = FormatImprovementPercent(snapshot.ImprovementPercent);
            HonestyNoteText = ResolveHonestyNote(snapshot);
            HonestyNoteVisible = !string.IsNullOrWhiteSpace(HonestyNoteText);
            PushStateToUi();
        }

        public void ShowBaseline()
        {
            SetMode(ABCompareMode.A);
        }

        public void ShowOptimized()
        {
            SetMode(ABCompareMode.B);
        }

        public void ShowSideBySide()
        {
            SetMode(ABCompareMode.SideBySide);
        }

        public static string FormatCompletionRate(float completionRate)
        {
            return $"{FormatOneDecimal(SanitizeRatio(completionRate) * 100f)}%";
        }

        public static string FormatAverageWaitMinutes(float averageWaitMinutes)
        {
            return $"{FormatOneDecimal(SanitizeNonNegative(averageWaitMinutes))} min";
        }

        public static string FormatImprovementPercent(float improvementPercent)
        {
            var value = float.IsNaN(improvementPercent) || float.IsInfinity(improvementPercent)
                ? 0f
                : improvementPercent;
            var sign = value >= 0f ? "+" : string.Empty;
            return $"{sign}{FormatOneDecimal(value)}%";
        }

        private void SetMode(ABCompareMode mode)
        {
            CurrentMode = mode;
            PushModeToUi();
            OnModeChanged?.Invoke(mode);
        }

        private void PushStateToUi()
        {
            if (baselineCompletionRateLabel != null) baselineCompletionRateLabel.text = BaselineCompletionRateText;
            if (baselineAverageWaitLabel != null) baselineAverageWaitLabel.text = BaselineAverageWaitText;
            if (optimizedCompletionRateLabel != null) optimizedCompletionRateLabel.text = OptimizedCompletionRateText;
            if (optimizedAverageWaitLabel != null) optimizedAverageWaitLabel.text = OptimizedAverageWaitText;
            if (improvementLabel != null) improvementLabel.text = ImprovementText;
            if (honestyNoteLabel != null)
            {
                honestyNoteLabel.text = HonestyNoteText;
                honestyNoteLabel.style.display = HonestyNoteVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            PushModeToUi();
        }

        private void PushModeToUi()
        {
            if (baselineButton != null) baselineButton.SetEnabled(CurrentMode != ABCompareMode.A);
            if (optimizedButton != null) optimizedButton.SetEnabled(CurrentMode != ABCompareMode.B);
            if (sideBySideButton != null) sideBySideButton.SetEnabled(CurrentMode != ABCompareMode.SideBySide);
        }

        private void UnbindButtons()
        {
            if (baselineButton != null) baselineButton.clicked -= ShowBaseline;
            if (optimizedButton != null) optimizedButton.clicked -= ShowOptimized;
            if (sideBySideButton != null) sideBySideButton.clicked -= ShowSideBySide;
        }

        private static string ResolveHonestyNote(ABCompareSnapshot snapshot)
        {
            if (Mathf.Abs(snapshot.ImprovementPercent) <= 0.0001f)
            {
                return ZeroImprovementHonestyNote;
            }

            return snapshot.HonestyNote;
        }

        private static string FormatOneDecimal(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static float SanitizeRatio(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp01(value);
        }

        private static float SanitizeNonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : Mathf.Max(0f, value);
        }
    }
}
