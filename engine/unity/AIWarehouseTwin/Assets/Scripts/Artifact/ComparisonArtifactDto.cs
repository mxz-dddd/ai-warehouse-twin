using System;
using System.Collections.Generic;

namespace AIWarehouseTwin.Artifact
{
    [Serializable]
    public sealed class ComparisonArtifactDto
    {
        public string schema_version = string.Empty;
        public ComparisonScenarioSummaryDto baseline = new ComparisonScenarioSummaryDto();
        public ComparisonScenarioSummaryDto candidate = new ComparisonScenarioSummaryDto();
        public ComparisonDeltaDto[] deltas = Array.Empty<ComparisonDeltaDto>();
        public Dictionary<string, ComparisonKpiDeltaDto> kpi_deltas =
            new Dictionary<string, ComparisonKpiDeltaDto>();
        public Dictionary<string, double> improvement_pct = new Dictionary<string, double>();
    }

    [Serializable]
    public sealed class ComparisonScenarioSummaryDto
    {
        public string scenario_id = string.Empty;
        public ComparisonMetricsDto metrics = new ComparisonMetricsDto();
    }

    [Serializable]
    public sealed class ComparisonMetricsDto
    {
        public long finished_at_ms;
        public int completed_receipts;
        public int completed_outbound_orders;
        public int completed_each_pick_orders;
        public double total_quantity_received;
        public double total_quantity_shipped;
        public double total_quantity_picked;
        public double inbound_receipt_throughput_per_hour;
        public double outbound_order_throughput_per_hour;
        public double each_pick_order_throughput_per_hour;
        public double total_work_item_throughput_per_hour;
    }

    [Serializable]
    public sealed class ComparisonDeltaDto
    {
        public string metric_name = string.Empty;
        public double baseline_value;
        public double candidate_value;
        public double delta;
        public double? delta_percent;
        public string direction = string.Empty;
    }

    [Serializable]
    public sealed class ComparisonKpiDeltaDto
    {
        public double baseline_value;
        public double candidate_value;
        public double delta;
        public bool lower_is_better;
    }
}
