using AIWarehouseTwin.Camera;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class IsoCameraControllerTests
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
        public void ApplyDefaults_configures_camera_for_isometric_orthographic_view()
        {
            var controller = CreateController(out var camera);

            controller.ApplyDefaults();

            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.orthographicSize, Is.EqualTo(12f).Within(0.001f));
            AssertRotation(controller.transform.rotation, Quaternion.Euler(35f, 45f, 0f));
        }

        [Test]
        public void ApplyZoom_clamps_to_minimum_ortho_size()
        {
            var controller = CreateController(out var camera);
            controller.ApplyDefaults();

            controller.ApplyZoom(100f);

            Assert.That(camera.orthographicSize, Is.EqualTo(controller.MinOrthoSize).Within(0.001f));
        }

        [Test]
        public void ApplyZoom_clamps_to_maximum_ortho_size()
        {
            var controller = CreateController(out var camera);
            controller.ApplyDefaults();

            controller.ApplyZoom(-100f);

            Assert.That(camera.orthographicSize, Is.EqualTo(controller.MaxOrthoSize).Within(0.001f));
        }

        [Test]
        public void ApplyPan_moves_transform_position()
        {
            var controller = CreateController(out _);
            controller.ApplyDefaults();
            var initialPosition = controller.transform.position;

            controller.ApplyPan(new Vector2(2f, -1f));

            Assert.That(controller.transform.position, Is.Not.EqualTo(initialPosition));
        }

        private IsoCameraController CreateController(out UnityEngine.Camera camera)
        {
            gameObject = new GameObject("IsoCameraControllerTests");
            camera = gameObject.AddComponent<UnityEngine.Camera>();
            return gameObject.AddComponent<IsoCameraController>();
        }

        private static void AssertRotation(Quaternion actual, Quaternion expected)
        {
            Assert.That(Quaternion.Angle(actual, expected), Is.LessThan(0.001f));
        }
    }
}
