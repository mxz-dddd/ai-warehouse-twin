using System;
using System.Collections.Generic;
using System.IO;

namespace AIWarehouseTwin.Artifact
{
    public static class RunArtifactLoader
    {
        public const string SupportedSchemaVersion = "run-artifact.v1";
        public const string SupportedR3SchemaVersion = "run-artifact.v1.r3";
        public const string SupportedArtifactKind = "warehouse-simulation-run";

        public static RunArtifactDto LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("RunArtifact path cannot be empty.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("RunArtifact file was not found.", path);
            }

            return LoadFromJson(File.ReadAllText(path));
        }

        public static RunArtifactDto LoadFromJson(string json)
        {
            var root = ArtifactJson.ParseObject(json, nameof(RunArtifactDto));
            var artifact = MapRunArtifact(root);
            Normalize(artifact);
            Validate(artifact);
            return artifact;
        }

        private static RunArtifactDto MapRunArtifact(Dictionary<string, object> root)
        {
            return new RunArtifactDto
            {
                schema_version = ArtifactJson.GetString(root, "schema_version"),
                artifact_kind = ArtifactJson.GetString(root, "artifact_kind"),
                scenario_id = ArtifactJson.GetString(root, "scenario_id"),
                seed = ArtifactJson.GetInt(root, "seed"),
                started_at_ms = ArtifactJson.GetLong(root, "started_at_ms"),
                finished_at_ms = ArtifactJson.GetLong(root, "finished_at_ms"),
                final_world_time_ms = ArtifactJson.GetLong(root, "final_world_time_ms"),
                kpi_summary = MapKpiSummary(ArtifactJson.GetObject(root, "kpi_summary")),
                layout = MapLayout(ArtifactJson.GetObject(root, "layout")),
                warehouse_graph = ArtifactJson.MapWarehouseGraph(ArtifactJson.GetObject(root, "warehouse_graph")),
                position_timeline = ArtifactJson.MapArray(
                    ArtifactJson.GetArray(root, "position_timeline"),
                    MapPositionTimelineEntry),
                event_log = ArtifactJson.ToStringArray(ArtifactJson.GetArray(root, "event_log"))
            };
        }

        private static RunArtifactKpiSummaryDto MapKpiSummary(Dictionary<string, object> kpi)
        {
            if (kpi == null)
            {
                return new RunArtifactKpiSummaryDto();
            }

            return new RunArtifactKpiSummaryDto
            {
                total_duration_ms = ArtifactJson.GetLong(kpi, "total_duration_ms"),
                total_completed_work_items = ArtifactJson.GetInt(kpi, "total_completed_work_items"),
                event_log_line_count = ArtifactJson.GetInt(kpi, "event_log_line_count"),
                receipt_throughput_per_hour = ArtifactJson.GetDouble(kpi, "receipt_throughput_per_hour"),
                outbound_order_throughput_per_hour = ArtifactJson.GetDouble(kpi, "outbound_order_throughput_per_hour"),
                each_pick_order_throughput_per_hour =
                    ArtifactJson.GetDouble(kpi, "each_pick_order_throughput_per_hour"),
                total_work_item_throughput_per_hour =
                    ArtifactJson.GetDouble(kpi, "total_work_item_throughput_per_hour"),
                order_cycle_p50_ms = ArtifactJson.GetNullableDouble(kpi, "order_cycle_p50_ms"),
                order_cycle_p90_ms = ArtifactJson.GetNullableDouble(kpi, "order_cycle_p90_ms"),
                order_cycle_p95_ms = ArtifactJson.GetNullableDouble(kpi, "order_cycle_p95_ms"),
                avg_wait_ms = ArtifactJson.GetNullableDouble(kpi, "avg_wait_ms"),
                resource_utilization =
                    ArtifactJson.ToDoubleMap(ArtifactJson.GetObject(kpi, "resource_utilization")),
                bottlenecks = ArtifactJson.MapArray(ArtifactJson.GetArray(kpi, "bottlenecks"), MapBottleneck),
                travel_distance_m_by_actor_type =
                    ArtifactJson.ToDoubleMap(ArtifactJson.GetObject(kpi, "travel_distance_m_by_actor_type"))
            };
        }

