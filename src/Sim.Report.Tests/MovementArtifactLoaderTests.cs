using System.Text.Json;
using Sim.Report;
using Xunit;

namespace Sim.Report.Tests;

public sealed class MovementArtifactLoaderTests
{
    [Fact]
    public void Load_ValidMovementArtifactFixture_ReturnsArtifact()
    {
        var artifact = MovementArtifactLoader.Load(MovementArtifactFixturePath());

        Assert.Equal(
            MovementArtifactLoader.CurrentSchemaVersion,
            artifact.schema_version);
        Assert.Equal(
            MovementArtifactLoader.CurrentArtifactKind,
            artifact.artifact_kind);
        Assert.Equal("sample-small-warehouse", artifact.scenario_id);
        Assert.Equal("run-001", artifact.run_id);
        Assert.Equal(20240627, artifact.seed);
        Assert.NotNull(artifact.warehouse_graph);
        Assert.NotEmpty(artifact.actors);
        Assert.NotEmpty(artifact.movement_events);
        Assert.NotEmpty(artifact.route_segments);
        Assert.NotNull(artifact.provenance);
    }

    [Fact]
    public void Load_MissingFile_ThrowsClearError()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"missing-movement-artifact-{Guid.NewGuid():N}.json");

        var exception = Assert.Throws<FileNotFoundException>(
            () => MovementArtifactLoader.Load(path));

        Assert.Contains("MovementArtifact JSON file was not found", exception.Message);
    }

    [Fact]
    public void Load_InvalidJson_ThrowsClearError()
    {
        var path = WriteTempFile("{ not json");

        var exception = Assert.Throws<InvalidDataException>(
            () => MovementArtifactLoader.Load(path));

        Assert.Contains("MovementArtifact JSON is invalid", exception.Message);
    }

    [Fact]
    public void Load_InvalidSchemaVersion_ThrowsClearError()
    {
        var path = WriteMutatedFixture(
            document => document.RootElement
                .CloneWith("schema_version", "movement-artifact.v0"));

        var exception = Assert.Throws<InvalidDataException>(
            () => MovementArtifactLoader.Load(path));

        Assert.Contains("Unsupported schema_version", exception.Message);
        Assert.Contains(MovementArtifactLoader.CurrentSchemaVersion, exception.Message);
    }

    [Fact]
    public void Load_InvalidArtifactKind_ThrowsClearError()
    {
        var path = WriteMutatedFixture(
            document => document.RootElement
                .CloneWith("artifact_kind", "something-else"));

        var exception = Assert.Throws<InvalidDataException>(
            () => MovementArtifactLoader.Load(path));

        Assert.Contains("Unsupported artifact_kind", exception.Message);
        Assert.Contains(MovementArtifactLoader.CurrentArtifactKind, exception.Message);
    }

    private static string MovementArtifactFixturePath()
    {
        return Path.Combine(
            TestPaths.RepoRoot(),
            "tests",
            "contract",
            "fixtures",
            "movement_artifact",
            "valid-small-single-actor-route.json");
    }

    private static string WriteMutatedFixture(Func<JsonDocument, string> mutate)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(MovementArtifactFixturePath()));
        return WriteTempFile(mutate(document));
    }

    private static string WriteTempFile(string content)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"movement-artifact-loader-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }
}

internal static class JsonElementExtensions
{
    public static string CloneWith(
        this JsonElement root,
        string propertyName,
        string value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(
                   stream,
                   new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName))
                {
                    writer.WriteString(propertyName, value);
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
