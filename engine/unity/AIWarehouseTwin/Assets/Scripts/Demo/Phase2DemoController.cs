using System;
using System.Collections.Generic;
using System.IO;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.World;
using UnityEngine;
using UnityEngine.Serialization;

namespace AIWarehouseTwin.Demo
{
    public sealed class Phase2DemoController : MonoBehaviour
    {
        public const string ArtifactFileName = "medium-warehouse-artifact.json";
        public const string WarehouseRootName = "WarehouseRoot";

        [SerializeField]
        [FormerlySerializedAs("createPlaceholderObjects")]
        private bool generateWarehouseStructure = true;

        [SerializeField]
        private WarehouseStructureBuilder structureBuilder;

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
