using System;
using System.Collections.Generic;
using System.IO;

namespace AIWarehouseTwin.Artifact
{
    public static class ComparisonArtifactLoader
    {
        public const string SupportedSchemaVersion = "comparison_artifact.v1";
        public const string SupportedR3SchemaVersion = "comparison_artifact.v1.r3";

        public static ComparisonArtifactDto LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("ComparisonArtifact path cannot be empty.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("ComparisonArtifact file was not found.", path);
            }

            return LoadFromJson(File.ReadAllText(path));
        }

        public static ComparisonArtifactDto LoadFromJson(string json)
        {
            var root = ArtifactJson.ParseObject(json, nameof(ComparisonArtifactDto));
            var artifact = MapComparisonArtifact(root);
            Normalize(artifact);
            Validate(artifact);
            return artifact;
        }

        private static ComparisonArtifactDto MapComparisonArtifact(Dictionary<string, object> root)
        {
            return new ComparisonArtifactDto
            {
                schema_version = ArtifactJson.GetString(root, "schema_version"),
                baseline = MapScenarioSummary(ArtifactJson.GetObject(root, "baseline")),
                candidate = MapScenarioSummary(ArtifactJson.GetObject(root, "candidate")),
                deltas = ArtifactJson.MapArray(ArtifactJson.GetArray(root, "deltas"), MapDelta),
                kpi_deltas = MapKpiDeltas(ArtifactJson.GetObject(root, "kpi_deltas")),
                improvement_pct = ArtifactJson.ToDoubleMap(ArtifactJson.GetObject(root, "improvement_pct"))
            };
        }

        private static ComparisonScenarioSummaryDto MapScenarioSummary(Dictionary<string, object> summary)
        {
            if (summary == null)
            {
                return new ComparisonScenarioSummaryDto();
            }

            return new ComparisonScenarioSummaryDto
            {
                scenario_id = ArtifactJson.GetString(summary, "scenario_id"),
                metrics = MapMetrics(ArtifactJson.GetObject(summary, "metrics"))
            };
        }

        private static ComparisonMetricsDto MapMetrics(Dictionary<string, object> metrics)
        {
            if (metrics == null)
            {
                return new ComparisonMetricsDto();
            }

            return new ComparisonMetricsDto
            {
                finished_at_ms = ArtifactJson.GetLong(metrics, "finished_at_ms"),
                completed_receipts = ArtifactJson.GetInt(metrics, "completed_receipts"),
                completed_outbound_orders = ArtifactJson.GetInt(metrics, "completed_outbound_orders"),
                completed_each_pick_orders = ArtifactJson.GetInt(metrics, "completed_each_pick_orders"),
                total_quantity_received = ArtifactJson.GetDouble(metrics, "total_quantity_received"),
                total_quantity_shipped = ArtifactJson.GetDouble(metrics, "total_quantity_shipped"),
                total_quantity_picked = ArtifactJson.GetDouble(metrics, "total_quantity_picked"),
                inbound_receipt_throughput_per_hour =
                    ArtifactJson.GetDouble(metrics, "inbound_receipt_throughput_per_hour"),
                outbound_order_throughput_per_hour =
                    ArtifactJson.GetDouble(metrics, "outbound_order_throughput_per_hour"),
                each_pick_order_throughput_per_hour =
                    ArtifactJson.GetDouble(metrics, "each_pick_order_throughput_per_hour"),
                total_work_item_throughput_per_hour =
                    ArtifactJson.GetDouble(metrics, "total_work_item_throughput_per_hour")
            };
        }

        private static ComparisonDeltaDto MapDelta(Dictionary<string, object> delta)
        {
            return new ComparisonDeltaDto
            {
                metric_name = ArtifactJson.GetString(delta, "metric_name"),
                baseline_value = ArtifactJson.GetDouble(delta, "baseline_value"),
                candidate_value = ArtifactJson.GetDouble(delta, "candidate_value"),
                delta = ArtifactJson.GetDouble(delta, "delta"),
                delta_percent = ArtifactJson.GetNullableDouble(delta, "delta_percent"),
                direction = ArtifactJson.GetString(delta, "direction")
            };
        }

        private static Dictionary<string, ComparisonKpiDeltaDto> MapKpiDeltas(Dictionary<string, object> obj)
        {
            var result = new Dictionary<string, ComparisonKpiDeltaDto>();
            if (obj == null)
            {
                return result;
            }

            foreach (var entry in obj)
            {
                if (entry.Value is Dictionary<string, object> value)
                {
                    result[entry.Key] = new ComparisonKpiDeltaDto
                    {
                        baseline_value = ArtifactJson.GetDouble(value, "baseline_value"),
                        candidate_value = ArtifactJson.GetDouble(value, "candidate_value"),
                        delta = ArtifactJson.GetDouble(value, "delta"),
                        lower_is_better = ArtifactJson.GetBool(value, "lower_is_better")
                    };
                }
            }

            return result;
        }

        private static void Normalize(ComparisonArtifactDto artifact)
        {
            artifact.baseline ??= new ComparisonScenarioSummaryDto();
            artifact.baseline.metrics ??= new ComparisonMetricsDto();
            artifact.candidate ??= new ComparisonScenarioSummaryDto();
            artifact.candidate.metrics ??= new ComparisonMetricsDto();
            artifact.deltas ??= Array.Empty<ComparisonDeltaDto>();
            artifact.kpi_deltas ??= new Dictionary<string, ComparisonKpiDeltaDto>();
            artifact.improvement_pct ??= new Dictionary<string, double>();
        }

        private static void Validate(ComparisonArtifactDto artifact)
        {
            if (artifact.schema_version != SupportedSchemaVersion &&
                artifact.schema_version != SupportedR3SchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported ComparisonArtifact schema_version '{artifact.schema_version}'.");
            }
        }
    }
}
