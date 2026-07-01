namespace AIWarehouseTwin.UI.Showcase
{
    public sealed class AbShowcaseScenarioSummary
    {
        public AbShowcaseScenarioSummary(
            string displayLabel,
            string dtoFieldName,
            string scenarioId)
        {
            DisplayLabel = displayLabel ?? string.Empty;
            DtoFieldName = dtoFieldName ?? string.Empty;
            ScenarioId = scenarioId ?? string.Empty;
        }

        public string DisplayLabel { get; }
        public string DtoFieldName { get; }
        public string ScenarioId { get; }
    }
}
