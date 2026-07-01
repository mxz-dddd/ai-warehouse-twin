using System;

namespace AIWarehouseTwin.Artifact
{
    [Serializable]
    public sealed class MovementArtifactDto
    {
        public string schema_version = string.Empty;
        public string artifact_kind = string.Empty;
        public string scenario_id = string.Empty;
        public string run_id = string.Empty;
        public int seed;
        public string source_run_artifact = string.Empty;
        public WarehouseGraphDto warehouse_graph = new WarehouseGraphDto();
        public MovementActorDto[] actors = Array.Empty<MovementActorDto>();
        public MovementEventDto[] movement_events = Array.Empty<MovementEventDto>();
        public MovementRouteSegmentDto[] route_segments = Array.Empty<MovementRouteSegmentDto>();
        public MovementProvenanceDto provenance = new MovementProvenanceDto();
    }

    [Serializable]
    public sealed class MovementActorDto
    {
        public string actor_id = string.Empty;
        public string actor_type = string.Empty;
        public string resource_id = string.Empty;
        public string initial_node_id = string.Empty;
        public double capacity;
        public string load_state = string.Empty;
    }

    [Serializable]
    public sealed class MovementEventDto
    {
        public string event_id = string.Empty;
        public string actor_id = string.Empty;
        public string operation_id = string.Empty;
        public string event_type = string.Empty;
        public long at_ms;
        public string node_id = string.Empty;
        public double x;
        public double y;
        public string load_state = string.Empty;
        public string related_resource_id = string.Empty;
    }

    [Serializable]
    public sealed class MovementRouteSegmentDto
    {
        public string segment_id = string.Empty;
        public string actor_id = string.Empty;
        public string operation_id = string.Empty;
        public string from_node_id = string.Empty;
        public string to_node_id = string.Empty;
        public long start_ms;
        public long end_ms;
        public double distance_m;
        public string[] path_node_ids = Array.Empty<string>();
        public string[] edge_ids = Array.Empty<string>();
        public long travel_time_ms;
    }

    [Serializable]
    public sealed class MovementProvenanceDto
    {
        public string movement_generator_version = string.Empty;
        public string graph_source = string.Empty;
        public bool movement_enabled;
        public string deterministic_generation_policy = string.Empty;
    }
}
