using Sim.Validation;
using Xunit;

namespace Sim.Validation.Tests;

public class ScenarioValidatorRuleTests
{
    private const string ValidEachPick = """
    {
      "schema_version": "warehouse-scenario.v0",
      "scenario_id": "rule-test",
      "seed": 1,
      "each_pick": {
        "station_count": 1,
        "worker_count": 1,
        "process": {
          "tote_bind_duration_ms": 10,
          "travel_to_station_duration_ms": 20,
          "pick_service_duration_ms": 30,
          "move_to_staging_duration_ms": 40
        },
        "inventory": [
          { "inventory_id": "inv-1", "sku_id": "sku-1", "quantity": 9, "location_id": "pick-face-1", "status": "available" }
        ],
        "orders": [
          { "order_id": "o-1", "warehouse_id": "wh-1", "sku_id": "sku-1", "quantity": 5, "pick_face_location_id": "pick-face-1", "pick_station_id": "station-1", "staging_location_id": "stage-1", "released_at_ms": 0 }
        ]
      }
    }
    """;

    [Fact]
    public void ValidMinimalScenario_IsValid()
    {
        var result = ScenarioValidator.Validate(ValidEachPick);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void MissingSchemaVersion_ReportedAtPath()
    {
        var json = """
        {
          "scenario_id": "x",
          "seed": 1,
          "each_pick": {
            "station_count": 1, "worker_count": 1,
            "process": { "tote_bind_duration_ms": 0, "travel_to_station_duration_ms": 0, "pick_service_duration_ms": 0, "move_to_staging_duration_ms": 0 },
            "inventory": [ { "inventory_id": "i", "sku_id": "s", "quantity": 1, "location_id": "l", "status": "available" } ],
            "orders": [ { "order_id": "o", "warehouse_id": "w", "sku_id": "s", "quantity": 1, "pick_face_location_id": "pf", "pick_station_id": "ps", "staging_location_id": "st", "released_at_ms": 0 } ]
          }
        }
        """;

        var result = ScenarioValidator.Validate(json);

        Assert.Contains(result.Errors, e => e.Code == "field.missing" && e.Path == "$.schema_version");
    }

    [Fact]
    public void WrongSchemaVersion_Reported()
    {
        var json = ValidEachPick.Replace("warehouse-scenario.v0", "warehouse-scenario.v9");

        var result = ScenarioValidator.Validate(json);

        Assert.Contains(result.Errors, e => e.Code == "schema_version.unsupported" && e.Path == "$.schema_version");
    }

    [Fact]
    public void QuantityZero_ReportedAsMustBePositive()
    {
        // Inventory quantity is 9 in the base doc, so this targets only the order.
        var json = ValidEachPick.Replace("\"quantity\": 5", "\"quantity\": 0");

        var result = ScenarioValidator.Validate(json);

        var error = Assert.Single(result.Errors);
        Assert.Equal("value.must_be_positive", error.Code);
        Assert.Equal("$.each_pick.orders[0].quantity", error.Path);
    }

    [Fact]
    public void NegativeDuration_ReportedAsMustBeNonNegative()
    {
        var json = ValidEachPick.Replace("\"pick_service_duration_ms\": 30", "\"pick_service_duration_ms\": -3");

        var result = ScenarioValidator.Validate(json);

        Assert.Contains(
            result.Errors,
            e => e.Code == "value.must_be_non_negative" && e.Path == "$.each_pick.process.pick_service_duration_ms");
    }

    [Fact]
    public void BadStatus_ReportedWithAllowedValues()
    {
        var json = ValidEachPick.Replace("\"available\"", "\"in_stock\"");

        var result = ScenarioValidator.Validate(json);

        var error = Assert.Single(result.Errors, e => e.Code == "value.invalid_enum");
        Assert.Equal("$.each_pick.inventory[0].status", error.Path);
        Assert.Contains("in_stock", error.Message);
        Assert.Contains("available", error.Message);
    }

    [Fact]
    public void UppercaseStatus_IsRejected_LowercaseExpected()
    {
        var json = ValidEachPick.Replace("\"available\"", "\"AVAILABLE\"");

        var result = ScenarioValidator.Validate(json);

        Assert.Contains(result.Errors, e => e.Code == "value.invalid_enum" && e.Path == "$.each_pick.inventory[0].status");
    }

    [Fact]
    public void StringWhereIntegerExpected_ReportedAsTypeError()
    {
        var json = ValidEachPick.Replace("\"quantity\": 5", "\"quantity\": \"5\"");

        var result = ScenarioValidator.Validate(json);

        var error = Assert.Single(result.Errors);
        Assert.Equal("field.type", error.Code);
        Assert.Equal("$.each_pick.orders[0].quantity", error.Path);
    }

    [Fact]
    public void NoFlow_Reported()
    {
        var json = """{ "schema_version": "warehouse-scenario.v0", "scenario_id": "x", "seed": 1 }""";

        var result = ScenarioValidator.Validate(json);

        Assert.Contains(result.Errors, e => e.Code == "scenario.no_flow" && e.Path == "$");
    }

    [Fact]
    public void EmptyOrders_ReportedAsArrayEmpty()
    {
        var json = """
        {
          "schema_version": "warehouse-scenario.v0", "scenario_id": "x", "seed": 1,
          "each_pick": {
            "station_count": 1, "worker_count": 1,
            "process": { "tote_bind_duration_ms": 10, "travel_to_station_duration_ms": 20, "pick_service_duration_ms": 30, "move_to_staging_duration_ms": 40 },
            "inventory": [ { "inventory_id": "i", "sku_id": "s", "quantity": 5, "location_id": "l", "status": "available" } ],
            "orders": []
          }
        }
        """;

        var result = ScenarioValidator.Validate(json);

        Assert.Contains(result.Errors, e => e.Code == "array.empty" && e.Path == "$.each_pick.orders");
    }

    [Fact]
    public void MissingFields_AccumulateWithPrecisePaths()
    {
        var json = """
        {
          "schema_version": "warehouse-scenario.v0", "scenario_id": "x", "seed": 1,
          "each_pick": {
            "station_count": 1, "worker_count": 1,
            "process": { "tote_bind_duration_ms": 10, "travel_to_station_duration_ms": 20, "pick_service_duration_ms": 30, "move_to_staging_duration_ms": 40 },
            "inventory": [ { "inventory_id": "i", "sku_id": "s", "quantity": 5, "location_id": "l", "status": "available" } ],
            "orders": [ { "order_id": "o", "warehouse_id": "w", "quantity": 5, "staging_location_id": "st", "released_at_ms": 0 } ]
          }
        }
        """;

        var result = ScenarioValidator.Validate(json);

        var missing = result.Errors.Where(e => e.Code == "field.missing").ToList();
        Assert.Contains(missing, e => e.Path == "$.each_pick.orders[0].sku_id");
        Assert.Contains(missing, e => e.Path == "$.each_pick.orders[0].pick_face_location_id");
        Assert.Contains(missing, e => e.Path == "$.each_pick.orders[0].pick_station_id");
    }

    [Fact]
    public void ExpectedNegative_ReportedAtExpectedPath()
    {
        var json = """
        {
          "schema_version": "warehouse-scenario.v0", "scenario_id": "x", "seed": 1,
          "each_pick": {
            "station_count": 1, "worker_count": 1,
            "process": { "tote_bind_duration_ms": 10, "travel_to_station_duration_ms": 20, "pick_service_duration_ms": 30, "move_to_staging_duration_ms": 40 },
            "inventory": [ { "inventory_id": "i", "sku_id": "s", "quantity": 5, "location_id": "l", "status": "available" } ],
            "orders": [ { "order_id": "o", "warehouse_id": "w", "sku_id": "s", "quantity": 5, "pick_face_location_id": "pf", "pick_station_id": "ps", "staging_location_id": "st", "released_at_ms": 0 } ]
          },
          "expected": { "completed_each_pick_orders": -1 }
        }
        """;

        var result = ScenarioValidator.Validate(json);

        Assert.Contains(
            result.Errors,
            e => e.Code == "value.must_be_non_negative" && e.Path == "$.expected.completed_each_pick_orders");
    }

    [Fact]
    public void MalformedJson_ReportedAsParseError()
    {
        var result = ScenarioValidator.Validate("{ not valid json ");

        var error = Assert.Single(result.Errors);
        Assert.Equal("json.parse_error", error.Code);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void PresentButNullOptionalField_ReportedAsTypeError()
    {
        var json = """
        {
          "schema_version": "warehouse-scenario.v0", "scenario_id": "x", "seed": 1,
          "description": null,
          "each_pick": {
            "station_count": 1, "worker_count": 1,
            "process": { "tote_bind_duration_ms": 10, "travel_to_station_duration_ms": 20, "pick_service_duration_ms": 30, "move_to_staging_duration_ms": 40 },
            "inventory": [ { "inventory_id": "i", "sku_id": "s", "quantity": 5, "location_id": "l", "status": "available" } ],
            "orders": [ { "order_id": "o", "warehouse_id": "w", "sku_id": "s", "quantity": 5, "pick_face_location_id": "pf", "pick_station_id": "ps", "staging_location_id": "st", "released_at_ms": 0 } ]
          }
        }
        """;

        var result = ScenarioValidator.Validate(json);

        Assert.Contains(result.Errors, e => e.Code == "field.type" && e.Path == "$.description");
    }

    [Fact]
    public void NullFlow_ReportedAsTypeError_NotMissingNorNoFlow()
    {
        var json = """{ "schema_version": "warehouse-scenario.v0", "scenario_id": "x", "seed": 1, "each_pick": null }""";

        var result = ScenarioValidator.Validate(json);

        Assert.Contains(result.Errors, e => e.Code == "field.type" && e.Path == "$.each_pick");
        Assert.DoesNotContain(result.Errors, e => e.Code == "scenario.no_flow");
    }
}
