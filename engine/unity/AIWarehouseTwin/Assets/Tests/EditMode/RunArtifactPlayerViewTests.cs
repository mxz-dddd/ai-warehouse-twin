using System.IO;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Playback;
using AIWarehouseTwin.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.Tests
{
    public sealed class RunArtifactPlayerViewTests
    {
        private static RunArtifactDto LoadGoldenArtifact()
        {
            var json = File.ReadAllText(Path.Combine(
                Application.dataPath, "StreamingAssets", "run-artifact.v1.json"));
            return RunArtifactLoader.LoadFromJson(json);
        }

        [Test]
        public void RefreshUi_sets_scenario_and_seed_labels()
        {
            var controller = new RunArtifactPlayerController(LoadGoldenArtifact());
            var scenarioLabel = new Label();
            var seedLabel = new Label();
            var timeLabel = new Label();
            var playPauseButton = new Button();
            var slider = new SliderInt();
            var kpiList = new ScrollView();
            var eventList = new ScrollView();

            RunArtifactPlayerView.RefreshUi(
                controller.State, scenarioLabel, seedLabel, timeLabel,
                playPauseButton, slider, kpiList, eventList);

            Assert.That(scenarioLabel.text, Is.EqualTo("sample-small-warehouse"));
            Assert.That(seedLabel.text, Is.EqualTo("Seed 20240627"));
        }

        [Test]
        public void RefreshUi_play_pause_button_text_reflects_state()
        {
            var controller = new RunArtifactPlayerController(LoadGoldenArtifact());
            var btn = new Button();
            var dummy = new Label();
            var slider = new SliderInt();
            var kpi = new ScrollView();
            var evt = new ScrollView();

            RunArtifactPlayerView.RefreshUi(
                controller.State, dummy, dummy, dummy, btn, slider, kpi, evt);
            Assert.That(btn.text, Is.EqualTo("Play"));

            controller.Play();
            RunArtifactPlayerView.RefreshUi(
                controller.State, dummy, dummy, dummy, btn, slider, kpi, evt);
            Assert.That(btn.text, Is.EqualTo("Pause"));
        }

        [Test]
        public void RefreshUi_kpi_list_rows_match_state()
        {
            var controller = new RunArtifactPlayerController(LoadGoldenArtifact());
            var kpiList = new ScrollView();
            var dummy = new Label();
            var btn = new Button();
            var slider = new SliderInt();
            var evtList = new ScrollView();

            RunArtifactPlayerView.RefreshUi(
                controller.State, dummy, dummy, dummy, btn, slider, kpiList, evtList);

            Assert.That(kpiList.childCount, Is.EqualTo(controller.State.KpiRows.Length));
        }

        [Test]
        public void RefreshUi_event_list_grows_after_tick()
        {
            var controller = new RunArtifactPlayerController(LoadGoldenArtifact());
            var evtList = new ScrollView();
            var dummy = new Label();
            var btn = new Button();
            var slider = new SliderInt();
            var kpi = new ScrollView();

            RunArtifactPlayerView.RefreshUi(
                controller.State, dummy, dummy, dummy, btn, slider, kpi, evtList);
            var before = evtList.childCount;

            controller.Play();
            controller.Tick(80);
            RunArtifactPlayerView.RefreshUi(
                controller.State, dummy, dummy, dummy, btn, slider, kpi, evtList);

            Assert.That(evtList.childCount, Is.GreaterThan(before));
        }
    }
}
