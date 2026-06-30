using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Playback;

namespace AIWarehouseTwin.UI
{
    public sealed class RunArtifactPlayerViewModel
    {
        private readonly RunArtifactDto artifact;
        private readonly RunArtifactTimeline timeline;

        public RunArtifactPlayerViewModel(RunArtifactDto artifact, RunArtifactTimeline timeline)
        {
            this.artifact = artifact;
            this.timeline = timeline;
        }

        public RunArtifactPlayerState Build(TimelinePlayback playback)
        {
            return new RunArtifactPlayerState
            {
                ScenarioId = artifact.scenario_id,
                Seed = artifact.seed,
                CurrentTimeMs = playback.CurrentTimeMs,
                StartTimeMs = timeline.StartMs,
                EndTimeMs = timeline.EndMs,
                IsPlaying = playback.IsPlaying,
                KpiRows = KpiSummaryFormatter.Format(artifact),
                EventRows = EventListFormatter.Format(playback.VisibleEvents)
            };
        }
    }
}
