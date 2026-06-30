using System.IO;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.UI;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class ShowcaseViewTests
    {
        // ── helpers ─────────────────────────────────────────────────────────────

        private static RunArtifactDto LoadGoldenArtifact() =>
            RunArtifactLoader.LoadFromJson(File.ReadAllText(
                Path.Combine(Application.dataPath, "StreamingAssets", "run-artifact.v1.json")));

        // ── controller independence ──────────────────────────────────────────────

        [Test]
        public void Controllers_are_independent_play_state()
        {
            var artifact = LoadGoldenArtifact();
            var ctrlA = new RunArtifactPlayerController(artifact);
            var ctrlB = new RunArtifactPlayerController(artifact);

            ctrlA.Play();

            Assert.That(ctrlA.State.IsPlaying, Is.True,  "A should be playing");
            Assert.That(ctrlB.State.IsPlaying, Is.False, "B must not be affected by A.Play()");
        }

        [Test]
        public void Controllers_are_independent_tick()
        {
            var artifact = LoadGoldenArtifact();
            var ctrlA = new RunArtifactPlayerController(artifact);
            var ctrlB = new RunArtifactPlayerController(artifact);

            ctrlA.Play();
            ctrlA.Tick(80);

            Assert.That(ctrlA.State.CurrentTimeMs, Is.EqualTo(90));
            Assert.That(ctrlB.State.CurrentTimeMs, Is.EqualTo(ctrlB.State.StartTimeMs),
                "B timeline must not advance when only A ticks");
        }

        // ── seek sync ────────────────────────────────────────────────────────────

        [Test]
        public void SeekSync_A_drives_B_to_same_position()
        {
            var artifact = LoadGoldenArtifact();
            var ctrlA = new RunArtifactPlayerController(artifact);
            var ctrlB = new RunArtifactPlayerController(artifact);

            // Mirrors what ShowcaseView.OnSliderA does when not suppressed
            const long target = 100;
            ctrlA.Seek(target);
            ctrlB.Seek(target);

            Assert.That(ctrlA.State.CurrentTimeMs, Is.EqualTo(target));
            Assert.That(ctrlB.State.CurrentTimeMs, Is.EqualTo(target));
        }

        [Test]
        public void SeekSync_B_drives_A_to_same_position()
        {
            var artifact = LoadGoldenArtifact();
            var ctrlA = new RunArtifactPlayerController(artifact);
            var ctrlB = new RunArtifactPlayerController(artifact);

            const long target = 150;
            ctrlB.Seek(target);
            ctrlA.Seek(target);

            Assert.That(ctrlB.State.CurrentTimeMs, Is.EqualTo(target));
            Assert.That(ctrlA.State.CurrentTimeMs, Is.EqualTo(target));
        }

        [Test]
        public void SeekSync_does_not_cross_contaminate_play_state()
        {
            var artifact = LoadGoldenArtifact();
            var ctrlA = new RunArtifactPlayerController(artifact);
            var ctrlB = new RunArtifactPlayerController(artifact);

            ctrlA.Play();
            ctrlA.Seek(100);
            ctrlB.Seek(100); // sync

            Assert.That(ctrlA.State.IsPlaying, Is.True,  "A play state must survive a sync seek");
            Assert.That(ctrlB.State.IsPlaying, Is.False, "B play state must not be changed by sync seek");
        }

        // ── delta label format ───────────────────────────────────────────────────

        [Test]
        public void FormatDelta_positive_improvement()
        {
            Assert.That(ShowcaseView.FormatDelta(100, 120), Is.EqualTo("Delta: +20%"));
        }

        [Test]
        public void FormatDelta_negative_regression()
        {
            Assert.That(ShowcaseView.FormatDelta(100, 80), Is.EqualTo("Delta: -20%"));
        }

        [Test]
        public void FormatDelta_zero_delta()
        {
            Assert.That(ShowcaseView.FormatDelta(100, 100), Is.EqualTo("Delta: +0%"));
        }

        [Test]
        public void FormatDelta_zero_baseline_returns_dash()
        {
            Assert.That(ShowcaseView.FormatDelta(0, 500), Is.EqualTo("Delta: —"));
        }

        [Test]
        public void FormatDelta_starts_with_Delta_prefix()
        {
            var result = ShowcaseView.FormatDelta(200, 250);
            Assert.That(result, Does.StartWith("Delta:"));
        }

        [Test]
        public void FormatDelta_fractional_rounds_to_one_decimal()
        {
            // 100 → 110.5 = +10.5 %
            Assert.That(ShowcaseView.FormatDelta(200, 221), Is.EqualTo("Delta: +10.5%"));
        }
    }
}
