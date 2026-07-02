using System.Text.RegularExpressions;
using AIWarehouseTwin.Agent;
using AIWarehouseTwin.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AIWarehouseTwin.Tests
{
    public sealed class AgentSpawnerTests
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
        public void SpawnActor_creates_worker_actor()
        {
            var spawner = CreateSpawner();

            var actor = spawner.SpawnActor("worker-01", "worker");

            Assert.That(actor, Is.InstanceOf<WorkerActor>());
            Assert.That(actor.actorId, Is.EqualTo("worker-01"));
            Assert.That(actor.gameObject.name, Is.EqualTo("Worker_worker-01"));
            Assert.That(actor.transform.parent, Is.EqualTo(spawner.ActorsRoot));
        }

        [Test]
        public void SpawnActor_creates_forklift_actor()
        {
            var spawner = CreateSpawner();

            var actor = spawner.SpawnActor("forklift-01", "forklift");

            Assert.That(actor, Is.InstanceOf<ForkliftActor>());
            Assert.That(actor.actorId, Is.EqualTo("forklift-01"));
            Assert.That(actor.gameObject.name, Is.EqualTo("Forklift_forklift-01"));
        }

        [Test]
        public void ActorsRoot_name_is_stable()
        {
            var spawner = CreateSpawner();

            Assert.That(spawner.ActorsRoot.name, Is.EqualTo("ActorsRoot"));
        }

        [Test]
        public void ClearActors_removes_spawned_actor_objects()
        {
            var spawner = CreateSpawner();
            spawner.SpawnActor("worker-01", "worker");
            spawner.SpawnActor("forklift-01", "forklift");

            spawner.ClearActors();

            Assert.That(spawner.SpawnedActors, Is.Empty);
            Assert.That(spawner.ActorsRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void Unknown_actor_type_is_safe_and_records_warning()
        {
            var spawner = CreateSpawner();
            LogAssert.Expect(LogType.Warning, new Regex("Unknown actor type 'drone'"));

            var actor = spawner.SpawnActor("drone-01", "drone");

            Assert.That(actor, Is.Null);
            Assert.That(spawner.LastWarning, Does.Contain("drone"));
            Assert.That(spawner.ActorsRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void Palette_is_injected_into_created_actor()
        {
            var spawner = CreateSpawner();
            palette = ScriptableObject.CreateInstance<WarehousePalette>();
            palette.ApplyDefaultColors();

            spawner.SetPalette(palette);
            var actor = (ForkliftActor)spawner.SpawnActor("forklift-01", "forklift");

            var color = actor.Chassis.GetComponent<Renderer>().sharedMaterial.color;
            Assert.That(color.r, Is.EqualTo(palette.forklift.r).Within(0.001f));
            Assert.That(color.g, Is.EqualTo(palette.forklift.g).Within(0.001f));
            Assert.That(color.b, Is.EqualTo(palette.forklift.b).Within(0.001f));
        }

        private AgentSpawner CreateSpawner()
        {
            gameObject = new GameObject("AgentSpawnerTests");
            return gameObject.AddComponent<AgentSpawner>();
        }
    }
}
