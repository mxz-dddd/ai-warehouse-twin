namespace Sim.Report;

public static class CustomerReportService
{
    public static string RenderFromFiles(
        string runArtifactPath,
        string comparisonArtifactPath)
    {
        return RenderFromFiles(
            runArtifactPath,
            comparisonArtifactPath,
            new CustomerReportRenderOptions());
    }

    public static string RenderFromFiles(
        string runArtifactPath,
        string comparisonArtifactPath,
        CustomerReportRenderOptions options)
    {
        var runArtifact = RunArtifactLoader.Load(runArtifactPath);
        var comparisonArtifact = ComparisonArtifactLoader.Load(comparisonArtifactPath);

        return CustomerMarkdownReportRenderer.Render(
            runArtifact,
            comparisonArtifact,
            options);
    }
}
