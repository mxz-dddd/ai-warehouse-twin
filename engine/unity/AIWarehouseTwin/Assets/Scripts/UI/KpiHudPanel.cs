using System;
using System.Globalization;
using System.Linq;
using AIWarehouseTwin.Playback;
using Sim.Contracts.Artifacts;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public readonly struct KpiHudSnapshot
    {
        public static KpiHudSnapshot Default { get; } = new(
            completedRate: 0f,
            averageWaitMinutes: 0f,
            processedOrders: 0,
            totalOrders: 0,
            pathEfficiency: 0f,
            simulationTime: 0f,
            totalDuration: 0f);

        public KpiHudSnapshot(
            float completedRate,
            float averageWaitMinutes,
            int processedOrders,
            int totalOrders,
            float pathEfficiency,
            float simulationTime,
            float totalDuration)
        {
            CompletedRate = SanitizeRatio(completedRate, clampUpper: true);
            AverageWaitMinutes = SanitizeNonNegative(averageWaitMinutes);
            ProcessedOrders = Mathf.Max(0, processedOrders);
            TotalOrders = Mathf.Max(0, totalOrders);
            PathEfficiency = SanitizeRatio(pathEfficiency, clampUpper: false);
            SimulationTime = SanitizeNonNegative(simulationTime);
            TotalDuration = SanitizeNonNegative(totalDuration);
        }

        public float CompletedRate { get; }

        public float AverageWaitMinutes { get; }

        public int ProcessedOrders { get; }

        public int TotalOrders { get; }

        public float PathEfficiency { get; }

        public float SimulationTime { get; }

        public float TotalDuration { get; }

        public static KpiHudSnapshot FromRunArtifact(
            RunArtifact artifact,
            int totalOrders = 0,
            float pathEfficiency = 0f)
        {
            if (artifact?.KpiSummary == null)
            {
                return Default;
            }

            var kpi = artifact.KpiSummary;
            var processedOrders = Mathf.Max(0, kpi.TotalCompletedWorkItems);
            var resolvedTotalOrders = totalOrders > 0 ? totalOrders : processedOrders;
            var completedRate = resolvedTotalOrders > 0
                ? processedOrders / (float)resolvedTotalOrders
                : 0f;
            var totalDurationSeconds = kpi.TotalDurationMs > 0
                ? kpi.TotalDurationMs / 1000f
                : 0f;
            var simulationTimeSeconds = artifact.FinalWorldTimeMs > 0
                ? artifact.FinalWorldTimeMs / 1000f
                : totalDurationSeconds;
            var averageWaitMinutes = TryReadDecimalProperty(kpi, "AvgWaitMs", out var avgWaitMs)
                ? (float)avgWaitMs / 60000f
                : 0f;

            return new KpiHudSnapshot(
                completedRate,
                averageWaitMinutes,
                processedOrders,
                resolvedTotalOrders,
                pathEfficiency,
                simulationTimeSeconds,
                totalDurationSeconds);
        }

        private static float SanitizeRatio(float value, bool clampUpper)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return clampUpper ? Mathf.Clamp01(value) : Mathf.Max(0f, value);
        }

        private static float SanitizeNonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : Mathf.Max(0f, value);
        }

        private static bool TryReadDecimalProperty(object target, string propertyName, out decimal value)
        {
            value = 0m;
            if (target == null)
            {
                return false;
            }

            var property = target.GetType().GetProperty(propertyName);
            if (property == null)
            {
                return false;
            }

            var rawValue = property.GetValue(target);
            if (rawValue == null)
            {
                return false;
            }

            try
            {
                value = Convert.ToDecimal(rawValue, CultureInfo.InvariantCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }
    }

    public class KpiHudPanel : MonoBehaviour
    {
        public const string CompletionRateLabelName = "completion-rate-label";
        public const string AverageWaitLabelName = "average-wait-label";
        public const string ProcessedOrdersLabelName = "processed-orders-label";
        public const string PathEfficiencyLabelName = "path-efficiency-label";
        public const string SimulationTimeLabelName = "simulation-time-label";
        public const string SpeedButtonName = "speed-button";
        public const string DefaultSpeedLabel = "1×";

        private static readonly IPlaybackControls DefaultPlaybackControls = new PlaybackControllerAdapter();

        [SerializeField] private UIDocument document;
        [SerializeField] private float refreshIntervalSeconds = 1f;
        [SerializeField] private float valueLerpSeconds;

        private Label completionRateLabel;
        private Label averageWaitLabel;
        private Label processedOrdersLabel;
        private Label pathEfficiencyLabel;
        private Label simulationTimeLabel;
        private Button speedButton;
        private float refreshElapsedSeconds;
        private IPlaybackControls playbackControls;

        public interface IPlaybackControls
        {
            string SpeedLabel { get; }

            void CycleSpeed();
        }

        public Func<KpiHudSnapshot> SnapshotProvider { get; set; }

        public IPlaybackControls PlaybackControls
        {
            get => playbackControls;
            set
            {
                playbackControls = value;
                RefreshSpeedLabel();
            }
        }

        public float RefreshIntervalSeconds
        {
            get => refreshIntervalSeconds;
            set => refreshIntervalSeconds = Mathf.Max(0f, value);
        }

        public float ValueLerpSeconds
        {
            get => valueLerpSeconds;
            set => valueLerpSeconds = Mathf.Max(0f, value);
        }

        public KpiHudSnapshot CurrentSnapshot { get; private set; } = KpiHudSnapshot.Default;

        public string CompletionRateText { get; private set; } = FormatCompletionRate(0f);

        public string AverageWaitText { get; private set; } = FormatAverageWaitMinutes(0f);

        public string ProcessedOrdersText { get; private set; } = FormatProcessedOrders(0, 0);

        public string PathEfficiencyText { get; private set; } = FormatPathEfficiency(0f);

        public string SimulationTimeText { get; private set; } = FormatSimulationTime(0f, 0f);

        public string SpeedLabelText { get; private set; } = DefaultSpeedLabel;

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
            if (speedButton != null)
            {
                speedButton.clicked -= CycleSpeed;
            }
        }

        private void Update()
        {
            if (refreshIntervalSeconds <= 0f)
            {
                return;
            }

            refreshElapsedSeconds += Time.unscaledDeltaTime;
            if (refreshElapsedSeconds >= refreshIntervalSeconds)
            {
                refreshElapsedSeconds = 0f;
                RefreshNow();
            }
        }

        public void Bind(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            if (speedButton != null)
            {
                speedButton.clicked -= CycleSpeed;
            }

            completionRateLabel = root.Q<Label>(CompletionRateLabelName);
            averageWaitLabel = root.Q<Label>(AverageWaitLabelName);
            processedOrdersLabel = root.Q<Label>(ProcessedOrdersLabelName);
            pathEfficiencyLabel = root.Q<Label>(PathEfficiencyLabelName);
            simulationTimeLabel = root.Q<Label>(SimulationTimeLabelName);
            speedButton = root.Q<Button>(SpeedButtonName);

            if (speedButton != null)
            {
                speedButton.clicked += CycleSpeed;
            }

            PushTextsToUi();
        }

        public void RefreshNow()
        {
            refreshElapsedSeconds = 0f;
            var snapshot = SnapshotProvider != null
                ? SnapshotProvider.Invoke()
                : KpiHudSnapshot.Default;

            SetSnapshot(ResolveDisplaySnapshot(CurrentSnapshot, snapshot, valueLerpSeconds));
            RefreshSpeedLabel();
        }

        public void CycleSpeed()
        {
            ResolvePlaybackControls().CycleSpeed();
            RefreshSpeedLabel();
        }

        public void SetSnapshot(KpiHudSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
            CompletionRateText = FormatCompletionRate(snapshot.CompletedRate);
            AverageWaitText = FormatAverageWaitMinutes(snapshot.AverageWaitMinutes);
            ProcessedOrdersText = FormatProcessedOrders(snapshot.ProcessedOrders, snapshot.TotalOrders);
            PathEfficiencyText = FormatPathEfficiency(snapshot.PathEfficiency);
            SimulationTimeText = FormatSimulationTime(snapshot.SimulationTime, snapshot.TotalDuration);
            PushTextsToUi();
        }

        public static void RefreshUi(RunArtifactPlayerState state, VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            root.Clear();

            if (state == null || state.KpiHudRows == null || state.KpiHudRows.Length == 0)
            {
                root.Add(CreateStructuredMutedLabel("No KPI data"));
                return;
            }

            foreach (var section in state.KpiHudRows.GroupBy(row => row.Section))
            {
                var sectionElement = new VisualElement();
                sectionElement.style.marginBottom = 10;
                sectionElement.Add(CreateStructuredSectionLabel(section.Key));

                foreach (var row in section)
                {
                    sectionElement.Add(CreateStructuredRow(row));
                }

                root.Add(sectionElement);
            }
        }

        public static string FormatCompletionRate(float completedRate)
        {
            return FormatPercent(completedRate);
        }

        public static string FormatAverageWaitMinutes(float averageWaitMinutes)
        {
            return $"{FormatOneDecimal(SanitizeNonNegative(averageWaitMinutes))} min";
        }

        public static string FormatProcessedOrders(int processedOrders, int totalOrders)
        {
            return $"{Mathf.Max(0, processedOrders)} / {Mathf.Max(0, totalOrders)}";
        }

        public static string FormatPathEfficiency(float pathEfficiency)
        {
            return FormatPercent(pathEfficiency);
        }

        public static string FormatSimulationTime(float simulationTime, float totalDuration)
        {
            return $"{FormatDuration(simulationTime)} / {FormatDuration(totalDuration)}";
        }

        protected virtual KpiHudSnapshot ResolveDisplaySnapshot(
            KpiHudSnapshot previousSnapshot,
            KpiHudSnapshot nextSnapshot,
            float lerpSeconds)
        {
            return nextSnapshot;
        }

        private void RefreshSpeedLabel()
        {
            SpeedLabelText = ResolvePlaybackControls().SpeedLabel;
            if (string.IsNullOrWhiteSpace(SpeedLabelText))
            {
                SpeedLabelText = DefaultSpeedLabel;
            }

            if (speedButton != null)
            {
                speedButton.text = SpeedLabelText;
            }
        }

        private IPlaybackControls ResolvePlaybackControls()
        {
            return playbackControls ?? DefaultPlaybackControls;
        }

        private void PushTextsToUi()
        {
            if (completionRateLabel != null) completionRateLabel.text = CompletionRateText;
            if (averageWaitLabel != null) averageWaitLabel.text = AverageWaitText;
            if (processedOrdersLabel != null) processedOrdersLabel.text = ProcessedOrdersText;
            if (pathEfficiencyLabel != null) pathEfficiencyLabel.text = PathEfficiencyText;
            if (simulationTimeLabel != null) simulationTimeLabel.text = SimulationTimeText;
            if (speedButton != null) speedButton.text = SpeedLabelText;
        }

        private static VisualElement CreateStructuredRow(KpiSummaryRow row)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.marginBottom = 3;

            var label = CreateStructuredMutedLabel(row.Label);
            label.style.flexGrow = 1;
            label.style.marginRight = 8;

            var value = new Label(row.Value);
            value.style.unityTextAlign = TextAnchor.MiddleRight;
            value.style.flexShrink = 0;

            container.Add(label);
            container.Add(value);
            return container;
        }

        private static Label CreateStructuredSectionLabel(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 4;
            return label;
        }

        private static Label CreateStructuredMutedLabel(string text)
        {
            var label = new Label(text);
            label.style.color = new StyleColor(new Color(0.67f, 0.71f, 0.75f));
            return label;
        }

        private static string FormatPercent(float ratio)
        {
            return $"{FormatOneDecimal(SanitizeNonNegative(ratio) * 100f)}%";
        }

        private static string FormatOneDecimal(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string FormatDuration(float seconds)
        {
            var totalSeconds = Mathf.FloorToInt(SanitizeNonNegative(seconds));
            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var remainingSeconds = totalSeconds % 60;

            return hours > 0
                ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", hours, minutes, remainingSeconds)
                : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes, remainingSeconds);
        }

        private static float SanitizeNonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : Mathf.Max(0f, value);
        }

        private sealed class PlaybackControllerAdapter : IPlaybackControls
        {
            public string SpeedLabel => PlaybackController.Instance != null
                ? PlaybackController.Instance.SpeedLabel
                : DefaultSpeedLabel;

            public void CycleSpeed()
            {
                if (PlaybackController.Instance != null)
                {
                    PlaybackController.Instance.CycleSpeed();
                }
            }
        }
    }
}
