using System;
using System.Collections.Generic;
using AIWarehouseTwin.Artifact;

namespace AIWarehouseTwin.Playback
{
    public sealed class RunArtifactTimeline
    {
        private readonly TimelineEvent[] events;

        private RunArtifactTimeline(TimelineEvent[] events, long startMs, long endMs)
        {
            this.events = events;
            StartMs = startMs;
            EndMs = endMs;
        }

        public IReadOnlyList<TimelineEvent> Events => events;

        public long StartMs { get; }

        public long EndMs { get; }

        public static RunArtifactTimeline FromArtifact(RunArtifactDto artifact)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            var parsedEvents = new List<TimelineEvent>(artifact.event_log.Length);
            foreach (var rawLine in artifact.event_log)
            {
                parsedEvents.Add(TimelineEvent.Parse(rawLine));
            }

            parsedEvents.Sort(CompareTimelineEvents);
            return new RunArtifactTimeline(
                parsedEvents.ToArray(),
                artifact.started_at_ms,
                artifact.finished_at_ms);
        }

        public IReadOnlyList<TimelineEvent> GetEventsAtOrBefore(long atMs)
        {
            var clampedTime = Clamp(atMs);
            var visible = new List<TimelineEvent>();
            foreach (var timelineEvent in events)
            {
                if (timelineEvent.AtMs <= clampedTime)
                {
                    visible.Add(timelineEvent);
                }
            }

            return visible;
        }

        public long Clamp(long atMs)
        {
            if (atMs < StartMs)
            {
                return StartMs;
            }

            return atMs > EndMs ? EndMs : atMs;
        }

        private static int CompareTimelineEvents(TimelineEvent left, TimelineEvent right)
        {
            var timeComparison = left.AtMs.CompareTo(right.AtMs);
            if (timeComparison != 0)
            {
                return timeComparison;
            }

            var flowComparison = string.CompareOrdinal(left.Flow, right.Flow);
            return flowComparison != 0 ? flowComparison : left.Sequence.CompareTo(right.Sequence);
        }
    }
}
