using System;
using System.Collections.Generic;
using AIWarehouseTwin.Artifact;
using Sim.Contracts.Artifacts;
using UnityEngine;

namespace AIWarehouseTwin.World
{
    public enum WarehouseStructureElementKind
    {
        Zone,
        Shelf,
        Dock
    }

    public readonly struct WarehouseStructureElement
    {
        public WarehouseStructureElement(
            string id,
            WarehouseStructureElementKind kind,
            string role,
            Vector3 center,
            Vector3 size,
            int row = 0,
            int column = 0)
        {
            Id = id;
            Kind = kind;
            Role = role;
            Center = center;
            Size = size;
            Row = row;
            Column = column;
        }

        public string Id { get; }
        public WarehouseStructureElementKind Kind { get; }
        public string Role { get; }
        public Vector3 Center { get; }
        public Vector3 Size { get; }
        public int Row { get; }
        public int Column { get; }
    }

    public sealed class WarehouseStructureLayout
    {
        public WarehouseStructureLayout(
            float widthM,
            float lengthM,
            IReadOnlyList<WarehouseStructureElement> elements)
        {
            WidthM = widthM;
            LengthM = lengthM;
            Elements = elements ?? Array.Empty<WarehouseStructureElement>();
        }

        public float WidthM { get; }
        public float LengthM { get; }
        public IReadOnlyList<WarehouseStructureElement> Elements { get; }

        public IEnumerable<WarehouseStructureElement> Zones => ElementsOfKind(WarehouseStructureElementKind.Zone);
        public IEnumerable<WarehouseStructureElement> Shelves => ElementsOfKind(WarehouseStructureElementKind.Shelf);
        public IEnumerable<WarehouseStructureElement> Docks => ElementsOfKind(WarehouseStructureElementKind.Dock);

        public WarehouseStructureElement Find(string id)
        {
            foreach (var element in Elements)
            {
                if (element.Id == id)
                {
                    return element;
                }
            }

            throw new ArgumentException($"Warehouse structure element '{id}' was not found.", nameof(id));
        }

        private IEnumerable<WarehouseStructureElement> ElementsOfKind(WarehouseStructureElementKind kind)
        {
            foreach (var element in Elements)
            {
                if (element.Kind == kind)
                {
                    yield return element;
                }
            }
        }
    }

    [Serializable]
    public sealed class LayoutAutoGeneratorSettings
    {
        public float widthM = 20f;
        public float lengthM = 40f;
        public float zoneDepthM = 6f;
        public int shelfRows = 3;
        public int shelfColumns = 4;
        public int dockPairs = 1;
        public float shelfWidthM = 0.8f;
        public float shelfLengthM = 1.8f;
        public float shelfHeightM = 0.6f;
        public float dockWidthM = 2.4f;
        public float dockDepthM = 1.2f;
    }

    public static class LayoutAutoGenerator
    {
        public static WarehouseStructureLayout Generate(
            RunArtifactDto artifact,
            LayoutAutoGeneratorSettings settings = null)
        {
            return Generate(artifact?.warehouse_graph, settings, artifact?.layout?.resources?.Length ?? 0);
        }

        public static WarehouseStructureLayout Generate(
            RunArtifact artifact,
            LayoutAutoGeneratorSettings settings = null)
        {
            return Generate(null, settings, artifact?.Layout?.Resources?.Count ?? 0);
        }

        public static WarehouseStructureLayout Generate(
            WarehouseGraphDto graph,
            LayoutAutoGeneratorSettings settings = null)
        {
            return Generate(graph, settings, 0);
        }

        private static WarehouseStructureLayout Generate(
            WarehouseGraphDto graph,
            LayoutAutoGeneratorSettings settings,
            int resourceHintCount)
        {
            settings ??= new LayoutAutoGeneratorSettings();
            Validate(settings);

            var elements = new List<WarehouseStructureElement>();
            var halfLength = settings.lengthM * 0.5f;
            var zoneDepth = Mathf.Min(settings.zoneDepthM, settings.lengthM / 3f);
            var zoneSize = new Vector3(settings.widthM, 0.04f, zoneDepth);

            elements.Add(new WarehouseStructureElement(
                "ReceivingZone",
                WarehouseStructureElementKind.Zone,
                "receiving",
                new Vector3(0f, 0.02f, -halfLength + (zoneDepth * 0.5f)),
                zoneSize));
            elements.Add(new WarehouseStructureElement(
                "StorageZone",
                WarehouseStructureElementKind.Zone,
                "storage",
                Vector3.zero,
                new Vector3(settings.widthM, 0.04f, settings.lengthM - (zoneDepth * 2f))));
            elements.Add(new WarehouseStructureElement(
                "ShippingZone",
                WarehouseStructureElementKind.Zone,
                "shipping",
                new Vector3(0f, 0.02f, halfLength - (zoneDepth * 0.5f)),
                zoneSize));

            AddShelves(elements, settings, ShelfCount(graph, settings, resourceHintCount));
            AddDocks(elements, settings, DockPairCount(graph, settings));

            return new WarehouseStructureLayout(settings.widthM, settings.lengthM, elements);
        }

