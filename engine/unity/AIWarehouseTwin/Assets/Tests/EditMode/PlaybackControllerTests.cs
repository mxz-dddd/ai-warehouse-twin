using AIWarehouseTwin.Playback;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class PlaybackControllerTests
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
        public void Default_speed_is_one()
        {
            var controller = CreateController();

            Assert.That(controller.Speed, Is.EqualTo(1f));
            Assert.That(controller.SpeedLabel, Is.EqualTo("1×"));
        }

        [Test]
        public void CycleSpeed_rotates_through_speed_labels()
        {
            var controller = CreateController();

            controller.CycleSpeed();
            Assert.That(controller.SpeedLabel, Is.EqualTo("5×"));

            controller.CycleSpeed();
            Assert.That(controller.SpeedLabel, Is.EqualTo("10×"));

            controller.CycleSpeed();
            Assert.That(controller.SpeedLabel, Is.EqualTo("⚡"));

            controller.CycleSpeed();
            Assert.That(controller.SpeedLabel, Is.EqualTo("1×"));
        }

        [Test]
        public void Tick_advances_simulation_time_by_speed()
        {
            var controller = CreateController();

            controller.Tick(2f);

            Assert.That(controller.simulationTime, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void Tick_wraps_simulation_time_by_total_duration()
        {
            var controller = CreateController();
            controller.totalDuration = 5f;

            controller.Tick(4f);
            controller.Tick(3f);

            Assert.That(controller.simulationTime, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void Tick_handles_invalid_total_duration_safely()
        {
            var controller = CreateController();
            controller.totalDuration = 0f;

            controller.Tick(1f);

            Assert.That(controller.simulationTime, Is.EqualTo(0f));
            Assert.That(float.IsNaN(controller.simulationTime), Is.False);

            controller.totalDuration = -1f;
            controller.Tick(1f);

            Assert.That(controller.simulationTime, Is.EqualTo(0f));
            Assert.That(float.IsNaN(controller.simulationTime), Is.False);
        }

        [Test]
        public void Awake_sets_singleton_instance_and_destroy_clears_it()
        {
            var controller = CreateController();

            Assert.That(PlaybackController.Instance, Is.SameAs(controller));

            Object.DestroyImmediate(gameObject);
            gameObject = null;

            Assert.That(PlaybackController.Instance, Is.Null);
        }

        private PlaybackController CreateController()
        {
            gameObject = new GameObject("PlaybackControllerTests");
            return gameObject.AddComponent<PlaybackController>();
        }
    }
}
