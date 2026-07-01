using System.Collections.Generic;

namespace AIWarehouseTwin.Graph
{
    // Frozen static path-network abstraction consumed by the layout renderer.
    // Shape mirrors the warehouse_graph nodes/edges seam so a future
    // artifact-backed producer can be swapped in behind the source adapter
    // without touching the rendering layer.
    public readonly struct WarehouseGraphNode
    {
        public readonly string NodeId;
        public readonly string NodeType;
        public readonly float X;
        public readonly float Y;

        public WarehouseGraphNode(string nodeId, string nodeType, float x, float y)
        {
            NodeId = nodeId;
            NodeType = nodeType;
            X = x;
            Y = y;
        }
    }

    public readonly struct WarehouseGraphEdge
    {
        public readonly string EdgeId;
        public readonly string FromNodeId;
        public readonly string ToNodeId;
        public readonly bool Bidirectional;

        public WarehouseGraphEdge(string edgeId, string fromNodeId, string toNodeId, bool bidirectional)
        {
            EdgeId = edgeId;
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            Bidirectional = bidirectional;
        }
    }

    public sealed class WarehouseGraph
    {
        public IReadOnlyList<WarehouseGraphNode> Nodes { get; }
        public IReadOnlyList<WarehouseGraphEdge> Edges { get; }

        public WarehouseGraph(
            IReadOnlyList<WarehouseGraphNode> nodes,
            IReadOnlyList<WarehouseGraphEdge> edges)
        {
            Nodes = nodes ?? new List<WarehouseGraphNode>();
            Edges = edges ?? new List<WarehouseGraphEdge>();
        }

        // Resolves an edge endpoint to its node coordinates.
        // Returns false when the referenced node is absent (dangling edge).
        public bool TryGetNode(string nodeId, out WarehouseGraphNode node)
        {
            foreach (var n in Nodes)
            {
                if (n.NodeId == nodeId)
                {
                    node = n;
                    return true;
                }
            }

            node = default;
            return false;
        }
    }
}
