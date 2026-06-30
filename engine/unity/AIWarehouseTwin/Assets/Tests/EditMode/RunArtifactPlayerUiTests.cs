using System.IO;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Playback;
using AIWarehouseTwin.UI;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class RunArtifactPlayerUiTests
    {
        [Test]
        public void KpiSummaryFormatter_maps_golden_artifact_to_display_rows()
        {
            var rows = KpiSummaryFormatter.Format(LoadGoldenArtifact());

            Assert.That(rows, Has.Length.EqualTo(10));
            Assert.That(rows[0], Is.EqualTo("Scenario: sample-small-warehouse"));
            Assert.That(rows[1], Is.EqualTo("Seed: 20240627"));
            Assert.That(rows[2], Is.EqualTo("Simulation time: 10-220 ms"));
            Assert.That(rows[4], Is.EqualTo("Completed work items: 3"));
            Assert.That(rows[9], Is.EqualTo("Total throughput/hour: 51428.571"));
        }

        [Test]
        public void EventListFormatter_maps_visible_events_to_display_rows()
        {
            var artifact = LoadGoldenArtifact();
            var timeline = RunArtifactTimeline.FromArtifact(artifact);
            var visibleEvents = timeline.GetEventsAtOrBefore(90);

            var rows = EventListFormatter.Format(visibleEvents);

            Assert.That(rows, Has.Length.EqualTo(5));
            Assert.That(rows[0], Is.EqualTo("10 ms | inbound | InboundReceiptArrived | inbound.receipt_arrived.receipt-1"));
            Assert.That(rows[4], Is.EqualTo("90 ms | each_pick | EachPickCompleted | each_pick.completed.each-order-1"));
        }

        [Test]
        public void RunArtifactPlayerController_maps_playback_to_ui_state()
        {
            var controller = new RunArtifactPlayerController(LoadGoldenArtifact());

            var initial = controller.State;
            Assert.That(initial.ScenarioId, Is.EqualTo("sample-small-warehouse"));
            Assert.That(initial.Seed, Is.EqualTo(20240627));
            Assert.That(initial.CurrentTimeMs, Is.EqualTo(10));
            Assert.That(initial.IsPlaying, Is.False);
            Assert.That(initial.EventRows, Has.Length.EqualTo(1));

            controller.Play();
            controller.Tick(80);

            var playing = controller.State;
            Assert.That(playing.CurrentTimeMs, Is.EqualTo(90));
            Assert.That(playing.IsPlaying, Is.True);
            Assert.That(playing.EventRows, Has.Length.EqualTo(5));

            controller.Pause();
            Assert.That(controller.State.IsPlaying, Is.False);

            controller.Seek(999);
            Assert.That(controller.State.CurrentTimeMs, Is.EqualTo(220));

            controller.Reset();
            Assert.That(controller.State.CurrentTimeMs, Is.EqualTo(10));
            Assert.That(controller.State.IsPlaying, Is.False);
        }

        [Test]
        public void StreamingAssetsArtifactSource_points_at_default_golden_artifact_name()
        {
            var source = new StreamingAssetsArtifactSource();

            Assert.That(source.Path, Does.EndWith(Path.Combine("StreamingAssets", "run-artifact.v1.json")));
        }

        private static RunArtifactDto LoadGoldenArtifact()
        {
            var json = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "StreamingAssets",
                "run-artifact.v1.json"));
            return RunArtifactLoader.LoadFromJson(json);
        }
    }
}
