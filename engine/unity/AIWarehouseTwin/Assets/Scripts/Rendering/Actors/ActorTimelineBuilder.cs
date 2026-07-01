using System;
using System.Collections.Generic;
using System.Linq;
using AIWarehouseTwin.Artifact;
using UnityEngine;

namespace AIWarehouseTwin.Rendering.Actors
{
    public static class ActorTimelineBuilder
    {
        public static ActorAnimationTimeline Empty()
        {
            return new ActorAnimationTimeline(Array.Empty<ActorTimeline>());
        }

        public static ActorAnimationTimeline FromLoadResult(MovementArtifactLoadResult loadResult)
        {
            if (loadResult == null || !loadResult.IsAvailable)
            {
                return Empty();
            }

            return FromArtifact(loadResult.Artifact);
        }

        public static ActorAnimationTimeline FromArtifact(MovementArtifactDto artifact)
        {
            if (artifact == null)
            {
                return Empty();
            }

            var nodes = BuildNodeLookup(artifact.warehouse_graph);
            var actors = BuildActorLookup(artifact.actors);
            var segmentsByActor = BuildSegmentsByActor(artifact.route_segments, nodes);
            var eventsByActor = BuildEventsByActor(artifact.movement_events);
            var actorIds = actors.Keys
                .Concat(segmentsByActor.Keys)
                .Concat(eventsByActor.Keys)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            var timelines = actorIds.Select(actorId =>
            {
                actors.TryGetValue(actorId, out var actor);
                segmentsByActor.TryGetValue(actorId, out var segments);
                eventsByActor.TryGetValue(actorId, out var events);

                return new ActorTimeline(
                    actorId,
                    segments ?? Array.Empty<ActorTimelineSegment>(),
                    events ?? Array.Empty<ActorTimelineEvent>(),
                    actor?.load_state ?? string.Empty,
                    artifact.source_run_artifact,
                    artifact.provenance?.graph_source ?? string.Empty,
                    artifact.provenance?.deterministic_generation_policy ?? string.Empty);
            });

            return new ActorAnimationTimeline(timelines);
        }

        private static Dictionary<string, Vector2> BuildNodeLookup(WarehouseGraphDto graph)
        {
            return (graph?.nodes ?? Array.Empty<WarehouseGraphNodeDto>())
                .Where(node => !string.IsNullOrWhiteSpace(node.node_id))
                .GroupBy(node => node.node_id, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var node = group.First();
                        return new Vector2((float)node.x, (float)node.y);
                    },
                    StringComparer.Ordinal);
        }

        private static Dictionary<string, MovementActorDto> BuildActorLookup(
            MovementActorDto[] actors)
        {
            return (actors ?? Array.Empty<MovementActorDto>())
                .Where(actor => !string.IsNullOrWhiteSpace(actor.actor_id))
                .GroupBy(actor => actor.actor_id, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.Ordinal);
        }

        private static Dictionary<string, IReadOnlyList<ActorTimelineSegment>> BuildSegmentsByActor(
            MovementRouteSegmentDto[] routeSegments,
            IReadOnlyDictionary<string, Vector2> nodes)
        {
            return (routeSegments ?? Array.Empty<MovementRouteSegmentDto>())
                .Where(segment => !string.IsNullOrWhiteSpace(segment.actor_id))
                .Select(segment => TryBuildSegment(segment, nodes, out var timelineSegment)
                    ? (isValid: true, segment.actor_id, timelineSegment)
                    : (isValid: false, segment.actor_id, timelineSegment))
                .Where(item => item.isValid)
                .GroupBy(item => item.actor_id, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ActorTimelineSegment>)group
                        .Select(item => item.timelineSegment)
                        .OrderBy(segment => segment.StartMs)
                        .ThenBy(segment => segment.EndMs)
                        .ThenBy(segment => segment.SegmentId, StringComparer.Ordinal)
                        .ToArray(),
                    StringComparer.Ordinal);
        }

        private static bool TryBuildSegment(
            MovementRouteSegmentDto segment,
            IReadOnlyDictionary<string, Vector2> nodes,
            out ActorTimelineSegment timelineSegment)
        {
            timelineSegment = default;

            if (!nodes.TryGetValue(segment.from_node_id ?? string.Empty, out var from) ||
                !nodes.TryGetValue(segment.to_node_id ?? string.Empty, out var to))
            {
                return false;
            }

            timelineSegment = new ActorTimelineSegment(
                segment.segment_id,
                segment.operation_id,
                segment.start_ms,
                segment.end_ms,
                from,
                to,
                segment.from_node_id,
                segment.to_node_id);
            return true;
        }

        private static Dictionary<string, IReadOnlyList<ActorTimelineEvent>> BuildEventsByActor(
            MovementEventDto[] movementEvents)
        {
            return (movementEvents ?? Array.Empty<MovementEventDto>())
                .Where(movementEvent => !string.IsNullOrWhiteSpace(movementEvent.actor_id))
                .GroupBy(movementEvent => movementEvent.actor_id, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ActorTimelineEvent>)group
                        .Select(movementEvent => new ActorTimelineEvent(
                            movementEvent.event_id,
                            movementEvent.at_ms,
                            new Vector2((float)movementEvent.x, (float)movementEvent.y),
                            movementEvent.load_state,
                            movementEvent.event_type))
                        .OrderBy(movementEvent => movementEvent.AtMs)
                        .ThenBy(movementEvent => movementEvent.EventId, StringComparer.Ordinal)
                        .ToArray(),
                    StringComparer.Ordinal);
        }
    }
}
