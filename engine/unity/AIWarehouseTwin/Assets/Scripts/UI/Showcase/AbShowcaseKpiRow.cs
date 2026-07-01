namespace AIWarehouseTwin.UI.Showcase
{
    public sealed class AbShowcaseKpiRow
    {
        public AbShowcaseKpiRow(
            string metricName,
            double baselineValue,
            double candidateValue,
            double delta,
            double? improvementPct,
            bool lowerIsBetter,
            string direction,
            string baselineDisplay,
            string candidateDisplay,
            string deltaDisplay,
            string improvementDisplay,
            string directionLabel,
            string trendLabel,
            string sourceLabel)
        {
            MetricName = metricName ?? string.Empty;
            BaselineValue = baselineValue;
            CandidateValue = candidateValue;
            Delta = delta;
            ImprovementPct = improvementPct;
            LowerIsBetter = lowerIsBetter;
            Direction = direction ?? string.Empty;
            BaselineDisplay = baselineDisplay ?? string.Empty;
            CandidateDisplay = candidateDisplay ?? string.Empty;
            DeltaDisplay = deltaDisplay ?? string.Empty;
            ImprovementDisplay = improvementDisplay ?? string.Empty;
            DirectionLabel = directionLabel ?? string.Empty;
            TrendLabel = trendLabel ?? string.Empty;
            SourceLabel = sourceLabel ?? string.Empty;
        }

        public string MetricName { get; }
        public double BaselineValue { get; }
        public double CandidateValue { get; }
        public double Delta { get; }
        public double? ImprovementPct { get; }
        public bool LowerIsBetter { get; }
        public string Direction { get; }
        public string BaselineDisplay { get; }
        public string CandidateDisplay { get; }
        public string DeltaDisplay { get; }
        public string ImprovementDisplay { get; }
        public string DirectionLabel { get; }
        public string TrendLabel { get; }
        public string SourceLabel { get; }
    }
}
