using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public class RunArtifactLoaderTests
{
    [Fact]
    public void Load_SampleWarehouseGoldenArtifact_PopulatesContract()
    {
        var artifact = RunArtifactLoader.Load(TestPaths.ArtifactPath());

        Assert.Equal("run-artifact.v1", artifact.SchemaVersion);
        Assert.Equal("warehouse-simulation-run", artifact.ArtifactKind);
        Assert.Equal("sample-small-warehouse", artifact.ScenarioId);
        Assert.Equal(20240627, artifact.Seed);
        Assert.Equal(3, artifact.KpiSummary.TotalCompletedWorkItems);
        Assert.Equal(10, artifact.EventLog.Count);
    }

    [Fact]
    public void Deserialize_UnsupportedSchemaVersion_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v0",
          "artifact_kind": "warehouse-simulation-run",
          "scenario_id": "x",
          "kpi_summary": {},
          "event_log": []
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_UnsupportedArtifactKind_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v1",
          "artifact_kind": "something-else",
          "scenario_id": "x",
          "kpi_summary": {},
          "event_log": []
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_EmptyScenarioId_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v1",
          "artifact_kind": "warehouse-simulation-run",
          "scenario_id": "",
          "kpi_summary": {},
          "event_log": []
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_NullKpiSummary_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v1",
          "artifact_kind": "warehouse-simulation-run",
          "scenario_id": "x",
          "kpi_summary": null,
          "event_log": []
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_NullEventLog_Throws()
    {
        const string json = """
        {
          "schema_version": "run-artifact.v1",
          "artifact_kind": "warehouse-simulation-run",
          "scenario_id": "x",
          "kpi_summary": {},
          "event_log": null
        }
        """;

        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize(json));
    }

    [Fact]
    public void Deserialize_NullJson_Throws()
    {
        Assert.Throws<InvalidDataException>(() => RunArtifactLoader.Deserialize("null"));
    }
}
