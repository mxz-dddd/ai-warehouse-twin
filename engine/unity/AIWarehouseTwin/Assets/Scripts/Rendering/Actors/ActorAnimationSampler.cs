using System;
using System.Linq;
using UnityEngine;

namespace AIWarehouseTwin.Rendering.Actors
{
    public static class ActorAnimationSampler
    {
        public static ActorPose Sample(
            ActorAnimationTimeline timeline,
            string actorId,
            long timeMs)
        {
            if (timeline == null ||
                !timeline.TryGetTimeline(actorId, out var actorTimeline))
            {
                return ActorPose.Unavailable(actorId);
            }

            return Sample(actorTimeline, timeMs);
        }

        public static ActorPose Sample(ActorTimeline timeline, long timeMs)
        {
            if (timeline == null || timeline.IsEmpty)
            {
                return ActorPose.Unavailable(timeline?.ActorId ?? string.Empty);
            }

            var activeSegment = timeline.Segments.FirstOrDefault(segment =>
                timeMs >= segment.StartMs && timeMs <= segment.EndMs);
            if (!string.IsNullOrEmpty(activeSegment.SegmentId))
            {
                return PoseFromSegment(timeline, activeSegment, timeMs);
            }

            if (timeline.Segments.Count > 0)
            {
                var first = timeline.Segments[0];
                if (timeMs < first.StartMs)
                {
                    var previousEvent = LatestEventAtOrBefore(timeline, timeMs);
                    if (!string.IsNullOrEmpty(previousEvent.EventId))
                    {
                        return PoseFromEvent(timeline, previousEvent, timeMs);
                    }

                    return PoseFromSegment(timeline, first, first.StartMs);
                }

                var last = timeline.Segments[timeline.Segments.Count - 1];
                if (timeMs > last.EndMs)
                {
                    return PoseFromSegment(timeline, last, last.EndMs);
                }

                var previous = timeline.Segments
                    .Where(segment => segment.EndMs <= timeMs)
                    .OrderByDescending(segment => segment.EndMs)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(previous.SegmentId))
                {
                    return PoseFromSegment(timeline, previous, previous.EndMs);
                }
            }

            if (timeline.Events.Count > 0)
            {
                var nearest = timeline.Events
                    .OrderBy(movementEvent => Math.Abs(movementEvent.AtMs - timeMs))
                    .ThenBy(movementEvent => movementEvent.AtMs)
                    .ThenBy(movementEvent => movementEvent.EventId, StringComparer.Ordinal)
                    .First();

                return PoseFromEvent(timeline, nearest, timeMs);
            }

            return ActorPose.Unavailable(timeline.ActorId);
        }

        private static ActorPose PoseFromSegment(
            ActorTimeline timeline,
            ActorTimelineSegment segment,
            long timeMs)
        {
            var position = Vector2.Lerp(
                segment.From,
                segment.To,
                Alpha(segment, timeMs));

            return new ActorPose(
                timeline.ActorId,
                position,
                isAvailable: true,
                loadState: ResolveLoadState(timeline, string.Empty, timeMs),
                evidenceId: segment.SegmentId,
                evidenceKind: "route_segment",
                sourceRunArtifact: timeline.SourceRunArtifact,
                graphSource: timeline.GraphSource,
                provenance: timeline.Provenance);
        }

        private static ActorPose PoseFromEvent(
            ActorTimeline timeline,
            ActorTimelineEvent movementEvent,
            long timeMs)
        {
            return new ActorPose(
                timeline.ActorId,
                movementEvent.Position,
                isAvailable: true,
                loadState: ResolveLoadState(timeline, movementEvent.LoadState, timeMs),
                evidenceId: movementEvent.EventId,
                evidenceKind: "movement_event",
                sourceRunArtifact: timeline.SourceRunArtifact,
                graphSource: timeline.GraphSource,
                provenance: timeline.Provenance);
        }

        private static ActorTimelineEvent LatestEventAtOrBefore(
            ActorTimeline timeline,
            long timeMs)
        {
            return timeline.Events
                .Where(movementEvent => movementEvent.AtMs <= timeMs)
                .OrderByDescending(movementEvent => movementEvent.AtMs)
                .ThenByDescending(movementEvent => movementEvent.EventId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static float Alpha(ActorTimelineSegment segment, long timeMs)
        {
            if (segment.EndMs <= segment.StartMs)
            {
                return timeMs <= segment.StartMs ? 0f : 1f;
            }

            var value = (timeMs - segment.StartMs) /
                (float)(segment.EndMs - segment.StartMs);
            return Mathf.Clamp01(value);
        }

        private static string ResolveLoadState(
            ActorTimeline timeline,
            string directLoadState,
            long timeMs)
        {
            if (!string.IsNullOrWhiteSpace(directLoadState))
            {
                return directLoadState;
            }

            var eventLoadState = timeline.Events
                .Where(movementEvent =>
                    movementEvent.AtMs <= timeMs &&
                    !string.IsNullOrWhiteSpace(movementEvent.LoadState))
                .OrderByDescending(movementEvent => movementEvent.AtMs)
                .ThenByDescending(movementEvent => movementEvent.EventId, StringComparer.Ordinal)
                .Select(movementEvent => movementEvent.LoadState)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(eventLoadState)
                ? timeline.InitialLoadState
                : eventLoadState;
        }
    }
}
