using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AIWarehouseTwin.Artifact;

namespace AIWarehouseTwin.UI
{
    public static class KpiSummaryFormatter
    {
        public static string[] Format(RunArtifactDto artifact)
        {
            var kpi = artifact.kpi_summary;
            return new[]
            {
                $"Scenario: {artifact.scenario_id}",
                $"Seed: {artifact.seed.ToString(CultureInfo.InvariantCulture)}",
                $"Simulation time: {artifact.started_at_ms.ToString(CultureInfo.InvariantCulture)}-{artifact.finished_at_ms.ToString(CultureInfo.InvariantCulture)} ms",
                $"Duration: {kpi.total_duration_ms.ToString(CultureInfo.InvariantCulture)} ms",
                $"Completed work items: {kpi.total_completed_work_items.ToString(CultureInfo.InvariantCulture)}",
                $"Event log lines: {kpi.event_log_line_count.ToString(CultureInfo.InvariantCulture)}",
                $"Receipt throughput/hour: {FormatRate(kpi.receipt_throughput_per_hour)}",
                $"Outbound throughput/hour: {FormatRate(kpi.outbound_order_throughput_per_hour)}",
                $"Each-pick throughput/hour: {FormatRate(kpi.each_pick_order_throughput_per_hour)}",
                $"Total throughput/hour: {FormatRate(kpi.total_work_item_throughput_per_hour)}"
            };
        }

        public static KpiSummaryRow[] FormatStructured(RunArtifactDto artifact)
        {
            if (artifact == null)
            {
                return Array.Empty<KpiSummaryRow>();
            }

            var kpi = artifact.kpi_summary ?? new RunArtifactKpiSummaryDto();
            var rows = new List<KpiSummaryRow>
            {
                new KpiSummaryRow("Overview", "Scenario", artifact.scenario_id),
                new KpiSummaryRow("Overview", "Seed", artifact.seed.ToString(CultureInfo.InvariantCulture)),
                new KpiSummaryRow(
                    "Overview",
                    "Simulation time",
                    $"{artifact.started_at_ms.ToString(CultureInfo.InvariantCulture)}-{artifact.finished_at_ms.ToString(CultureInfo.InvariantCulture)} ms"),
                new KpiSummaryRow("Overview", "Duration", FormatMilliseconds(kpi.total_duration_ms)),
                new KpiSummaryRow(
                    "Overview",
                    "Completed work items",
                    kpi.total_completed_work_items.ToString(CultureInfo.InvariantCulture)),
                new KpiSummaryRow(
                    "Overview",
                    "Event log lines",
                    kpi.event_log_line_count.ToString(CultureInfo.InvariantCulture)),
                new KpiSummaryRow("Throughput", "Receipt/hour", FormatRate(kpi.receipt_throughput_per_hour)),
                new KpiSummaryRow(
                    "Throughput",
                    "Outbound/hour",
                    FormatRate(kpi.outbound_order_throughput_per_hour)),
                new KpiSummaryRow(
                    "Throughput",
                    "Each-pick/hour",
                    FormatRate(kpi.each_pick_order_throughput_per_hour)),
                new KpiSummaryRow(
                    "Throughput",
                    "Total/hour",
                    FormatRate(kpi.total_work_item_throughput_per_hour)),
                new KpiSummaryRow("Cycle time", "Order cycle p50", FormatNullableMilliseconds(kpi.order_cycle_p50_ms)),
                new KpiSummaryRow("Cycle time", "Order cycle p90", FormatNullableMilliseconds(kpi.order_cycle_p90_ms)),
                new KpiSummaryRow("Cycle time", "Order cycle p95", FormatNullableMilliseconds(kpi.order_cycle_p95_ms)),
                new KpiSummaryRow("Cycle time", "Average wait", FormatNullableMilliseconds(kpi.avg_wait_ms))
            };

            foreach (var entry in OrderedEntries(kpi.resource_utilization))
            {
                rows.Add(new KpiSummaryRow("Resource utilization", entry.Key, FormatPercent(entry.Value)));
            }

            foreach (var bottleneck in OrderedBottlenecks(kpi.bottlenecks))
            {
                var label = string.IsNullOrWhiteSpace(bottleneck.resource_type)
                    ? bottleneck.resource_id
                    : $"{bottleneck.resource_id} ({bottleneck.resource_type})";
                rows.Add(new KpiSummaryRow(
                    "Bottlenecks",
                    $"#{bottleneck.rank.ToString(CultureInfo.InvariantCulture)} {label}",
                    $"{FormatMilliseconds(bottleneck.avg_wait_ms)} avg wait, {FormatPercent(bottleneck.utilization)} util"));
            }

            foreach (var entry in OrderedEntries(kpi.travel_distance_m_by_actor_type))
            {
                rows.Add(new KpiSummaryRow("Actor distance (artifact)", entry.Key, $"{FormatNumber(entry.Value)} m"));
            }

            return rows.ToArray();
        }

        private static string FormatRate(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatNullableMilliseconds(double? value) =>
            value.HasValue ? FormatMilliseconds(value.Value) : "N/A";

        private static string FormatMilliseconds(double value) =>
            $"{FormatNumber(value)} ms";

        private static string FormatNumber(double value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string FormatPercent(double value) =>
            $"{FormatNumber(value)}%";

        private static IEnumerable<KeyValuePair<string, double>> OrderedEntries(
            IDictionary<string, double> entries)
        {
            if (entries == null)
            {
                return Array.Empty<KeyValuePair<string, double>>();
            }

            return entries.OrderBy(entry => entry.Key, StringComparer.Ordinal);
        }

        private static IEnumerable<RunArtifactBottleneckDto> OrderedBottlenecks(
            IEnumerable<RunArtifactBottleneckDto> bottlenecks)
        {
            if (bottlenecks == null)
            {
                return Array.Empty<RunArtifactBottleneckDto>();
            }

            return bottlenecks
                .Where(bottleneck => bottleneck != null)
                .OrderBy(bottleneck => bottleneck.rank)
                .ThenBy(bottleneck => bottleneck.resource_id, StringComparer.Ordinal);
        }
    }

    public sealed class KpiSummaryRow
    {
        public KpiSummaryRow(string section, string label, string value)
        {
            Section = section ?? string.Empty;
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string Section { get; }

        public string Label { get; }

        public string Value { get; }

        public string DisplayText => $"{Label}: {Value}";
    }
}
