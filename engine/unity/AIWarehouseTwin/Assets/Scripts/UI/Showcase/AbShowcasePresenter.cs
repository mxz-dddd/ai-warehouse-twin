using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AIWarehouseTwin.Artifact;

namespace AIWarehouseTwin.UI.Showcase
{
    public static class AbShowcasePresenter
    {
        public const string BaselineDisplayLabel = "Baseline";
        public const string CandidateDisplayLabel = "Optimized";
        public const string MockSourceLabel = "Mock comparison fixture";
        public const string MockEvidenceLabel = "Demo only - not a real optimization result; A5b comparison generation not assumed.";

        public static AbShowcaseViewModel FromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Unavailable("ComparisonArtifact fixture path is empty.");
            }

            try
            {
                return FromComparisonArtifact(ComparisonArtifactLoader.LoadFromFile(path), MockSourceLabel);
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is FileNotFoundException ||
                ex is InvalidOperationException)
            {
                return Unavailable($"ComparisonArtifact unavailable: {ex.Message}");
            }
        }

        public static AbShowcaseViewModel FromJson(string json)
        {
            try
            {
                return FromComparisonArtifact(ComparisonArtifactLoader.LoadFromJson(json), MockSourceLabel);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return Unavailable($"ComparisonArtifact unavailable: {ex.Message}");
            }
        }

        public static AbShowcaseViewModel FromComparisonArtifact(
            ComparisonArtifactDto artifact,
            string sourceLabel)
        {
            if (artifact == null)
            {
                return Unavailable("ComparisonArtifact is missing.");
            }

            var baseline = new AbShowcaseScenarioSummary(
                BaselineDisplayLabel,
                "baseline",
                artifact.baseline?.scenario_id ?? string.Empty);

            var candidate = new AbShowcaseScenarioSummary(
                CandidateDisplayLabel,
                "candidate",
                artifact.candidate?.scenario_id ?? string.Empty);

            var deltasByMetric = new Dictionary<string, ComparisonDeltaDto>();
            if (artifact.deltas != null)
            {
                foreach (var delta in artifact.deltas)
                {
                    if (!string.IsNullOrWhiteSpace(delta.metric_name))
                    {
                        deltasByMetric[delta.metric_name] = delta;
                    }
                }
            }

            var rows = new List<AbShowcaseKpiRow>();
            if (artifact.kpi_deltas != null)
            {
                foreach (var entry in artifact.kpi_deltas)
                {
                    double improvement = 0;
                    var hasImprovement = artifact.improvement_pct != null &&
                        artifact.improvement_pct.TryGetValue(entry.Key, out improvement);
                    deltasByMetric.TryGetValue(entry.Key, out var delta);

                    rows.Add(ToKpiRow(
                        entry.Key,
                        entry.Value,
                        hasImprovement ? improvement : (double?)null,
                        delta?.direction,
                        sourceLabel));
                }
            }

            rows.Sort((left, right) => string.CompareOrdinal(left.MetricName, right.MetricName));

            return new AbShowcaseViewModel(
                true,
                string.Empty,
                true,
                sourceLabel,
                MockEvidenceLabel,
                baseline,
                candidate,
                rows,
                artifact.deltas?.Length ?? 0);
        }

        private static AbShowcaseKpiRow ToKpiRow(
            string metricName,
            ComparisonKpiDeltaDto delta,
            double? improvementPct,
            string direction,
            string sourceLabel)
        {
            return new AbShowcaseKpiRow(
                metricName,
                delta.baseline_value,
                delta.candidate_value,
                delta.delta,
                improvementPct,
                delta.lower_is_better,
                direction,
                FormatNumber(delta.baseline_value),
                FormatNumber(delta.candidate_value),
                FormatSignedNumber(delta.delta),
                FormatImprovement(improvementPct),
                delta.lower_is_better ? "Lower is better" : "Higher is better",
                improvementPct.HasValue
                    ? (improvementPct.Value >= 0 ? "Improvement" : "Regression")
                    : "N/A",
                sourceLabel);
        }

        private static AbShowcaseViewModel Unavailable(string reason)
        {
            return new AbShowcaseViewModel(
                false,
                reason,
                true,
                MockSourceLabel,
                MockEvidenceLabel,
                new AbShowcaseScenarioSummary(BaselineDisplayLabel, "baseline", string.Empty),
                new AbShowcaseScenarioSummary(CandidateDisplayLabel, "candidate", string.Empty),
                Array.Empty<AbShowcaseKpiRow>(),
                0);
        }

        private static string FormatNumber(double value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string FormatSignedNumber(double value)
        {
            var sign = value >= 0 ? "+" : string.Empty;
            return sign + FormatNumber(value);
        }

        private static string FormatImprovement(double? value)
        {
            if (!value.HasValue)
            {
                return "N/A";
            }

            var sign = value.Value >= 0 ? "+" : string.Empty;
            return $"{sign}{value.Value.ToString("0.0", CultureInfo.InvariantCulture)}%";
        }
    }
}
