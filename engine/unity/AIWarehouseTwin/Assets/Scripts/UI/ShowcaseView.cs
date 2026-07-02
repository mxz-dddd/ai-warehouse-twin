using System;
using System.Globalization;
using System.IO;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.UI.Showcase;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public sealed class ShowcaseView : MonoBehaviour
    {
        private const string ComparisonSourceLabel = "A5b deterministic comparison artifact";

        // H2: swap these to drive each panel from a different artifact source.
        // H1: unused — both panels load from StreamingAssetsArtifactSource.
        [SerializeField] private UIDocument _documentA;
        [SerializeField] private UIDocument _documentB;

        private RunArtifactPlayerController _controllerA;
        private RunArtifactPlayerController _controllerB;
        private RunArtifactDto _artifactA;
        private RunArtifactDto _artifactB;
        private AbShowcaseViewModel _comparisonModel;

        private Label _deltaLabel;
        private ScrollView _abComparePanel;

        private Label _scenarioA;
        private Label _seedA;
        private Label _timeA;
        private Button _playPauseA;
        private Button _resetA;
        private SliderInt _sliderA;
        private ScrollView _kpiHudA;
        private ScrollView _evtA;

        private Label _scenarioB;
        private Label _seedB;
        private Label _timeB;
        private Button _playPauseB;
        private Button _resetB;
        private SliderInt _sliderB;
        private ScrollView _kpiHudB;
        private ScrollView _evtB;

        private bool _suppressSliderA;
        private bool _suppressSliderB;
        private bool _suppressSync;

        private void Awake()
        {
            if (GetComponent<UIDocument>() == null)
            {
                Debug.LogWarning("[ShowcaseView] No UIDocument on this GameObject; ShowcaseView.uxml must be assigned.");
            }
        }

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            BindElements(root);

            var source = new StreamingAssetsArtifactSource();
            _artifactA = source.Load();
            _artifactB = source.Load();

            _controllerA = new RunArtifactPlayerController(_artifactA);
            _controllerB = new RunArtifactPlayerController(_artifactB);
            _comparisonModel = LoadComparisonModel();

            InitSlider(_controllerA, _sliderA);
            InitSlider(_controllerB, _sliderB);

            _sliderA.RegisterValueChangedCallback(OnSliderA);
            _sliderB.RegisterValueChangedCallback(OnSliderB);
            _playPauseA.clicked += ToggleA;
            _playPauseB.clicked += ToggleB;
            _resetA.clicked += ResetA;
            _resetB.clicked += ResetB;

            RefreshAll();
            ABComparePanel.RefreshUi(_comparisonModel, _abComparePanel);
        }

        private void Update()
        {
            if (_controllerA == null)
            {
                return;
            }

            var dt = (long)(Time.deltaTime * 1000f);
            _controllerA.Tick(dt);
            _controllerB.Tick(dt);
            RefreshAll();
        }

        private void BindElements(VisualElement root)
        {
            _deltaLabel = root.Q<Label>("delta-label");
            _abComparePanel = root.Q<ScrollView>("ab-compare-panel");

            _scenarioA  = root.Q<Label>("scenario-label-a");
            _seedA      = root.Q<Label>("seed-label-a");
            _timeA      = root.Q<Label>("time-label-a");
            _playPauseA = root.Q<Button>("play-pause-button-a");
            _resetA     = root.Q<Button>("reset-button-a");
            _sliderA    = root.Q<SliderInt>("timeline-slider-a");
            _kpiHudA    = root.Q<ScrollView>("kpi-hud-a");
            _evtA       = root.Q<ScrollView>("event-list-a");

            _scenarioB  = root.Q<Label>("scenario-label-b");
            _seedB      = root.Q<Label>("seed-label-b");
            _timeB      = root.Q<Label>("time-label-b");
            _playPauseB = root.Q<Button>("play-pause-button-b");
            _resetB     = root.Q<Button>("reset-button-b");
            _sliderB    = root.Q<SliderInt>("timeline-slider-b");
            _kpiHudB    = root.Q<ScrollView>("kpi-hud-b");
            _evtB       = root.Q<ScrollView>("event-list-b");
        }

        private static void InitSlider(RunArtifactPlayerController ctrl, SliderInt slider)
        {
            slider.lowValue  = (int)ctrl.State.StartTimeMs;
            slider.highValue = (int)ctrl.State.EndTimeMs;
        }

        private void ToggleA()
        {
            if (_controllerA.State.IsPlaying) _controllerA.Pause();
            else _controllerA.Play();
            RefreshAll();
        }

        private void ToggleB()
        {
            if (_controllerB.State.IsPlaying) _controllerB.Pause();
            else _controllerB.Play();
            RefreshAll();
        }

        private void ResetA() { _controllerA.Reset(); RefreshAll(); }
        private void ResetB() { _controllerB.Reset(); RefreshAll(); }

        private void OnSliderA(ChangeEvent<int> e)
        {
            if (_suppressSliderA) return;
            _controllerA.Seek(e.newValue);
            if (!_suppressSync)
            {
                _suppressSync = true;
                _controllerB.Seek(e.newValue);
                _suppressSync = false;
            }
            RefreshAll();
        }

        private void OnSliderB(ChangeEvent<int> e)
        {
            if (_suppressSliderB) return;
            _controllerB.Seek(e.newValue);
            if (!_suppressSync)
            {
                _suppressSync = true;
                _controllerA.Seek(e.newValue);
                _suppressSync = false;
            }
            RefreshAll();
        }

        private void RefreshAll()
        {
            var stateA = _controllerA.State;
            var stateB = _controllerB.State;

            _suppressSliderA = true;
            RefreshPlaybackUi(stateA, _scenarioA, _seedA, _timeA, _playPauseA, _sliderA, _evtA);
            KpiHudPanel.RefreshUi(stateA, _kpiHudA);
            _suppressSliderA = false;

            _suppressSliderB = true;
            RefreshPlaybackUi(stateB, _scenarioB, _seedB, _timeB, _playPauseB, _sliderB, _evtB);
            KpiHudPanel.RefreshUi(stateB, _kpiHudB);
            _suppressSliderB = false;

            _deltaLabel.text = FormatDelta(
                _artifactA.kpi_summary.total_work_item_throughput_per_hour,
                _artifactB.kpi_summary.total_work_item_throughput_per_hour);
        }

        private static void RefreshPlaybackUi(
            RunArtifactPlayerState state,
            Label scenarioLabel,
            Label seedLabel,
            Label timeLabel,
            Button playPauseButton,
            SliderInt timelineSlider,
            ScrollView eventList)
        {
            scenarioLabel.text = state.ScenarioId;
            seedLabel.text = $"Seed {state.Seed}";
            timeLabel.text = $"{state.CurrentTimeMs} ms";
            playPauseButton.text = state.IsPlaying ? "Pause" : "Play";
            timelineSlider.value = (int)state.CurrentTimeMs;
            ReplaceRows(eventList, state.EventRows);
        }

        private static void ReplaceRows(ScrollView list, string[] rows)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();
            foreach (var row in rows ?? Array.Empty<string>())
            {
                list.Add(new Label(row));
            }
        }

        private static AbShowcaseViewModel LoadComparisonModel()
        {
            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "..",
                "..",
                "datasets",
                "medium-warehouse",
                "optimized",
                "artifacts",
                "comparison-artifact.v1.json"));

            try
            {
                return AbShowcasePresenter.FromComparisonArtifact(
                    ComparisonArtifactLoader.LoadFromFile(path),
                    ComparisonSourceLabel);
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is FileNotFoundException ||
                ex is InvalidOperationException)
            {
                return UnavailableComparison($"ComparisonArtifact unavailable: {ex.Message}");
            }
        }

        private static AbShowcaseViewModel UnavailableComparison(string reason)
        {
            return new AbShowcaseViewModel(
                false,
                reason,
                true,
                ComparisonSourceLabel,
                AbShowcasePresenter.MockEvidenceLabel,
                new AbShowcaseScenarioSummary(AbShowcasePresenter.BaselineDisplayLabel, "baseline", string.Empty),
                new AbShowcaseScenarioSummary(AbShowcasePresenter.CandidateDisplayLabel, "candidate", string.Empty),
                Array.Empty<AbShowcaseKpiRow>(),
                0);
        }

        /// <summary>
        /// Formats a KPI throughput delta as a signed-percentage string.
        /// Returns "Delta: —" when baseline is zero.
        /// Public for EditMode tests.
        /// </summary>
        public static string FormatDelta(double throughputA, double throughputB)
        {
            if (throughputA == 0)
            {
                return "Delta: —";
            }

            var pct = (throughputB - throughputA) * 100.0 / throughputA;
            var sign = pct >= 0 ? "+" : string.Empty;
            return $"Delta: {sign}{pct.ToString("0.#", CultureInfo.InvariantCulture)}%";
        }
    }
}
