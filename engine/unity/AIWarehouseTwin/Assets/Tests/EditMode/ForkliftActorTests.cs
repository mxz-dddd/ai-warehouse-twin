using AIWarehouseTwin.Agent;
using AIWarehouseTwin.World;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class ForkliftActorTests
    {
        private GameObject gameObject;
        private WarehousePalette palette;

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }

            if (palette != null)
            {
                Object.DestroyImmediate(palette);
            }
        }

        [Test]
        public void ForkliftActor_inherits_actor_controller()
        {
            var forklift = CreateForklift();

            Assert.That(forklift, Is.InstanceOf<ActorController>());
        }

        [Test]
        public void EnsureVisuals_creates_chassis_mast_and_forks()
        {
            var forklift = CreateForklift();

            forklift.EnsureVisuals();

            Assert.That(forklift.Chassis, Is.Not.Null);
            Assert.That(forklift.Mast, Is.Not.Null);
            Assert.That(forklift.LeftFork, Is.Not.Null);
            Assert.That(forklift.RightFork, Is.Not.Null);
            Assert.That(forklift.Chassis.name, Is.EqualTo("ForkliftChassis"));
            Assert.That(forklift.Mast.name, Is.EqualTo("ForkliftMast"));
            Assert.That(forklift.LeftFork.name, Is.EqualTo("ForkliftLeftFork"));
            Assert.That(forklift.RightFork.name, Is.EqualTo("ForkliftRightFork"));
            AssertVector(forklift.Chassis.transform.localScale, new Vector3(1.2f, 0.6f, 0.8f));
        }

        [Test]
        public void Default_fork_height_is_empty_height()
        {
            var forklift = CreateForklift();

            forklift.EnsureVisuals();

            Assert.That(forklift.ForkHeight, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(forklift.LeftFork.transform.localPosition.y, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(forklift.RightFork.transform.localPosition.y, Is.EqualTo(0.1f).Within(0.001f));
        }

        [Test]
        public void Carrying_state_raises_forks()
        {
            var forklift = CreateForklift();
            forklift.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 1f, Vector3.zero, Vector3.right, ActorState.Carrying),
            });

            forklift.Tick(0.5f);

            Assert.That(forklift.ForkHeight, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(forklift.LeftFork.transform.localPosition.y, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(forklift.RightFork.transform.localPosition.y, Is.EqualTo(1.2f).Within(0.001f));
        }

        [Test]
        public void Tick_interpolates_route_midpoint()
        {
            var forklift = CreateForklift();
            forklift.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 10f, Vector3.zero, new Vector3(10f, 0f, 0f), ActorState.Moving),
            });

            forklift.Tick(5f);

            AssertVector(forklift.transform.position, new Vector3(5f, 0f, 0f));
        }

        [Test]
        public void Null_and_empty_routes_are_safe()
        {
            var forklift = CreateForklift();

            Assert.DoesNotThrow(() => forklift.LoadRoute(null));
            Assert.DoesNotThrow(() => forklift.Tick(1f));

            forklift.LoadRoute(new ActorRouteSegment[0]);

            Assert.DoesNotThrow(() => forklift.Tick(1f));
        }

        [Test]
        public void EnsureVisuals_uses_palette_forklift_color_when_provided()
        {
            var forklift = CreateForklift();
            palette = ScriptableObject.CreateInstance<WarehousePalette>();
            palette.ApplyDefaultColors();

            forklift.SetPalette(palette);
            forklift.EnsureVisuals();

            var chassisColor = forklift.Chassis.GetComponent<Renderer>().sharedMaterial.color;
            AssertColor(chassisColor, palette.forklift);
        }

        private ForkliftActor CreateForklift()
        {
            gameObject = new GameObject("ForkliftActorTests");
            return gameObject.AddComponent<ForkliftActor>();
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f));
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
