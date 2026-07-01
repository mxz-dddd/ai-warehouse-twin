using System.Collections.Generic;
using UnityEngine;

namespace AIWarehouseTwin.Rendering.Layout
{
    public enum WarehouseLayoutNodeCategory
    {
        Generic,
        Dock,
        Aisle,
        Shelf,
        Zone
    }

    public readonly struct WarehouseLayoutNodeRenderModel
    {
        public readonly string NodeId;
        public readonly string NodeType;
        public readonly WarehouseLayoutNodeCategory Category;
        public readonly float X;
        public readonly float Y;
        public readonly Vector2 LocalPosition;
        public readonly Color Color;
        public readonly float Size;

        public WarehouseLayoutNodeRenderModel(
            string nodeId,
            string nodeType,
            WarehouseLayoutNodeCategory category,
            float x,
            float y,
            Vector2 localPosition,
            Color color,
            float size)
        {
            NodeId = nodeId;
            NodeType = nodeType;
            Category = category;
            X = x;
            Y = y;
            LocalPosition = localPosition;
            Color = color;
            Size = size;
        }
    }

    public readonly struct WarehouseLayoutEdgeRenderModel
    {
        public readonly string EdgeId;
        public readonly string FromNodeId;
        public readonly string ToNodeId;
        public readonly double DistanceM;
        public readonly long TravelTimeMs;
        public readonly bool Bidirectional;
        public readonly Vector2 FromLocalPosition;
        public readonly Vector2 ToLocalPosition;
        public readonly Color Color;

        public WarehouseLayoutEdgeRenderModel(
            string edgeId,
            string fromNodeId,
            string toNodeId,
            double distanceM,
            long travelTimeMs,
            bool bidirectional,
            Vector2 fromLocalPosition,
            Vector2 toLocalPosition,
            Color color)
        {
            EdgeId = edgeId;
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            DistanceM = distanceM;
            TravelTimeMs = travelTimeMs;
            Bidirectional = bidirectional;
            FromLocalPosition = fromLocalPosition;
            ToLocalPosition = toLocalPosition;
            Color = color;
        }
    }

    public readonly struct WarehouseLayoutZoneRenderModel
    {
        public readonly string ZoneId;
        public readonly string ZoneType;
        public readonly Vector2 Center;
        public readonly Vector2 Size;
        public readonly Color FillColor;
        public readonly Color BorderColor;

        public WarehouseLayoutZoneRenderModel(
            string zoneId,
            string zoneType,
            Vector2 center,
            Vector2 size,
            Color fillColor,
            Color borderColor)
        {
            ZoneId = zoneId;
            ZoneType = zoneType;
            Center = center;
            Size = size;
            FillColor = fillColor;
            BorderColor = borderColor;
        }
    }

    public sealed class WarehouseLayoutRenderModel
    {
        public static readonly WarehouseLayoutRenderModel Empty =
            new WarehouseLayoutRenderModel(
                new List<WarehouseLayoutNodeRenderModel>(),
                new List<WarehouseLayoutEdgeRenderModel>(),
                new List<WarehouseLayoutZoneRenderModel>(),
                0);

        public IReadOnlyList<WarehouseLayoutNodeRenderModel> Nodes { get; }
        public IReadOnlyList<WarehouseLayoutEdgeRenderModel> Edges { get; }
        public IReadOnlyList<WarehouseLayoutZoneRenderModel> Zones { get; }
        public int SkippedEdgeCount { get; }

        public bool IsEmpty => Nodes.Count == 0;

        public WarehouseLayoutRenderModel(
            IReadOnlyList<WarehouseLayoutNodeRenderModel> nodes,
            IReadOnlyList<WarehouseLayoutEdgeRenderModel> edges,
            IReadOnlyList<WarehouseLayoutZoneRenderModel> zones,
            int skippedEdgeCount)
        {
            Nodes = nodes ?? new List<WarehouseLayoutNodeRenderModel>();
            Edges = edges ?? new List<WarehouseLayoutEdgeRenderModel>();
            Zones = zones ?? new List<WarehouseLayoutZoneRenderModel>();
            SkippedEdgeCount = skippedEdgeCount;
        }
    }
}
