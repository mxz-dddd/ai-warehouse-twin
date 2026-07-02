using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AIWarehouseTwin.Agent;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Playback;
using AIWarehouseTwin.World;
using UnityEngine;
using UnityEngine.Serialization;

namespace AIWarehouseTwin.Demo
{
    public sealed class Phase2DemoController : MonoBehaviour
    {
        public const string ArtifactFileName = "medium-warehouse-artifact.json";
        public const string MovementArtifactFileName = "movement-artifact.v1.json";
        public const string ActorsRootName = "ActorsRoot";
        public const string WarehouseRootName = "WarehouseRoot";

        [SerializeField]
        [FormerlySerializedAs("createPlaceholderObjects")]
        private bool generateWarehouseStructure = true;

        [SerializeField]
        private bool generateActors = true;

        [SerializeField]
        private WarehouseStructureBuilder structureBuilder;

        [SerializeField]
        private ActorDirector actorDirector;

        [SerializeField]
        private PlaybackController playbackController;

        [SerializeField]
        private WarehousePalette palette;

        [SerializeField]
        private LayoutAutoGeneratorSettings layoutSettings = new LayoutAutoGeneratorSettings();

        private void Start()
        {
            LoadArtifactAndGenerateWarehouse(Application.streamingAssetsPath);
        }

        public static Phase2DemoSummary BuildSummary(RunArtifactDto artifact)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            var actors = new HashSet<string>(StringComparer.Ordinal);
            var routes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in artifact.position_timeline ?? Array.Empty<RunArtifactPositionTimelineEntryDto>())
            {
                AddIfPresent(actors, entry.resource_id);
                AddIfPresent(routes, entry.operation_id);
            }

            if (actors.Count == 0)
            {
                foreach (var resource in artifact.layout?.resources ?? Array.Empty<RunArtifactLayoutResourceDto>())
                {
                    AddIfPresent(actors, resource.resource_id);
                }
            }

