using System;
using System.Collections.Generic;
using AIWarehouseTwin.Artifact;
using UnityEngine;

namespace AIWarehouseTwin.Rendering.Layout
{
    public static class WarehouseLayoutRenderModelBuilder
    {
        private static readonly StringComparer Ordinal = StringComparer.Ordinal;

        public static WarehouseLayoutRenderModel Build(
            RunArtifactDto artifact,
            WarehouseLayoutCoordinateMapper mapper = null,
            WarehousePalette palette = null)
        {
            return Build(artifact?.warehouse_graph, mapper, palette);
        }

        public static WarehouseLayoutRenderModel Build(
            WarehouseGraphDto graph,
            WarehouseLayoutCoordinateMapper mapper = null,
            WarehousePalette palette = null)
        {
            if (graph?.nodes == null || graph.nodes.Length == 0)
            {
                return WarehouseLayoutRenderModel.Empty;
            }

            mapper ??= new WarehouseLayoutCoordinateMapper(Vector2.one, Vector2.zero);

            var nodes = BuildNodes(graph.nodes, mapper, palette, out var positions);
            var zones = BuildZones(nodes, palette);
            var edges = BuildEdges(graph.edges, positions, palette, out var skippedEdgeCount);
            return new WarehouseLayoutRenderModel(nodes, edges, zones, skippedEdgeCount);
        }

        public static WarehouseLayoutNodeCategory ClassifyNode(string nodeType)
        {
            var normalized = (nodeType ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized.Contains("dock")) return WarehouseLayoutNodeCategory.Dock;
            if (normalized.Contains("aisle")) return WarehouseLayoutNodeCategory.Aisle;
            if (normalized.Contains("shelf") || normalized.Contains("rack")) return WarehouseLayoutNodeCategory.Shelf;
            if (normalized.Contains("zone")) return WarehouseLayoutNodeCategory.Zone;
            return WarehouseLayoutNodeCategory.Generic;
        }

        private static IReadOnlyList<WarehouseLayoutNodeRenderModel> BuildNodes(
            WarehouseGraphNodeDto[] sourceNodes,
            WarehouseLayoutCoordinateMapper mapper,
            WarehousePalette palette,
            out Dictionary<string, Vector2> positions)
        {
            var validNodes = new List<WarehouseGraphNodeDto>();
            foreach (var node in sourceNodes)
            {
                if (!string.IsNullOrWhiteSpace(node?.node_id))
                {
                    validNodes.Add(node);
                }
            }

            validNodes.Sort(CompareNodes);
            var bounds = ComputeBounds(validNodes);
            var nodes = new List<WarehouseLayoutNodeRenderModel>(validNodes.Count);
            positions = new Dictionary<string, Vector2>(StringComparer.Ordinal);

            foreach (var node in validNodes)
            {
                var category = ClassifyNode(node.node_type);
                var localPosition = mapper.Map((float)node.x, (float)node.y, bounds);
                nodes.Add(new WarehouseLayoutNodeRenderModel(
                    node.node_id,
                    node.node_type ?? string.Empty,
                    category,
                    (float)node.x,
                    (float)node.y,
                    localPosition,
                    NodeColorForCategory(category, palette),
                    SizeForCategory(category)));
                positions[node.node_id] = localPosition;
            }

            return nodes;
        }

