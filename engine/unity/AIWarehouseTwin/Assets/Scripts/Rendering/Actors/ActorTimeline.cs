using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIWarehouseTwin.Rendering.Actors
{
    public sealed class ActorTimeline
    {
        public ActorTimeline(
            string actorId,
            IReadOnlyList<ActorTimelineSegment> segments,
            IReadOnlyList<ActorTimelineEvent> events,
            string initialLoadState,
            string sourceRunArtifact,
            string graphSource,
            string provenance)
        {
            ActorId = actorId ?? string.Empty;
            Segments = segments ?? Array.Empty<ActorTimelineSegment>();
            Events = events ?? Array.Empty<ActorTimelineEvent>();
            InitialLoadState = initialLoadState ?? string.Empty;
            SourceRunArtifact = sourceRunArtifact ?? string.Empty;
            GraphSource = graphSource ?? string.Empty;
            Provenance = provenance ?? string.Empty;
        }

        public string ActorId { get; }

        public IReadOnlyList<ActorTimelineSegment> Segments { get; }

        public IReadOnlyList<ActorTimelineEvent> Events { get; }

        public string InitialLoadState { get; }

        public string SourceRunArtifact { get; }

        public string GraphSource { get; }

        public string Provenance { get; }

        public bool IsEmpty => Segments.Count == 0 && Events.Count == 0;
    }

    public sealed class ActorAnimationTimeline
    {
        private readonly Dictionary<string, ActorTimeline> timelines;

        public ActorAnimationTimeline(IEnumerable<ActorTimeline> actorTimelines)
        {
            timelines = (actorTimelines ?? Array.Empty<ActorTimeline>())
                .Where(timeline => !string.IsNullOrWhiteSpace(timeline.ActorId))
                .GroupBy(timeline => timeline.ActorId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.Ordinal);
        }

        public IReadOnlyCollection<string> ActorIds => timelines.Keys;

        public bool IsEmpty => timelines.Count == 0;

        public bool TryGetTimeline(string actorId, out ActorTimeline timeline)
        {
            return timelines.TryGetValue(actorId ?? string.Empty, out timeline);
        }
    }

    public readonly struct ActorTimelineSegment
    {
        public ActorTimelineSegment(
            string segmentId,
            string operationId,
            long startMs,
            long endMs,
            Vector2 from,
            Vector2 to,
            string fromNodeId,
            string toNodeId)
        {
            SegmentId = segmentId ?? string.Empty;
            OperationId = operationId ?? string.Empty;
            StartMs = startMs;
            EndMs = endMs;
            From = from;
            To = to;
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;
        }

        public string SegmentId { get; }

        public string OperationId { get; }

        public long StartMs { get; }

        public long EndMs { get; }

        public Vector2 From { get; }

        public Vector2 To { get; }

        public string FromNodeId { get; }

        public string ToNodeId { get; }
    }

    public readonly struct ActorTimelineEvent
    {
        public ActorTimelineEvent(
            string eventId,
            long atMs,
            Vector2 position,
            string loadState,
            string eventType)
        {
            EventId = eventId ?? string.Empty;
            AtMs = atMs;
            Position = position;
            LoadState = loadState ?? string.Empty;
            EventType = eventType ?? string.Empty;
        }

        public string EventId { get; }

        public long AtMs { get; }

        public Vector2 Position { get; }

        public string LoadState { get; }

        public string EventType { get; }
    }
}
