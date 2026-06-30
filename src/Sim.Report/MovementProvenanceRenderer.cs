using System.Text;
using System.Text.Json;
using WarehouseTwin.Contracts;

namespace Sim.Report;

public static class MovementProvenanceRenderer
{
    public static string Render(MovementArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var graphSource = GetProvenanceField(artifact, "graph_source") ?? "(unknown)";
        var generatorVersion = GetProvenanceField(artifact, "movement_generator_version") ?? "(unknown)";

        var sb = new StringBuilder();
        void Line(string text = "") => sb.Append(text).Append('\n');

        Line("## Movement Data Provenance");
        Line();
        Line($"- schema_version: {artifact.schema_version}");
        Line($"- scenario_id: {artifact.scenario_id} / run_id: {artifact.run_id}");
        Line($"- graph_source: {graphSource} / generator_version: {generatorVersion}");
        Line($"- source_run_artifact: {artifact.source_run_artifact}");
        Line("- **Note**: RunArtifact v1 position_timeline contains baseline layout positions, NOT simulated movement.");

        return sb.ToString();
    }

    private static string? GetProvenanceField(MovementArtifact artifact, string propertyName)
    {
        if (artifact.provenance is JsonElement element &&
            element.TryGetProperty(propertyName, out var prop))
        {
            return prop.GetString();
        }

        return null;
    }
}
