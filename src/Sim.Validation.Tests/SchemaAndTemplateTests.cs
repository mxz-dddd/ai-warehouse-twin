using System.Text.Json;
using Sim.Validation;
using Xunit;

namespace Sim.Validation.Tests;

public class SchemaAndTemplateTests
{
    [Fact]
    public void Schema_IsWellFormedAndPinsSchemaVersion()
    {
        var path = Path.Combine(TestPaths.TemplatesDir(), "scenario.schema.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.True(root.TryGetProperty("$id", out _));

        var required = root.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("schema_version", required);
        Assert.Contains("scenario_id", required);
        Assert.Contains("seed", required);

        var versionConst = root.GetProperty("properties").GetProperty("schema_version").GetProperty("const").GetString();
        Assert.Equal(ScenarioValidator.ExpectedSchemaVersion, versionConst);
    }

    [Fact]
    public void Template_PassesValidator()
    {
        var path = Path.Combine(TestPaths.TemplatesDir(), "scenario.template.json");

        var result = ScenarioValidator.Validate(File.ReadAllText(path));

        Assert.True(result.IsValid, ScenarioErrorFormatter.FormatText(result, "scenario.template.json"));
    }

    [Fact]
    public void Schema_StatusEnumMatchesValidatorExactly()
    {
        var path = Path.Combine(TestPaths.TemplatesDir(), "scenario.schema.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var schemaEnum = doc.RootElement.GetProperty("$defs").GetProperty("status").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString()!).ToHashSet();

        var validatorEnum = ScenarioValidator.AllowedInventoryStatuses.ToHashSet();

        Assert.Equal(validatorEnum, schemaEnum);
    }

    [Fact]
    public void StatusEnum_AcceptsCanonicalValue_RejectsUnknown()
    {
        var good = ScenarioValidator.Validate(MinimalWithStatus("available"));
        Assert.True(good.IsValid, ScenarioErrorFormatter.FormatText(good));

        var bad = ScenarioValidator.Validate(MinimalWithStatus("in_stock"));
        Assert.Contains(bad.Errors, e => e.Code == "value.invalid_enum");
    }

    private static string MinimalWithStatus(string status) => $$"""
    {
      "schema_version": "warehouse-scenario.v0",
      "scenario_id": "x",
      "seed": 1,
      "each_pick": {
        "scenario_id": "x.each-pick",
        "station_count": 1, "worker_count": 1,
        "process": { "tote_bind_duration_ms": 10, "travel_to_station_duration_ms": 20, "pick_service_duration_ms": 30, "move_to_staging_duration_ms": 40 },
        "inventory": [ { "inventory_id": "i", "sku_id": "s", "quantity": 5, "location_id": "l", "status": "{{status}}" } ],
        "orders": [ { "order_id": "o", "warehouse_id": "w", "sku_id": "s", "quantity": 5, "pick_face_location_id": "pf", "pick_station_id": "ps", "staging_location_id": "st", "released_at_ms": 0 } ]
      }
    }
    """;
}
