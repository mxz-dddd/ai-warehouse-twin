using System.Collections.Generic;
using AIWarehouseTwin.Agent;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class ActorControllerTests
    {
        private GameObject gameObject;

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LoadRoute_accepts_null_route()
        {
            var actor = CreateActor();

            Assert.DoesNotThrow(() => actor.LoadRoute(null));
            Assert.That(actor.Route, Is.Empty);
        }

        [Test]
        public void Tick_with_empty_route_is_safe()
        {
            var actor = CreateActor();

            Assert.DoesNotThrow(() => actor.Tick(5f));
            Assert.That(actor.transform.position, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Tick_interpolates_midpoint_of_single_segment()
        {
            var actor = CreateActor();
            actor.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 10f, Vector3.zero, new Vector3(10f, 0f, 0f), ActorState.Moving),
            });

            actor.Tick(5f);

            AssertVector(actor.transform.position, new Vector3(5f, 0f, 0f));
        }

        [Test]
        public void Tick_clamps_before_first_and_after_last_segment()
        {
            var actor = CreateActor();
            actor.LoadRoute(new[]
            {
                new ActorRouteSegment(10f, 20f, new Vector3(1f, 0f, 2f), new Vector3(5f, 0f, 6f), ActorState.Moving),
            });

            actor.Tick(0f);
            AssertVector(actor.transform.position, new Vector3(1f, 0f, 2f));

            actor.Tick(30f);
            AssertVector(actor.transform.position, new Vector3(5f, 0f, 6f));
        }

        [Test]
        public void Tick_updates_facing_when_moving_direction_is_non_zero()
        {
            var actor = CreateActor();
            actor.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 10f, Vector3.zero, Vector3.forward, ActorState.Moving),
            });

            actor.Tick(5f);

            Assert.That(Quaternion.Angle(actor.transform.rotation, Quaternion.LookRotation(Vector3.forward)), Is.LessThan(0.001f));
        }

        [Test]
        public void State_changes_only_fire_once_for_repeated_same_state_ticks()
        {
            var actor = CreateActor();
            actor.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 10f, Vector3.zero, Vector3.right, ActorState.Moving),
            });

            actor.Tick(1f);
            actor.Tick(2f);
            actor.Tick(3f);

            Assert.That(actor.StateChanges, Is.EqualTo(new[] { ActorState.Moving }));
        }

        [Test]
        public void State_change_fires_when_segment_state_changes()
        {
            var actor = CreateActor();
            actor.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 5f, Vector3.zero, Vector3.right, ActorState.Moving),
                new ActorRouteSegment(5f, 10f, Vector3.right, Vector3.right, ActorState.Picking),
            });

            actor.Tick(1f);
            actor.Tick(7f);

            Assert.That(actor.StateChanges, Is.EqualTo(new[] { ActorState.Moving, ActorState.Picking }));
        }

        private TestActorController CreateActor()
        {
            gameObject = new GameObject("ActorControllerTests");
            return gameObject.AddComponent<TestActorController>();
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f));
        }

        private sealed class TestActorController : ActorController
        {
            public readonly List<ActorState> StateChanges = new List<ActorState>();

            protected override void OnStateChanged(ActorState next)
            {
                StateChanges.Add(next);
            }
        }
    }
}
