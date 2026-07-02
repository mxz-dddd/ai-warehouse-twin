using System;
using System.IO;
using System.Linq;
using AIWarehouseTwin.Agent;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Demo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AIWarehouseTwin.Tests
{
    public sealed class Phase2DemoControllerTests
    {
        private GameObject controllerRoot;

        [TearDown]
        public void TearDown()
        {
            if (controllerRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(controllerRoot);
            }

            DestroyWarehouseRoots();
            DestroyActorRoots();
        }

        [Test]
        public void MediumArtifact_exists_in_streaming_assets()
        {
            Assert.That(File.Exists(MediumArtifactPath()), Is.True);
        }

        [Test]
        public void BuildSummary_counts_actors_and_routes_from_medium_artifact()
        {
            var artifact = RunArtifactLoader.LoadFromFile(MediumArtifactPath());

            var summary = Phase2DemoController.BuildSummary(artifact);

            Assert.That(summary.ActorCount, Is.EqualTo(4));
            Assert.That(summary.RouteCount, Is.EqualTo(17));
        }

        [Test]
        public void FormatLoadedMessage_uses_required_console_format()
        {
            var message = Phase2DemoController.FormatLoadedMessage(new Phase2DemoSummary(4, 17));

            Assert.That(message, Is.EqualTo("Scene loaded. Actors: 4, Routes: 17"));
        }

        [Test]
        public void FormatStructureGeneratedMessage_uses_required_console_format()
        {
            var message = Phase2DemoController.FormatStructureGeneratedMessage(
                new Phase2WarehouseStructureSummary(3, 12, 2));

            Assert.That(message, Is.EqualTo("Warehouse structure generated. Zones: 3, Shelves: 12, Docks: 2"));
        }

        [Test]
        public void FormatActorsSpawnedMessage_uses_required_console_format()
        {
            var message = Phase2DemoController.FormatActorsSpawnedMessage(new Phase2ActorDirectorSummary(1, 1, 2));

            Assert.That(message, Is.EqualTo("Actors spawned. Workers: 1, Forklifts: 1, Routes: 2"));
        }

        [Test]
        public void BuildSummary_falls_back_to_layout_resources_when_timeline_is_empty()
        {
            var artifact = new RunArtifactDto
            {
                schema_version = "run-artifact.v1",
                artifact_kind = "warehouse-simulation-run",
                scenario_id = "demo",
                layout = new RunArtifactLayoutDto
                {
                    resources = new[]
                    {
                        new RunArtifactLayoutResourceDto { resource_id = "dock-1" },
                        new RunArtifactLayoutResourceDto { resource_id = "station-1" },
                    },
                },
                position_timeline = new RunArtifactPositionTimelineEntryDto[0],
                event_log = new string[0],
            };

            var summary = Phase2DemoController.BuildSummary(artifact);

            Assert.That(summary.ActorCount, Is.EqualTo(2));
            Assert.That(summary.RouteCount, Is.EqualTo(0));
        }

        [Test]
        public void LoadArtifactAndGenerateWarehouse_reads_medium_artifact_and_builds_structure()
        {
            var controller = CreateController();
            ExpectFallbackMovementWarning();

            var loaded = controller.LoadArtifactAndGenerateWarehouse(MediumArtifactDirectory());
            var warehouse = GameObject.Find(Phase2DemoController.WarehouseRootName);
            var actorsRoot = GameObject.Find(Phase2DemoController.ActorsRootName);

            Assert.That(loaded, Is.True);
            Assert.That(warehouse, Is.Not.Null);
            Assert.That(warehouse.transform.Find("Floor"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Zones"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Zones/ReceivingZone"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Zones/StorageZone"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Zones/ShippingZone"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Shelves"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Docks"), Is.Not.Null);
            Assert.That(actorsRoot, Is.Not.Null);
            Assert.That(actorsRoot.transform.childCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void GenerateWarehouseStructure_reports_generated_counts()
        {
            var controller = CreateController();
            var artifact = RunArtifactLoader.LoadFromFile(MediumArtifactPath());

            var summary = controller.GenerateWarehouseStructure(artifact);

            Assert.That(summary.ZoneCount, Is.EqualTo(3));
            Assert.That(summary.ShelfCount, Is.GreaterThanOrEqualTo(12));
            Assert.That(summary.DockCount, Is.EqualTo(2));
        }

        [Test]
        public void GenerateWarehouseStructure_replaces_existing_root_without_stacking()
        {
            var controller = CreateController();
            var artifact = RunArtifactLoader.LoadFromFile(MediumArtifactPath());

            controller.GenerateWarehouseStructure(artifact);
            controller.GenerateWarehouseStructure(artifact);

            var rootCount = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Count(go => go.name == Phase2DemoController.WarehouseRootName);
            Assert.That(rootCount, Is.EqualTo(1));
        }

        [Test]
        public void GenerateActors_spawns_worker_and_forklift_under_stable_root()
        {
            var controller = CreateController();
            var artifact = RunArtifactLoader.LoadFromFile(MediumArtifactPath());
            ExpectFallbackMovementWarning();

            var summary = controller.GenerateActors(artifact);
            var director = UnityEngine.Object.FindFirstObjectByType<ActorDirector>();
            var actorsRoot = GameObject.Find(Phase2DemoController.ActorsRootName);

            Assert.That(summary.WorkerCount, Is.EqualTo(1));
            Assert.That(summary.ForkliftCount, Is.EqualTo(1));
            Assert.That(summary.RouteCount, Is.EqualTo(2));
            Assert.That(director, Is.Not.Null);
            Assert.That(director.TickFromPlayback, Is.True);
            Assert.That(director.Actors.Any(actor => actor is WorkerActor), Is.True);
            Assert.That(director.Actors.Any(actor => actor is ForkliftActor), Is.True);
            Assert.That(actorsRoot, Is.Not.Null);
            Assert.That(actorsRoot.transform.childCount, Is.EqualTo(2));
        }

        [Test]
        public void GenerateActors_reuses_actor_root_without_stacking()
        {
            var controller = CreateController();
            var artifact = RunArtifactLoader.LoadFromFile(MediumArtifactPath());
            ExpectFallbackMovementWarning();
            ExpectFallbackMovementWarning();

            controller.GenerateActors(artifact);
            controller.GenerateActors(artifact);

            var rootCount = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Count(go => go.name == Phase2DemoController.ActorsRootName);
            var director = UnityEngine.Object.FindFirstObjectByType<ActorDirector>();

            Assert.That(rootCount, Is.EqualTo(1));
            Assert.That(director.Actors, Has.Count.EqualTo(2));
            Assert.That(director.Spawner.ActorsRoot.childCount, Is.EqualTo(2));
        }

        [Test]
        public void TickActors_advances_fallback_actor_position()
        {
            var controller = CreateController();
            var artifact = RunArtifactLoader.LoadFromFile(MediumArtifactPath());
            ExpectFallbackMovementWarning();

            controller.GenerateActors(artifact);
            var director = UnityEngine.Object.FindFirstObjectByType<ActorDirector>();
            var movingActor = director.Actors.First(actor =>
                actor.Route.Any(route => Vector3.Distance(route.from, route.to) > 0.001f));

            controller.TickActors(0f);
            var startPosition = movingActor.transform.position;
            controller.TickActors(artifact.final_world_time_ms * 0.001f);
            var endPosition = movingActor.transform.position;

            Assert.That(Vector3.Distance(startPosition, endPosition), Is.GreaterThan(0.001f));
        }

        [Test]
        public void LoadArtifactAndGenerateWarehouse_returns_false_and_logs_when_artifact_missing()
        {
            var controller = CreateController();
            var missingDirectory = Path.Combine(Application.temporaryCachePath, $"missing-phase2-artifact-{Guid.NewGuid():N}");
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Phase2 demo artifact load failed: .*RunArtifact file was not found"));

            var loaded = controller.LoadArtifactAndGenerateWarehouse(missingDirectory);

            Assert.That(loaded, Is.False);
            Assert.That(GameObject.Find(Phase2DemoController.WarehouseRootName), Is.Null);
        }

        [Test]
        public void LoadArtifactAndGenerateWarehouse_returns_false_and_logs_when_artifact_invalid()
        {
            var controller = CreateController();
            var directory = Path.Combine(Application.temporaryCachePath, $"invalid-phase2-artifact-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, Phase2DemoController.ArtifactFileName), "{}");
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Phase2 demo artifact load failed: .*Unsupported RunArtifact schema_version"));

            var loaded = controller.LoadArtifactAndGenerateWarehouse(directory);

            Assert.That(loaded, Is.False);
            Assert.That(GameObject.Find(Phase2DemoController.WarehouseRootName), Is.Null);
        }

        private static string MediumArtifactPath()
        {
            return Path.Combine(
                MediumArtifactDirectory(),
                Phase2DemoController.ArtifactFileName);
        }

        private static string MediumArtifactDirectory()
        {
            return Path.Combine(
                Application.dataPath,
                "StreamingAssets");
        }

        private Phase2DemoController CreateController()
        {
            controllerRoot = new GameObject("Phase2DemoControllerTests");
            return controllerRoot.AddComponent<Phase2DemoController>();
        }

        private static void DestroyWarehouseRoots()
        {
            GameObject existing;
            while ((existing = GameObject.Find(Phase2DemoController.WarehouseRootName)) != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static void DestroyActorRoots()
        {
            GameObject existing;
            while ((existing = GameObject.Find(Phase2DemoController.ActorsRootName)) != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static void ExpectFallbackMovementWarning()
        {
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex("Movement artifact unavailable; using deterministic run artifact actor fallback"));
        }
    }
}
