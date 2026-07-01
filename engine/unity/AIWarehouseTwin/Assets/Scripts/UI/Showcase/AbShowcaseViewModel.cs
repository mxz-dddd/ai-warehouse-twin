using System;
using System.Collections.Generic;

namespace AIWarehouseTwin.UI.Showcase
{
    public sealed class AbShowcaseViewModel
    {
        public AbShowcaseViewModel(
            bool isAvailable,
            string unavailableReason,
            bool isMock,
            string sourceLabel,
            string evidenceLabel,
            AbShowcaseScenarioSummary baseline,
            AbShowcaseScenarioSummary candidate,
            IReadOnlyList<AbShowcaseKpiRow> kpiRows,
            int deltaCount)
        {
            IsAvailable = isAvailable;
            UnavailableReason = unavailableReason ?? string.Empty;
            IsMock = isMock;
            SourceLabel = sourceLabel ?? string.Empty;
            EvidenceLabel = evidenceLabel ?? string.Empty;
            Baseline = baseline ?? new AbShowcaseScenarioSummary("Baseline", "baseline", string.Empty);
            Candidate = candidate ?? new AbShowcaseScenarioSummary("Optimized", "candidate", string.Empty);
            KpiRows = kpiRows ?? Array.Empty<AbShowcaseKpiRow>();
            DeltaCount = deltaCount;
        }

        public bool IsAvailable { get; }
        public string UnavailableReason { get; }
        public bool IsMock { get; }
        public string SourceLabel { get; }
        public string EvidenceLabel { get; }
        public AbShowcaseScenarioSummary Baseline { get; }
        public AbShowcaseScenarioSummary Candidate { get; }
        public IReadOnlyList<AbShowcaseKpiRow> KpiRows { get; }
        public int DeltaCount { get; }
        public bool HasDeltas => DeltaCount > 0;
    }
}
