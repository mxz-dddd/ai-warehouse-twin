using System;

namespace AIWarehouseTwin.Graph
{
    // Data-transfer shapes for the layout.json path network fields.
    // Field names match the on-disk JSON so UnityEngine.JsonUtility can bind.
    [Serializable]
    public sealed class LayoutDocumentDto
    {
        public LayoutPathNodeDto[] path_nodes = Array.Empty<LayoutPathNodeDto>();
        public LayoutPathEdgeDto[] path_edges = Array.Empty<LayoutPathEdgeDto>();
    }

    [Serializable]
    public sealed class LayoutPathNodeDto
    {
        public string node_id = string.Empty;
        public string node_type = string.Empty;
        public double x;
        public double y;
    }

    [Serializable]
    public sealed class LayoutPathEdgeDto
    {
        public string edge_id = string.Empty;
        public string from_node_id = string.Empty;
        public string to_node_id = string.Empty;
        public double distance_mm;
        public bool bidirectional;
    }
}
