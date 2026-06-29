namespace Sim.Report;

public sealed record CustomerReportRenderOptions(
    string? RunRunnerMode = null,
    string? ComparisonRunnerMode = null)
{
    public bool HasRunnerProvenance =>
        RunRunnerMode is not null || ComparisonRunnerMode is not null;
}
