using AIWarehouseTwin.Agent;
using AIWarehouseTwin.Artifact;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AIWarehouseTwin.Tests
{
    public sealed class ActorDirectorTests
    {
        private GameObject gameObject;

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Null_artifact_is_safe()
        {
            var director = CreateDirector();

            Assert.DoesNotThrow(() => director.LoadFromArtifact((MovementArtifactDto)null));

            Assert.That(director.Actors, Is.Empty);
        }

        [Test]
        public void Empty_artifact_is_safe()
        {
            var director = CreateDirector();

            Assert.DoesNotThrow(() => director.LoadFromArtifact(new MovementArtifactDto()));

            Assert.That(director.Actors, Is.Empty);
        }

        [Test]
        public void LoadFromArtifact_spawns_worker_and_forklift()
        {
            var director = CreateDirector();

            director.LoadFromArtifact(MovementArtifact());

            Assert.That(director.Actors, Has.Count.EqualTo(2));
            Assert.That(director.Actors[0], Is.InstanceOf<ForkliftActor>());
            Assert.That(director.Actors[1], Is.InstanceOf<WorkerActor>());
        }

        [Test]
        public void LoadFromArtifact_loads_routes_into_actors()
        {
            var director = CreateDirector();

            director.LoadFromArtifact(MovementArtifact());

            Assert.That(director.Actors[0].Route, Has.Count.EqualTo(1));
            Assert.That(director.Actors[1].Route, Has.Count.EqualTo(1));
            Assert.That(director.Actors[0].Route[0].state, Is.EqualTo(ActorState.Carrying));
            Assert.That(director.Actors[1].Route[0].state, Is.EqualTo(ActorState.Moving));
        }

        [Test]
        public void Tick_advances_actor_position_without_playback_singleton()
        {
            var director = CreateDirector();
            director.LoadFromArtifact(MovementArtifact());

            director.Tick(0.5f);

            AssertVector(director.Actors[0].transform.position, new Vector3(5f, 0f, 0f));
            AssertVector(director.Actors[1].transform.position, new Vector3(0f, 0f, 5f));
        }

        [Test]
        public void Clear_removes_actor_list_and_game_objects()
        {
            var director = CreateDirector();
            director.LoadFromArtifact(MovementArtifact());

            director.Clear();

            Assert.That(director.Actors, Is.Empty);
            Assert.That(director.Spawner.ActorsRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void Event_only_artifact_uses_deterministic_fallback_route()
        {
            var director = CreateDirector();

            director.LoadFromArtifact(EventOnlyMovementArtifact());
            director.Tick(1f);

            Assert.That(director.Actors, Has.Count.EqualTo(1));
            Assert.That(director.Actors[0].Route, Has.Count.EqualTo(1));
            AssertVector(director.Actors[0].transform.position, new Vector3(1f, 0f, 2f));
        }

        [Test]
        public void Unknown_actor_type_is_skipped_safely()
        {
            var director = CreateDirector();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Unknown actor type 'drone'"));

            director.LoadFromArtifact(UnknownActorArtifact());

            Assert.That(director.Actors, Is.Empty);
            Assert.That(director.Spawner.LastWarning, Does.Contain("drone"));
        }

        private ActorDirector CreateDirector()
        {
            gameObject = new GameObject("ActorDirectorTests");
            return gameObject.AddComponent<ActorDirector>();
        }

        private static MovementArtifactDto MovementArtifact()
        {
            return new MovementArtifactDto
            {
                schema_version = MovementArtifactLoader.SupportedSchemaVersion,
                artifact_kind = MovementArtifactLoader.SupportedArtifactKind,
                scenario_id = "director-tests",
                warehouse_graph = Graph(),
                actors = new[]
                {
                    new MovementActorDto
                    {
                        actor_id = "forklift-01",
                        actor_type = "forklift",
                        load_state = "loaded",
                    },
                    new MovementActorDto
                    {
                        actor_id = "worker-01",
                        actor_type = "worker",
                        load_state = "empty",
                    },
                },
                route_segments = new[]
                {
                    new MovementRouteSegmentDto
                    {
                        segment_id = "seg-forklift",
                        actor_id = "forklift-01",
                        from_node_id = "node-a",
                        to_node_id = "node-b",
                        start_ms = 0,
                        end_ms = 1000,
                    },
                    new MovementRouteSegmentDto
                    {
                        segment_id = "seg-worker",
                        actor_id = "worker-01",
                        from_node_id = "node-a",
                        to_node_id = "node-c",
                        start_ms = 0,
                        end_ms = 1000,
                    },
                },
            };
        }

        private static MovementArtifactDto EventOnlyMovementArtifact()
        {
            return new MovementArtifactDto
            {
                schema_version = MovementArtifactLoader.SupportedSchemaVersion,
                artifact_kind = MovementArtifactLoader.SupportedArtifactKind,
                scenario_id = "director-tests",
                warehouse_graph = Graph(),
                actors = new[]
                {
                    new MovementActorDto { actor_id = "worker-01", actor_type = "worker" },
                },
                movement_events = new[]
                {
                    new MovementEventDto
                    {
                        event_id = "evt-1",
                        actor_id = "worker-01",
                        at_ms = 1000,
                        x = 1,
                        y = 2,
                    },
                },
            };
        }

        private static MovementArtifactDto UnknownActorArtifact()
        {
            var artifact = MovementArtifact();
            artifact.actors = new[]
            {
                new MovementActorDto { actor_id = "drone-01", actor_type = "drone" },
            };
            artifact.route_segments = new[]
            {
                new MovementRouteSegmentDto
                {
                    segment_id = "seg-drone",
                    actor_id = "drone-01",
                    from_node_id = "node-a",
                    to_node_id = "node-b",
                    start_ms = 0,
                    end_ms = 1000,
                },
            };
            return artifact;
        }

        private static WarehouseGraphDto Graph()
        {
            return new WarehouseGraphDto
            {
                nodes = new[]
                {
                    new WarehouseGraphNodeDto { node_id = "node-a", x = 0, y = 0 },
                    new WarehouseGraphNodeDto { node_id = "node-b", x = 10, y = 0 },
                    new WarehouseGraphNodeDto { node_id = "node-c", x = 0, y = 10 },
                },
            };
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f));
        }
    }
}
