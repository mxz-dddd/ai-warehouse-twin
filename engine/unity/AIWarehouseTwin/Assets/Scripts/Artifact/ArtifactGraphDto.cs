using System;

namespace AIWarehouseTwin.Artifact
{
    [Serializable]
    public sealed class WarehouseGraphDto
    {
        public WarehouseGraphNodeDto[] nodes = Array.Empty<WarehouseGraphNodeDto>();
        public WarehouseGraphEdgeDto[] edges = Array.Empty<WarehouseGraphEdgeDto>();
    }

    [Serializable]
    public sealed class WarehouseGraphNodeDto
    {
        public string node_id = string.Empty;
        public string node_type = string.Empty;
        public double x;
        public double y;
    }

    [Serializable]
    public sealed class WarehouseGraphEdgeDto
    {
        public string edge_id = string.Empty;
        public string from_node_id = string.Empty;
        public string to_node_id = string.Empty;
        public double distance_m;
        public long travel_time_ms;
        public bool bidirectional;
    }
}
