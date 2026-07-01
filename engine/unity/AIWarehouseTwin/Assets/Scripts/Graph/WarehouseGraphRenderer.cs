using System.Collections.Generic;

namespace AIWarehouseTwin.Graph
{
    // A graph node projected into a [0,1] x [0,1] normalized layout box.
    public readonly struct GraphNodeLayout
    {
        public readonly string NodeId;
        public readonly string NodeType;
        public readonly float Nx;
        public readonly float Ny;

        public GraphNodeLayout(string nodeId, string nodeType, float nx, float ny)
        {
            NodeId = nodeId;
            NodeType = nodeType;
            Nx = nx;
            Ny = ny;
        }
    }

    // An edge projected as a normalized segment between two node positions.
    public readonly struct GraphEdgeLayout
    {
        public readonly string EdgeId;
        public readonly float FromNx;
        public readonly float FromNy;
        public readonly float ToNx;
        public readonly float ToNy;

        public GraphEdgeLayout(string edgeId, float fromNx, float fromNy, float toNx, float toNy)
        {
            EdgeId = edgeId;
            FromNx = fromNx;
            FromNy = fromNy;
            ToNx = toNx;
            ToNy = toNy;
        }
    }

    // Pure-C# projection of a WarehouseGraph into normalized coordinates.
    // No UnityEngine dependency so it can be exercised in EditMode tests; the
    // view layer maps [0,1] into pixel space with padding.
    public static class WarehouseGraphRenderer
    {
        public static IReadOnlyList<GraphNodeLayout> BuildNodeLayout(WarehouseGraph graph)
        {
            var result = new List<GraphNodeLayout>();
            if (graph == null || graph.Nodes.Count == 0) return result;

            ComputeBounds(graph, out float minX, out float minY, out float rangeX, out float rangeY);

            foreach (var n in graph.Nodes)
            {
                var (nx, ny) = Normalize(n.X, n.Y, minX, minY, rangeX, rangeY);
                result.Add(new GraphNodeLayout(n.NodeId, n.NodeType, nx, ny));
            }

            return result;
        }

        // Projects each edge to a normalized segment. Edges referencing an
        // absent node (dangling) are skipped rather than throwing.
        public static IReadOnlyList<GraphEdgeLayout> BuildEdgeLayout(WarehouseGraph graph)
        {
            var result = new List<GraphEdgeLayout>();
            if (graph == null || graph.Nodes.Count == 0 || graph.Edges.Count == 0) return result;

            ComputeBounds(graph, out float minX, out float minY, out float rangeX, out float rangeY);

            foreach (var e in graph.Edges)
            {
                if (!graph.TryGetNode(e.FromNodeId, out var from)) continue;
                if (!graph.TryGetNode(e.ToNodeId, out var to)) continue;

                var (fx, fy) = Normalize(from.X, from.Y, minX, minY, rangeX, rangeY);
                var (tx, ty) = Normalize(to.X, to.Y, minX, minY, rangeX, rangeY);
                result.Add(new GraphEdgeLayout(e.EdgeId, fx, fy, tx, ty));
            }

            return result;
        }

        // Maps raw coordinate space to [0,1] relative to bounding box.
        // Returns 0.5 on a degenerate (zero-range) axis so a single point
        // or a collinear graph is centered rather than divided by zero.
        public static (float nx, float ny) Normalize(
            float x, float y, float minX, float minY, float rangeX, float rangeY)
        {
            float nx = rangeX > 0 ? (x - minX) / rangeX : 0.5f;
            float ny = rangeY > 0 ? (y - minY) / rangeY : 0.5f;
            return (nx, ny);
        }

        private static void ComputeBounds(
            WarehouseGraph graph, out float minX, out float minY, out float rangeX, out float rangeY)
        {
            minX = float.MaxValue;
            minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var n in graph.Nodes)
            {
                if (n.X < minX) minX = n.X;
                if (n.Y < minY) minY = n.Y;
                if (n.X > maxX) maxX = n.X;
                if (n.Y > maxY) maxY = n.Y;
            }

            rangeX = maxX - minX;
            rangeY = maxY - minY;
        }
    }
}
