using System;

namespace AIWarehouseTwin.Artifact
{
    [Serializable]
    public sealed class RunArtifactDto
    {
        public string schema_version = string.Empty;
        public string artifact_kind = string.Empty;
        public string scenario_id = string.Empty;
        public int seed;
        public long started_at_ms;
        public long finished_at_ms;
        public long final_world_time_ms;
        public RunArtifactKpiSummaryDto kpi_summary = new RunArtifactKpiSummaryDto();
        public RunArtifactLayoutDto layout = new RunArtifactLayoutDto();
        public RunArtifactPositionTimelineEntryDto[] position_timeline =
            Array.Empty<RunArtifactPositionTimelineEntryDto>();
        public string[] event_log = Array.Empty<string>();
    }

    [Serializable]
    public sealed class RunArtifactKpiSummaryDto
    {
        public long total_duration_ms;
        public int total_completed_work_items;
        public int event_log_line_count;
        public double receipt_throughput_per_hour;
        public double outbound_order_throughput_per_hour;
        public double each_pick_order_throughput_per_hour;
        public double total_work_item_throughput_per_hour;
    }

    [Serializable]
    public sealed class RunArtifactLayoutDto
    {
        public RunArtifactLayoutResourceDto[] resources = Array.Empty<RunArtifactLayoutResourceDto>();
    }

    [Serializable]
    public sealed class RunArtifactLayoutResourceDto
    {
        public string resource_id = string.Empty;
        public string node_id = string.Empty;
        public double x;
        public double y;
    }

    [Serializable]
    public sealed class RunArtifactPositionTimelineEntryDto
    {
        public string operation_id = string.Empty;
        public string operation_type = string.Empty;
        public string stage_type = string.Empty;
        public string resource_id = string.Empty;
        public long at_ms;
        public string event_type = string.Empty;
        public string node_id = string.Empty;
        public double x;
        public double y;
    }
}
