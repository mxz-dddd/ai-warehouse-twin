using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public sealed class RunArtifactPlayerView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private RunArtifactPlayerController controller;
        private Label scenarioLabel;
        private Label seedLabel;
        private Label timeLabel;
        private Button playPauseButton;
        private Button resetButton;
        private SliderInt timelineSlider;
        private ScrollView kpiList;
        private ScrollView eventList;
        private bool suppressSliderCallback;

        private void Awake()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            BindElements(_document.rootVisualElement);
            controller = new RunArtifactPlayerController(new StreamingAssetsArtifactSource().Load());

            var state = controller.State;
            timelineSlider.lowValue = (int)state.StartTimeMs;
            timelineSlider.highValue = (int)state.EndTimeMs;
            timelineSlider.RegisterValueChangedCallback(OnTimelineChanged);
            playPauseButton.clicked += TogglePlayback;
            resetButton.clicked += ResetPlayback;

            Refresh(state);
        }

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            controller.Tick((long)(Time.deltaTime * 1000f));
            Refresh(controller.State);
        }

        private void BindElements(VisualElement root)
        {
            scenarioLabel = root.Q<Label>("scenario-label");
            seedLabel = root.Q<Label>("seed-label");
            timeLabel = root.Q<Label>("time-label");
            playPauseButton = root.Q<Button>("play-pause-button");
            resetButton = root.Q<Button>("reset-button");
            timelineSlider = root.Q<SliderInt>("timeline-slider");
            kpiList = root.Q<ScrollView>("kpi-list");
            eventList = root.Q<ScrollView>("event-list");
        }

        private void TogglePlayback()
        {
            if (controller.State.IsPlaying)
            {
                controller.Pause();
            }
            else
            {
                controller.Play();
            }

            Refresh(controller.State);
        }

        private void ResetPlayback()
        {
            controller.Reset();
            Refresh(controller.State);
        }

        private void OnTimelineChanged(ChangeEvent<int> evt)
        {
            if (suppressSliderCallback)
            {
                return;
            }

            controller.Seek(evt.newValue);
            Refresh(controller.State);
        }

        private void Refresh(RunArtifactPlayerState state)
        {
            suppressSliderCallback = true;
            RefreshUi(state, scenarioLabel, seedLabel, timeLabel, playPauseButton, timelineSlider, kpiList, eventList);
            suppressSliderCallback = false;
        }

        public static void RefreshUi(
            RunArtifactPlayerState state,
            Label scenarioLabel,
            Label seedLabel,
            Label timeLabel,
            Button playPauseButton,
            SliderInt timelineSlider,
            ScrollView kpiList,
            ScrollView eventList)
        {
            scenarioLabel.text = state.ScenarioId;
            seedLabel.text = $"Seed {state.Seed}";
            timeLabel.text = $"{state.CurrentTimeMs} ms";
            playPauseButton.text = state.IsPlaying ? "Pause" : "Play";
            timelineSlider.value = (int)state.CurrentTimeMs;
            ReplaceRows(kpiList, state.KpiRows);
            ReplaceRows(eventList, state.EventRows);
        }

        private static void ReplaceRows(ScrollView list, string[] rows)
        {
            list.Clear();

            foreach (var row in rows)
            {
                list.Add(new Label(row));
            }
        }
    }
}