        private static RunArtifactBottleneckDto MapBottleneck(Dictionary<string, object> bottleneck)
        {
            return new RunArtifactBottleneckDto
            {
                rank = ArtifactJson.GetInt(bottleneck, "rank"),
                resource_id = ArtifactJson.GetString(bottleneck, "resource_id"),
                resource_type = ArtifactJson.GetString(bottleneck, "resource_type"),
                avg_wait_ms = ArtifactJson.GetDouble(bottleneck, "avg_wait_ms"),
                total_wait_ms = ArtifactJson.GetDouble(bottleneck, "total_wait_ms"),
                utilization = ArtifactJson.GetDouble(bottleneck, "utilization")
            };
        }

        private static RunArtifactLayoutDto MapLayout(Dictionary<string, object> layout)
        {
            return new RunArtifactLayoutDto
            {
                resources = ArtifactJson.MapArray(
                    ArtifactJson.GetArray(layout ?? new Dictionary<string, object>(), "resources"),
                    resource => new RunArtifactLayoutResourceDto
                    {
                        resource_id = ArtifactJson.GetString(resource, "resource_id"),
                        node_id = ArtifactJson.GetString(resource, "node_id"),
                        x = ArtifactJson.GetDouble(resource, "x"),
                        y = ArtifactJson.GetDouble(resource, "y")
                    })
            };
        }

        private static RunArtifactPositionTimelineEntryDto MapPositionTimelineEntry(
            Dictionary<string, object> entry)
        {
            return new RunArtifactPositionTimelineEntryDto
            {
                operation_id = ArtifactJson.GetString(entry, "operation_id"),
                operation_type = ArtifactJson.GetString(entry, "operation_type"),
                stage_type = ArtifactJson.GetString(entry, "stage_type"),
                resource_id = ArtifactJson.GetString(entry, "resource_id"),
                at_ms = ArtifactJson.GetLong(entry, "at_ms"),
                event_type = ArtifactJson.GetString(entry, "event_type"),
                node_id = ArtifactJson.GetString(entry, "node_id"),
                x = ArtifactJson.GetDouble(entry, "x"),
                y = ArtifactJson.GetDouble(entry, "y")
            };
        }

        private static void Normalize(RunArtifactDto artifact)
        {
            artifact.kpi_summary ??= new RunArtifactKpiSummaryDto();
            artifact.kpi_summary.resource_utilization ??= new Dictionary<string, double>();
            artifact.kpi_summary.bottlenecks ??= Array.Empty<RunArtifactBottleneckDto>();
            artifact.kpi_summary.travel_distance_m_by_actor_type ??= new Dictionary<string, double>();
            artifact.layout ??= new RunArtifactLayoutDto();
            artifact.layout.resources ??= Array.Empty<RunArtifactLayoutResourceDto>();
            artifact.warehouse_graph ??= new WarehouseGraphDto();
            artifact.warehouse_graph.nodes ??= Array.Empty<WarehouseGraphNodeDto>();
            artifact.warehouse_graph.edges ??= Array.Empty<WarehouseGraphEdgeDto>();
            artifact.position_timeline ??= Array.Empty<RunArtifactPositionTimelineEntryDto>();
            artifact.event_log ??= Array.Empty<string>();
        }

        private static void Validate(RunArtifactDto artifact)
        {
            if (artifact.schema_version != SupportedSchemaVersion &&
                artifact.schema_version != SupportedR3SchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported RunArtifact schema_version '{artifact.schema_version}'.");
            }

            if (artifact.artifact_kind != SupportedArtifactKind)
            {
                throw new InvalidOperationException(
                    $"Unsupported RunArtifact artifact_kind '{artifact.artifact_kind}'.");
            }

            if (string.IsNullOrWhiteSpace(artifact.scenario_id))
            {
                throw new InvalidOperationException("RunArtifact scenario_id cannot be empty.");
            }

            if (artifact.kpi_summary.event_log_line_count != artifact.event_log.Length)
            {
                throw new InvalidOperationException(
                    "RunArtifact KPI event_log_line_count must match event_log length.");
            }

            foreach (var entry in artifact.position_timeline)
            {
                if (entry.at_ms < 0)
                {
                    throw new InvalidOperationException("RunArtifact position timeline time cannot be negative.");
                }

                if (entry.event_type != "start" && entry.event_type != "finish")
                {
                    throw new InvalidOperationException(
                        $"RunArtifact position timeline event_type must be start or finish: '{entry.event_type}'.");
                }
            }
        }
    }
}
