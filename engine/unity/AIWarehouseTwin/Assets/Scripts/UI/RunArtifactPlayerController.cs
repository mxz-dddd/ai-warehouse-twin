using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Playback;

namespace AIWarehouseTwin.UI
{
    public sealed class RunArtifactPlayerController
    {
        private readonly RunArtifactPlayerViewModel viewModel;
        private readonly TimelinePlayback playback;

        public RunArtifactPlayerController(RunArtifactDto artifact)
        {
            var timeline = RunArtifactTimeline.FromArtifact(artifact);
            playback = new TimelinePlayback(timeline);
            viewModel = new RunArtifactPlayerViewModel(artifact, timeline);
        }

        public RunArtifactPlayerState State => viewModel.Build(playback);

        public void Play()
        {
            playback.Play();
        }

        public void Pause()
        {
            playback.Pause();
        }

        public void Seek(long atMs)
        {
            playback.Seek(atMs);
        }

        public void Reset()
        {
            playback.Pause();
            playback.Seek(State.StartTimeMs);
        }

        public void Tick(long deltaMs)
        {
            playback.Tick(deltaMs);
        }
    }
}