        private static void AddShelves(
            List<WarehouseStructureElement> elements,
            LayoutAutoGeneratorSettings settings,
            int shelfCount)
        {
            var rows = Mathf.Max(1, settings.shelfRows);
            var columns = Mathf.Max(1, settings.shelfColumns);
            var total = Mathf.Max(shelfCount, rows * columns);
            var storageLength = settings.lengthM - (Mathf.Min(settings.zoneDepthM, settings.lengthM / 3f) * 2f);
            var xStep = settings.widthM / (columns + 1);
            var zStep = storageLength / (rows + 1);
            var zStart = -storageLength * 0.5f;
            var shelfSize = new Vector3(settings.shelfWidthM, settings.shelfHeightM, settings.shelfLengthM);

            for (var i = 0; i < total; i++)
            {
                var row = (i / columns) + 1;
                var column = (i % columns) + 1;
                var x = (-settings.widthM * 0.5f) + (xStep * column);
                var z = zStart + (zStep * row);
                elements.Add(new WarehouseStructureElement(
                    $"Shelf_R{row:00}_C{column:00}",
                    WarehouseStructureElementKind.Shelf,
                    "shelf",
                    new Vector3(x, settings.shelfHeightM * 0.5f, z),
                    shelfSize,
                    row,
                    column));
            }
        }

        private static void AddDocks(
            List<WarehouseStructureElement> elements,
            LayoutAutoGeneratorSettings settings,
            int dockPairs)
        {
            var count = Mathf.Max(1, dockPairs);
            var xStep = settings.widthM / (count + 1);
            var receivingZ = (-settings.lengthM * 0.5f) + (settings.dockDepthM * 0.5f);
            var shippingZ = (settings.lengthM * 0.5f) - (settings.dockDepthM * 0.5f);
            var dockSize = new Vector3(settings.dockWidthM, 0.2f, settings.dockDepthM);

            for (var i = 0; i < count; i++)
            {
                var x = (-settings.widthM * 0.5f) + (xStep * (i + 1));
                elements.Add(new WarehouseStructureElement(
                    $"ReceivingDock_{i + 1:00}",
                    WarehouseStructureElementKind.Dock,
                    "receiving",
                    new Vector3(x, dockSize.y * 0.5f, receivingZ),
                    dockSize,
                    0,
                    i + 1));
                elements.Add(new WarehouseStructureElement(
                    $"ShippingDock_{i + 1:00}",
                    WarehouseStructureElementKind.Dock,
                    "shipping",
                    new Vector3(x, dockSize.y * 0.5f, shippingZ),
                    dockSize,
                    0,
                    i + 1));
            }
        }

        private static int ShelfCount(
            WarehouseGraphDto graph,
            LayoutAutoGeneratorSettings settings,
            int resourceHintCount)
        {
            var count = CountNodes(graph, "shelf") + CountNodes(graph, "rack");
            if (count > 0)
            {
                return count;
            }

            if (resourceHintCount > 0)
            {
                return Mathf.Max(resourceHintCount, settings.shelfRows * settings.shelfColumns);
            }

            return settings.shelfRows * settings.shelfColumns;
        }

        private static int DockPairCount(WarehouseGraphDto graph, LayoutAutoGeneratorSettings settings)
        {
            var dockNodes = CountNodes(graph, "dock");
            if (dockNodes <= 0)
            {
                return settings.dockPairs;
            }

            return Mathf.Max(settings.dockPairs, Mathf.CeilToInt(dockNodes / 2f));
        }

        private static int CountNodes(WarehouseGraphDto graph, string token)
        {
            if (graph?.nodes == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var node in graph.nodes)
            {
                if (node == null)
                {
                    continue;
                }

                var nodeType = node.node_type ?? string.Empty;
                var nodeId = node.node_id ?? string.Empty;
                if (nodeType.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    nodeId.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static void Validate(LayoutAutoGeneratorSettings settings)
        {
            if (settings.widthM <= 0f) throw new ArgumentOutOfRangeException(nameof(settings.widthM));
            if (settings.lengthM <= 0f) throw new ArgumentOutOfRangeException(nameof(settings.lengthM));
            if (settings.zoneDepthM <= 0f) throw new ArgumentOutOfRangeException(nameof(settings.zoneDepthM));
            if (settings.shelfRows <= 0) throw new ArgumentOutOfRangeException(nameof(settings.shelfRows));
            if (settings.shelfColumns <= 0) throw new ArgumentOutOfRangeException(nameof(settings.shelfColumns));
            if (settings.dockPairs <= 0) throw new ArgumentOutOfRangeException(nameof(settings.dockPairs));
        }
    }
}
