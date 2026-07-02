using System;
using System.Collections.Generic;
using System.Linq;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Playback;
using AIWarehouseTwin.Rendering.Actors;
using UnityEngine;

namespace AIWarehouseTwin.Agent
{
    public sealed class ActorDirector : MonoBehaviour
    {
        private const float MillisecondsToSeconds = 0.001f;
        private readonly List<ActorController> actors = new List<ActorController>();

        [SerializeField]
        private AgentSpawner spawner;

        [SerializeField]
        private bool tickFromPlayback;

        public IReadOnlyList<ActorController> Actors => actors;

        public bool TickFromPlayback
        {
            get => tickFromPlayback;
            set => tickFromPlayback = value;
        }

        public AgentSpawner Spawner => ResolveSpawner();

        public void LoadFromArtifact(MovementArtifactDto artifact)
        {
            Clear();

            if (artifact == null)
            {
                return;
            }

            var timeline = ActorTimelineBuilder.FromArtifact(artifact);
            var actorTypes = BuildActorTypes(artifact);
            var actorIds = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var actor in artifact.actors ?? Array.Empty<MovementActorDto>())
            {
                AddIfPresent(actorIds, actor.actor_id);
            }

            foreach (var actorId in timeline.ActorIds)
            {
                AddIfPresent(actorIds, actorId);
            }

            foreach (var actorId in actorIds)
            {
                actorTypes.TryGetValue(actorId, out var actorType);
                var controller = ResolveSpawner().SpawnActor(actorId, actorType);
                if (controller == null)
                {
                    continue;
                }

                controller.LoadRoute(timeline.TryGetTimeline(actorId, out var actorTimeline)
                    ? BuildRoute(actorTimeline)
                    : Array.Empty<ActorRouteSegment>());
                actors.Add(controller);
            }
        }

        public void LoadFromArtifact(RunArtifactDto artifact)
        {
            Clear();

            if (artifact != null)
            {
                Debug.LogWarning(
                    "RunArtifactDto does not contain actor route data; load a MovementArtifactDto to spawn actors.");
            }
        }

        public void Tick(float simulationTime)
        {
            foreach (var actor in actors)
            {
                if (actor != null)
                {
                    actor.Tick(simulationTime);
                }
            }
        }

        public void Clear()
        {
            actors.Clear();

            if (spawner != null)
            {
                spawner.ClearActors();
            }
        }

        private void Update()
        {
            if (!tickFromPlayback)
            {
                return;
            }

            var playback = PlaybackController.Instance;
            if (playback != null)
            {
                Tick(playback.simulationTime);
            }
        }

        private AgentSpawner ResolveSpawner()
        {
            if (spawner != null)
            {
                return spawner;
            }

            spawner = GetComponent<AgentSpawner>();
            if (spawner == null)
            {
                spawner = gameObject.AddComponent<AgentSpawner>();
            }

            return spawner;
        }

        private static IReadOnlyList<ActorRouteSegment> BuildRoute(ActorTimeline timeline)
        {
            if (timeline == null)
            {
                return Array.Empty<ActorRouteSegment>();
            }

            if (timeline.Segments.Count > 0)
            {
                return timeline.Segments
                    .Select(segment => new ActorRouteSegment(
                        Seconds(segment.StartMs),
                        Seconds(segment.EndMs),
                        ToVector3(segment.From),
                        ToVector3(segment.To),
                        ResolveState(timeline, segment)))
                    .ToArray();
            }

            return BuildEventFallbackRoute(timeline);
        }

        private static IReadOnlyList<ActorRouteSegment> BuildEventFallbackRoute(ActorTimeline timeline)
        {
            if (timeline.Events.Count == 0)
            {
                return Array.Empty<ActorRouteSegment>();
            }

            if (timeline.Events.Count == 1)
            {
                var movementEvent = timeline.Events[0];
                var time = Seconds(movementEvent.AtMs);
                var position = ToVector3(movementEvent.Position);
                return new[]
                {
                    new ActorRouteSegment(
                        time,
                        time + 0.001f,
                        position,
                        position,
                        MapLoadState(movementEvent.LoadState, ActorState.Idle)),
                };
            }

            var segments = new List<ActorRouteSegment>();
            for (var i = 0; i < timeline.Events.Count - 1; i++)
            {
                var from = timeline.Events[i];
                var to = timeline.Events[i + 1];
                segments.Add(new ActorRouteSegment(
                    Seconds(from.AtMs),
                    Seconds(to.AtMs),
                    ToVector3(from.Position),
                    ToVector3(to.Position),
                    MapLoadState(from.LoadState, ActorState.Moving)));
            }

            return segments;
        }

        private static ActorState ResolveState(ActorTimeline timeline, ActorTimelineSegment segment)
        {
            var loadState = timeline.Events
                .Where(movementEvent =>
                    movementEvent.AtMs <= segment.StartMs &&
                    !string.IsNullOrWhiteSpace(movementEvent.LoadState))
                .OrderByDescending(movementEvent => movementEvent.AtMs)
                .ThenByDescending(movementEvent => movementEvent.EventId, StringComparer.Ordinal)
                .Select(movementEvent => movementEvent.LoadState)
                .FirstOrDefault();

            return MapLoadState(
                string.IsNullOrWhiteSpace(loadState) ? timeline.InitialLoadState : loadState,
                ActorState.Moving);
        }

        private static ActorState MapLoadState(string loadState, ActorState fallback)
        {
            var normalized = (loadState ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "loaded":
                case "carrying":
                case "carry":
                    return ActorState.Carrying;
                case "picking":
                case "pick":
                    return ActorState.Picking;
                case "idle":
                case "waiting":
                    return ActorState.Idle;
                default:
                    return fallback;
            }
        }

        private static Dictionary<string, string> BuildActorTypes(MovementArtifactDto artifact)
        {
            return (artifact.actors ?? Array.Empty<MovementActorDto>())
                .Where(actor => !string.IsNullOrWhiteSpace(actor.actor_id))
                .GroupBy(actor => actor.actor_id, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().actor_type,
                    StringComparer.Ordinal);
        }

        private static Vector3 ToVector3(Vector2 point)
        {
            return new Vector3(point.x, 0f, point.y);
        }

        private static float Seconds(long milliseconds)
        {
            return milliseconds * MillisecondsToSeconds;
        }

        private static void AddIfPresent(ISet<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }
    }
}
