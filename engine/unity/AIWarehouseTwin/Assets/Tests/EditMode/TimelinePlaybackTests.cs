using System.IO;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Playback;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class TimelinePlaybackTests
    {
        [Test]
        public void Timeline_sorts_events_by_time_and_preserves_golden_bounds()
        {
            var timeline = LoadGoldenTimeline();

            Assert.That(timeline.StartMs, Is.EqualTo(10));
            Assert.That(timeline.EndMs, Is.EqualTo(220));
            Assert.That(timeline.Events.Count, Is.EqualTo(10));
            Assert.That(timeline.Events[0].AtMs, Is.EqualTo(10));
            Assert.That(timeline.Events[0].Flow, Is.EqualTo("inbound"));
            Assert.That(timeline.Events[9].AtMs, Is.EqualTo(220));
        }

        [Test]
        public void Playback_seek_clamps_and_exposes_visible_events()
        {
            var playback = new TimelinePlayback(LoadGoldenTimeline());

            playback.Seek(90);

            Assert.That(playback.CurrentTimeMs, Is.EqualTo(90));
            Assert.That(playback.VisibleEvents, Has.Count.EqualTo(5));

            playback.Seek(-1);
            Assert.That(playback.CurrentTimeMs, Is.EqualTo(10));

            playback.Seek(999);
            Assert.That(playback.CurrentTimeMs, Is.EqualTo(220));
        }

        [Test]
        public void Playback_tick_advances_only_while_playing_and_stops_at_end()
        {
            var playback = new TimelinePlayback(LoadGoldenTimeline());

            playback.Tick(100);
            Assert.That(playback.CurrentTimeMs, Is.EqualTo(10));

            playback.Play();
            playback.Tick(100);
            Assert.That(playback.CurrentTimeMs, Is.EqualTo(110));
            Assert.That(playback.IsPlaying, Is.True);

            playback.Tick(200);
            Assert.That(playback.CurrentTimeMs, Is.EqualTo(220));
            Assert.That(playback.IsPlaying, Is.False);
        }

        private static RunArtifactTimeline LoadGoldenTimeline()
        {
            var json = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "StreamingAssets",
                "run-artifact.v1.json"));
            return RunArtifactTimeline.FromArtifact(RunArtifactLoader.LoadFromJson(json));
        }
    }
}
