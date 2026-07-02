using AIWarehouseTwin.VFX;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class PickCompleteVFXTests
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
        public void EnsureParticleSystem_creates_particle_system()
        {
            var vfx = CreateVfx();

            var system = vfx.EnsureParticleSystem();

            Assert.That(system, Is.Not.Null);
            Assert.That(vfx.HasParticleSystem, Is.True);
            Assert.That(system.GetType().Name, Is.EqualTo("ParticleSystem"));
            Assert.That(system.GetComponent<PickCompleteVFX>(), Is.EqualTo(vfx));
        }

        [Test]
        public void Defaults_use_pick_complete_burst_and_duration()
        {
            var vfx = CreateVfx();
            vfx.EnsureParticleSystem();

            Assert.That(vfx.BurstCount, Is.EqualTo(30));
            Assert.That(vfx.Duration, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(vfx.StartLifetime, Is.EqualTo(0.3f).Within(0.001f));
        }

        [Test]
        public void Default_color_uses_highlight_yellow()
        {
            var vfx = CreateVfx();

            AssertColor(vfx.StartColor, new Color(1f, 0.8784314f, 0.4f, 1f));
        }

        [Test]
        public void PlayAt_sets_position_and_enters_playable_state()
        {
            var vfx = CreateVfx();
            var position = new Vector3(1f, 2f, 3f);

            vfx.PlayAt(position);

            AssertVector(vfx.transform.position, position);
            Assert.That(vfx.IsPlaying, Is.True);
        }

        [Test]
        public void Play_without_external_prefab_is_safe()
        {
            var vfx = CreateVfx();

            Assert.DoesNotThrow(() => vfx.Play());
        }

        private PickCompleteVFX CreateVfx()
        {
            gameObject = new GameObject("PickCompleteVFXTests");
            return gameObject.AddComponent<PickCompleteVFX>();
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