        private static IReadOnlyList<WarehouseLayoutEdgeRenderModel> BuildEdges(
            WarehouseGraphEdgeDto[] sourceEdges,
            IReadOnlyDictionary<string, Vector2> positions,
            WarehousePalette palette,
            out int skippedEdgeCount)
        {
            var edges = new List<WarehouseGraphEdgeDto>();
            if (sourceEdges != null)
            {
                foreach (var edge in sourceEdges)
                {
                    if (edge != null)
                    {
                        edges.Add(edge);
                    }
                }
            }

            edges.Sort(CompareEdges);
            var result = new List<WarehouseLayoutEdgeRenderModel>();
            skippedEdgeCount = 0;

            foreach (var edge in edges)
            {
                if (!positions.TryGetValue(edge.from_node_id ?? string.Empty, out var from) ||
                    !positions.TryGetValue(edge.to_node_id ?? string.Empty, out var to))
                {
                    skippedEdgeCount++;
                    continue;
                }

                result.Add(new WarehouseLayoutEdgeRenderModel(
                    edge.edge_id ?? string.Empty,
                    edge.from_node_id ?? string.Empty,
                    edge.to_node_id ?? string.Empty,
                    edge.distance_m,
                    edge.travel_time_ms,
                    edge.bidirectional,
                    from,
                    to,
                    palette != null ? palette.EdgeColor : WarehousePalette.DefaultEdgeColor));
            }

            return result;
        }

        private static IReadOnlyList<WarehouseLayoutZoneRenderModel> BuildZones(
            IReadOnlyList<WarehouseLayoutNodeRenderModel> nodes,
            WarehousePalette palette)
        {
            var zones = new List<WarehouseLayoutZoneRenderModel>();
            foreach (var node in nodes)
            {
                if (node.Category != WarehouseLayoutNodeCategory.Zone)
                {
                    continue;
                }

                zones.Add(new WarehouseLayoutZoneRenderModel(
                    node.NodeId,
                    node.NodeType,
                    node.LocalPosition,
                    new Vector2(0.18f, 0.18f),
                    palette != null ? palette.ZoneFillColor : WarehousePalette.DefaultZoneFillColor,
                    palette != null ? palette.ZoneBorderColor : WarehousePalette.DefaultZoneBorderColor));
            }

            return zones;
        }

        private static WarehouseLayoutBounds ComputeBounds(IReadOnlyList<WarehouseGraphNodeDto> nodes)
        {
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var node in nodes)
            {
                var x = (float)node.x;
                var y = (float)node.y;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

            return new WarehouseLayoutBounds(minX, minY, maxX - minX, maxY - minY);
        }

        private static Color NodeColorForCategory(
            WarehouseLayoutNodeCategory category,
            WarehousePalette palette)
        {
            return palette != null
                ? palette.NodeColorFor(category)
                : WarehousePalette.DefaultNodeColorFor(category);
        }

        private static float SizeForCategory(WarehouseLayoutNodeCategory category)
        {
            return category switch
            {
                WarehouseLayoutNodeCategory.Dock => 0.08f,
                WarehouseLayoutNodeCategory.Aisle => 0.055f,
                WarehouseLayoutNodeCategory.Shelf => 0.065f,
                WarehouseLayoutNodeCategory.Zone => 0.1f,
                _ => 0.05f
            };
        }

        private static int CompareNodes(WarehouseGraphNodeDto left, WarehouseGraphNodeDto right)
        {
            var idCompare = Ordinal.Compare(left.node_id ?? string.Empty, right.node_id ?? string.Empty);
            if (idCompare != 0) return idCompare;
            var typeCompare = Ordinal.Compare(left.node_type ?? string.Empty, right.node_type ?? string.Empty);
            if (typeCompare != 0) return typeCompare;
            var xCompare = left.x.CompareTo(right.x);
            return xCompare != 0 ? xCompare : left.y.CompareTo(right.y);
        }

        private static int CompareEdges(WarehouseGraphEdgeDto left, WarehouseGraphEdgeDto right)
        {
            var idCompare = Ordinal.Compare(left.edge_id ?? string.Empty, right.edge_id ?? string.Empty);
            if (idCompare != 0) return idCompare;
            var fromCompare = Ordinal.Compare(left.from_node_id ?? string.Empty, right.from_node_id ?? string.Empty);
            if (fromCompare != 0) return fromCompare;
            return Ordinal.Compare(left.to_node_id ?? string.Empty, right.to_node_id ?? string.Empty);
        }
    }
}
