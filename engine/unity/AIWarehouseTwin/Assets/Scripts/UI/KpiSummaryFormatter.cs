using System.Globalization;
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

        private static string FormatRate(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
