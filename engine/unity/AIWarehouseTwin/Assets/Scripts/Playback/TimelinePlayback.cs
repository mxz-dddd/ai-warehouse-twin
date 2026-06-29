using System;
using System.Collections.Generic;

namespace AIWarehouseTwin.Playback
{
    public sealed class TimelinePlayback
    {
        private readonly RunArtifactTimeline timeline;

        public TimelinePlayback(RunArtifactTimeline timeline)
        {
            this.timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            CurrentTimeMs = timeline.StartMs;
        }

        public long CurrentTimeMs { get; private set; }

        public bool IsPlaying { get; private set; }

        public IReadOnlyList<TimelineEvent> VisibleEvents => timeline.GetEventsAtOrBefore(CurrentTimeMs);

        public void Play()
        {
            IsPlaying = true;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Seek(long atMs)
        {
            CurrentTimeMs = timeline.Clamp(atMs);
        }

        public void Tick(long deltaMs)
        {
            if (!IsPlaying || deltaMs <= 0)
            {
                return;
            }

            CurrentTimeMs = timeline.Clamp(CurrentTimeMs + deltaMs);
            if (CurrentTimeMs >= timeline.EndMs)
            {
                IsPlaying = false;
            }
        }
    }
}