            return new Phase2DemoSummary(actors.Count, routes.Count);
        }

        public static string FormatLoadedMessage(Phase2DemoSummary summary)
        {
            return $"Scene loaded. Actors: {summary.ActorCount}, Routes: {summary.RouteCount}";
        }

        public static string FormatStructureGeneratedMessage(Phase2WarehouseStructureSummary summary)
        {
            return $"Warehouse structure generated. Zones: {summary.ZoneCount}, Shelves: {summary.ShelfCount}, Docks: {summary.DockCount}";
        }

        public static string FormatActorsSpawnedMessage(Phase2ActorDirectorSummary summary)
        {
            return $"Actors spawned. Workers: {summary.WorkerCount}, Forklifts: {summary.ForkliftCount}, Routes: {summary.RouteCount}";
        }

        public bool LoadArtifactAndGenerateWarehouse(string streamingAssetsPath)
        {
            try
            {
                var artifactPath = Path.Combine(streamingAssetsPath, ArtifactFileName);
                var artifact = RunArtifactLoader.LoadFromFile(artifactPath);
                var summary = BuildSummary(artifact);

                Debug.Log(FormatLoadedMessage(summary));

                if (generateWarehouseStructure)
                {
                    var structureSummary = GenerateWarehouseStructure(artifact);
                    Debug.Log(FormatStructureGeneratedMessage(structureSummary));
                }

                if (generateActors)
                {
                    var actorSummary = GenerateActors(
                        artifact,
                        Path.Combine(streamingAssetsPath, MovementArtifactFileName));
                    Debug.Log(FormatActorsSpawnedMessage(actorSummary));
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Phase2 demo artifact load failed: {ex.Message}");
                return false;
            }
        }

        public Phase2WarehouseStructureSummary GenerateWarehouseStructure(RunArtifactDto artifact)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            DestroyExistingWarehouseRoots();

            var layout = LayoutAutoGenerator.Generate(artifact, layoutSettings);
            var warehouse = ResolveStructureBuilder().Build(transform, layout, ResolvePalette());
            warehouse.name = WarehouseRootName;

            return Phase2WarehouseStructureSummary.FromLayout(layout);
        }

        public Phase2ActorDirectorSummary GenerateActors(
            RunArtifactDto artifact,
            string movementArtifactPath = null)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            var director = ResolveActorDirector();
            director.Spawner.SetPalette(ResolvePalette());
            director.TickFromPlayback = true;
            director.LoadFromArtifact(ResolveMovementArtifact(artifact, movementArtifactPath));

            var playback = ResolvePlaybackController();
            playback.totalDuration = Mathf.Max(0.001f, artifact.final_world_time_ms * 0.001f);

            return Phase2ActorDirectorSummary.FromDirector(director);
        }

        public void TickActors(float simulationTime)
        {
            ResolveActorDirector().Tick(simulationTime);
        }

        private static void AddIfPresent(ISet<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        private WarehouseStructureBuilder ResolveStructureBuilder()
        {
            if (structureBuilder != null)
            {
                return structureBuilder;
            }

            structureBuilder = GetComponent<WarehouseStructureBuilder>();
            if (structureBuilder == null)
            {
                structureBuilder = gameObject.AddComponent<WarehouseStructureBuilder>();
            }

            return structureBuilder;
        }

        private ActorDirector ResolveActorDirector()
        {
            if (actorDirector != null)
            {
                return actorDirector;
            }

            actorDirector = GetComponent<ActorDirector>();
            if (actorDirector == null)
            {
                actorDirector = FindFirstObjectByType<ActorDirector>();
            }

            if (actorDirector == null)
            {
                actorDirector = gameObject.AddComponent<ActorDirector>();
            }

            return actorDirector;
        }

        private PlaybackController ResolvePlaybackController()
        {
            if (playbackController != null)
            {
                return playbackController;
            }

            playbackController = PlaybackController.Instance;
            if (playbackController == null)
            {
                playbackController = gameObject.AddComponent<PlaybackController>();
            }

            return playbackController;
        }

        private MovementArtifactDto ResolveMovementArtifact(
            RunArtifactDto runArtifact,
            string movementArtifactPath)
        {
            var movementArtifact = MovementArtifactLoader.LoadOptionalFromFile(movementArtifactPath);
            if (movementArtifact.IsAvailable)
            {
                return movementArtifact.Artifact;
            }

            Debug.LogWarning(
                $"Movement artifact unavailable; using deterministic run artifact actor fallback. Reason: {movementArtifact.UnavailableReason}");
            return BuildFallbackMovementArtifact(runArtifact);
        }

        private static MovementArtifactDto BuildFallbackMovementArtifact(RunArtifactDto artifact)
        {
            var resources = (artifact.layout?.resources ?? Array.Empty<RunArtifactLayoutResourceDto>())
                .Where(resource => !string.IsNullOrWhiteSpace(resource.resource_id))
                .OrderBy(resource => resource.resource_id, StringComparer.Ordinal)
                .ToArray();
            var dockResources = resources
                .Where(resource => IsDockResource(resource.resource_id))
                .ToArray();
            var workerResources = resources
                .Where(resource => !IsDockResource(resource.resource_id))
                .ToArray();
            var finalTimeMs = Math.Max(1L, artifact.final_world_time_ms);

            return new MovementArtifactDto
            {
                schema_version = MovementArtifactLoader.SupportedSchemaVersion,
                artifact_kind = MovementArtifactLoader.SupportedArtifactKind,
                scenario_id = artifact.scenario_id,
                run_id = "phase2-demo-fallback",
                seed = artifact.seed,
                source_run_artifact = ArtifactFileName,
                warehouse_graph = new WarehouseGraphDto
                {
                    nodes = resources.Select(resource => new WarehouseGraphNodeDto
                    {
                        node_id = string.IsNullOrWhiteSpace(resource.node_id)
                            ? resource.resource_id
                            : resource.node_id,
                        node_type = IsDockResource(resource.resource_id) ? "dock" : "workstation",
                        x = resource.x,
                        y = resource.y,
                    }).ToArray(),
                },
                actors = BuildFallbackActors(dockResources, workerResources),
                movement_events = BuildFallbackMovementEvents(dockResources, workerResources, finalTimeMs),
                route_segments = Array.Empty<MovementRouteSegmentDto>(),
                provenance = new MovementProvenanceDto
                {
                    movement_enabled = false,
                    graph_source = "run-artifact layout resources",
                    deterministic_generation_policy =
                        "Demo fallback actor events from static run artifact layout resources; not calibrated real trajectory",
                },
            };
        }

        private static MovementActorDto[] BuildFallbackActors(
            RunArtifactLayoutResourceDto[] dockResources,
            RunArtifactLayoutResourceDto[] workerResources)
        {
            var actors = new List<MovementActorDto>();

            if (workerResources.Length > 0)
            {
                actors.Add(new MovementActorDto
                {
                    actor_id = "worker-demo-01",
                    actor_type = "worker",
                    resource_id = workerResources[0].resource_id,
                    initial_node_id = NodeId(workerResources[0]),
                    load_state = "idle",
                });
            }

            if (dockResources.Length > 0)
            {
                actors.Add(new MovementActorDto
                {
                    actor_id = "forklift-demo-01",
                    actor_type = "forklift",
                    resource_id = dockResources[0].resource_id,
                    initial_node_id = NodeId(dockResources[0]),
                    load_state = "loaded",
                });
            }

            return actors.ToArray();
        }

        private static MovementEventDto[] BuildFallbackMovementEvents(
            RunArtifactLayoutResourceDto[] dockResources,
            RunArtifactLayoutResourceDto[] workerResources,
            long finalTimeMs)
        {
            var events = new List<MovementEventDto>();
            AddFallbackEvents(events, "worker-demo-01", workerResources, finalTimeMs, "idle");
            AddFallbackEvents(events, "forklift-demo-01", dockResources, finalTimeMs, "loaded");
            return events
                .OrderBy(movementEvent => movementEvent.at_ms)
                .ThenBy(movementEvent => movementEvent.event_id, StringComparer.Ordinal)
                .ToArray();
        }

        private static void AddFallbackEvents(
            ICollection<MovementEventDto> events,
            string actorId,
            RunArtifactLayoutResourceDto[] resources,
            long finalTimeMs,
            string loadState)
        {
            if (resources.Length == 0)
            {
                return;
            }

            AddFallbackEvent(events, actorId, resources[0], 0, "fallback.start", loadState);
            AddFallbackEvent(
                events,
                actorId,
                resources.Length > 1 ? resources[1] : resources[0],
                finalTimeMs,
                "fallback.finish",
                loadState);
        }

        private static void AddFallbackEvent(
            ICollection<MovementEventDto> events,
            string actorId,
            RunArtifactLayoutResourceDto resource,
            long atMs,
            string eventType,
            string loadState)
        {
            events.Add(new MovementEventDto
            {
                event_id = $"{actorId}-{eventType}",
                actor_id = actorId,
                event_type = eventType,
                at_ms = atMs,
                node_id = NodeId(resource),
                x = resource.x,
                y = resource.y,
                load_state = loadState,
                related_resource_id = resource.resource_id,
            });
        }

        private static bool IsDockResource(string resourceId)
        {
            return (resourceId ?? string.Empty)
                .IndexOf("dock", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NodeId(RunArtifactLayoutResourceDto resource)
        {
            return string.IsNullOrWhiteSpace(resource.node_id)
                ? resource.resource_id
                : resource.node_id;
        }

        private WarehousePalette ResolvePalette()
        {
            if (palette != null)
            {
                return palette;
            }

            palette = ScriptableObject.CreateInstance<WarehousePalette>();
            palette.ApplyDefaultColors();
            return palette;
        }

        private static void DestroyExistingWarehouseRoots()
        {
            GameObject existing;
            while ((existing = GameObject.Find(WarehouseRootName)) != null)
            {
                existing.name = $"{WarehouseRootName} (Replaced)";
                if (Application.isPlaying)
                {
                    Destroy(existing);
                }
                else
                {
                    DestroyImmediate(existing);
                }
            }
        }
    }

    public readonly struct Phase2ActorDirectorSummary
    {
        public Phase2ActorDirectorSummary(int workerCount, int forkliftCount, int routeCount)
        {
            WorkerCount = workerCount;
            ForkliftCount = forkliftCount;
            RouteCount = routeCount;
        }

        public int WorkerCount { get; }

        public int ForkliftCount { get; }

        public int RouteCount { get; }

        public static Phase2ActorDirectorSummary FromDirector(ActorDirector director)
        {
            if (director == null)
            {
                throw new ArgumentNullException(nameof(director));
            }

            var workers = 0;
            var forklifts = 0;
            var routes = 0;

            foreach (var actor in director.Actors)
            {
                switch (actor)
                {
                    case WorkerActor _:
                        workers++;
                        break;
                    case ForkliftActor _:
                        forklifts++;
                        break;
                }

                routes += actor?.Route?.Count ?? 0;
            }

            return new Phase2ActorDirectorSummary(workers, forklifts, routes);
        }
    }

    public readonly struct Phase2DemoSummary
    {
        public Phase2DemoSummary(int actorCount, int routeCount)
        {
            ActorCount = actorCount;
            RouteCount = routeCount;
        }

        public int ActorCount { get; }

        public int RouteCount { get; }
    }

    public readonly struct Phase2WarehouseStructureSummary
    {
        public Phase2WarehouseStructureSummary(int zoneCount, int shelfCount, int dockCount)
        {
            ZoneCount = zoneCount;
            ShelfCount = shelfCount;
            DockCount = dockCount;
        }

        public int ZoneCount { get; }

        public int ShelfCount { get; }

        public int DockCount { get; }

        public static Phase2WarehouseStructureSummary FromLayout(WarehouseStructureLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var zones = 0;
            var shelves = 0;
            var docks = 0;

            foreach (var element in layout.Elements)
            {
                switch (element.Kind)
                {
                    case WarehouseStructureElementKind.Zone:
                        zones++;
                        break;
                    case WarehouseStructureElementKind.Shelf:
                        shelves++;
                        break;
                    case WarehouseStructureElementKind.Dock:
                        docks++;
                        break;
                }
            }

            return new Phase2WarehouseStructureSummary(zones, shelves, docks);
        }
    }
}
