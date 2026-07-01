using System.Text.Json;
using Xunit;

namespace Sim.Validation.Tests;

public sealed class MediumWarehouseDatasetTests
{
    [Fact]
    public void Scenario_HasExpectedMediumDatasetShape()
    {
        using var doc = LoadScenario();
        var root = doc.RootElement;

        var skus = root.GetProperty("sku_master").EnumerateArray().ToArray();
        var outboundOrders = root.GetProperty("outbound").GetProperty("orders").EnumerateArray().ToArray();
        var eachPickOrders = root.GetProperty("each_pick").GetProperty("orders").EnumerateArray().ToArray();
        var forklifts = root.GetProperty("resources").GetProperty("forklifts").EnumerateArray().ToArray();
        var workers = root.GetProperty("resources").GetProperty("workers").EnumerateArray().ToArray();

        Assert.Equal(30, skus.Length);
        Assert.Equal(80, outboundOrders.Length + eachPickOrders.Length);
        Assert.Equal(2, forklifts.Length);
        Assert.Equal(3, workers.Length);
        Assert.Equal(2, root.GetProperty("inbound").GetProperty("forklift_count").GetInt32());
        Assert.Equal(3, root.GetProperty("outbound").GetProperty("worker_count").GetInt32());
        Assert.Equal(3, root.GetProperty("each_pick").GetProperty("worker_count").GetInt32());
    }

    [Fact]
    public void Scenario_UsesUniqueStableIds()
    {
        using var doc = LoadScenario();
        var root = doc.RootElement;

        AssertNoDuplicates(root.GetProperty("sku_master").EnumerateArray().Select(e => String(e, "sku_id")));

        var orderIds = root.GetProperty("outbound").GetProperty("orders").EnumerateArray()
            .Concat(root.GetProperty("each_pick").GetProperty("orders").EnumerateArray())
            .Select(e => String(e, "order_id"));
        AssertNoDuplicates(orderIds);

        var inventoryIds = root.GetProperty("outbound").GetProperty("inventory").EnumerateArray()
            .Concat(root.GetProperty("each_pick").GetProperty("inventory").EnumerateArray())
            .Select(e => String(e, "inventory_id"));
        AssertNoDuplicates(inventoryIds);

        var resourceIds = root.GetProperty("resources").GetProperty("forklifts").EnumerateArray()
            .Concat(root.GetProperty("resources").GetProperty("workers").EnumerateArray())
            .Select(e => String(e, "id"));
        AssertNoDuplicates(resourceIds);
    }

    [Fact]
    public void Scenario_ReferencesOnlyKnownSkusAndLayoutLocations()
    {
        using var doc = LoadScenario();
        var root = doc.RootElement;
        var skuIds = root.GetProperty("sku_master").EnumerateArray()
            .Select(e => String(e, "sku_id"))
            .ToHashSet(StringComparer.Ordinal);
        var layoutIds = CollectLayoutIds(root.GetProperty("layout"));

        foreach (var receipt in root.GetProperty("inbound").GetProperty("receipts").EnumerateArray())
        {
            Assert.Contains(String(receipt, "sku_id"), skuIds);
            Assert.Contains(String(receipt, "staging_location_id"), layoutIds);
            Assert.Contains(String(receipt, "storage_location_id"), layoutIds);
        }

        foreach (var inventory in root.GetProperty("outbound").GetProperty("inventory").EnumerateArray()
                     .Concat(root.GetProperty("each_pick").GetProperty("inventory").EnumerateArray()))
        {
            Assert.Contains(String(inventory, "sku_id"), skuIds);
            Assert.Contains(String(inventory, "location_id"), layoutIds);
        }

        foreach (var order in root.GetProperty("outbound").GetProperty("orders").EnumerateArray())
        {
            Assert.Contains(String(order, "sku_id"), skuIds);
            Assert.Contains(String(order, "pick_location_id"), layoutIds);
            Assert.Contains(String(order, "staging_location_id"), layoutIds);
            Assert.Contains(String(order, "dock_id"), layoutIds);
        }

        foreach (var order in root.GetProperty("each_pick").GetProperty("orders").EnumerateArray())
        {
            Assert.Contains(String(order, "sku_id"), skuIds);
            Assert.Contains(String(order, "pick_face_location_id"), layoutIds);
            Assert.Contains(String(order, "pick_station_id"), layoutIds);
            Assert.Contains(String(order, "staging_location_id"), layoutIds);
        }

        foreach (var resource in root.GetProperty("resources").GetProperty("forklifts").EnumerateArray()
                     .Concat(root.GetProperty("resources").GetProperty("workers").EnumerateArray()))
        {
            Assert.Contains(String(resource, "home_node_id"), layoutIds);
        }
    }

    [Fact]
    public void Scenario_CoversZonesPeakMergeAndSlottingIntent()
    {
        using var doc = LoadScenario();
        var root = doc.RootElement;

        var layoutIds = CollectLayoutIds(root.GetProperty("layout"));
        Assert.Contains(layoutIds, id => id.StartsWith("pick-a-", StringComparison.Ordinal));
        Assert.Contains(layoutIds, id => id.StartsWith("pick-b-", StringComparison.Ordinal));
        Assert.Contains("stage-merge-01", layoutIds);

        var allOrders = root.GetProperty("outbound").GetProperty("orders").EnumerateArray()
            .Concat(root.GetProperty("each_pick").GetProperty("orders").EnumerateArray())
            .ToArray();
        var releaseTimes = allOrders.Select(e => e.GetProperty("released_at_ms").GetInt64()).ToArray();
        Assert.True(releaseTimes.Distinct().Count() > 1);
        Assert.True(releaseTimes.Count(t => t is >= 1_800_000 and <= 2_100_000) >= 40);

        Assert.Contains(root.GetProperty("outbound").GetProperty("orders").EnumerateArray(),
            o => String(o, "staging_location_id") == "stage-merge-01");
        Assert.Contains(root.GetProperty("each_pick").GetProperty("orders").EnumerateArray(),
            o => String(o, "staging_location_id") == "stage-merge-01");

        var skuVelocity = root.GetProperty("sku_master").EnumerateArray()
            .ToDictionary(e => String(e, "sku_id"), e => String(e, "velocity_class"), StringComparer.Ordinal);
        var outboundInventoryBySku = root.GetProperty("outbound").GetProperty("inventory").EnumerateArray()
            .ToDictionary(e => String(e, "sku_id"), e => String(e, "location_id"), StringComparer.Ordinal);
        var remoteHighVelocitySkus = root.GetProperty("slotting_notes")
            .GetProperty("non_optimal_high_velocity_skus")
            .EnumerateArray()
            .Select(e => e.GetString()!)
            .ToArray();

        Assert.True(remoteHighVelocitySkus.Length >= 4);
        Assert.All(remoteHighVelocitySkus, sku =>
        {
            Assert.Equal("high", skuVelocity[sku]);
            Assert.StartsWith("pick-b-", outboundInventoryBySku[sku], StringComparison.Ordinal);
        });
    }

    private static JsonDocument LoadScenario()
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestPaths.DatasetsDir(),
            "medium-warehouse",
            "scenario.json")));

    private static HashSet<string> CollectLayoutIds(JsonElement layout)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        AddIds(layout, "locations", "id", ids);
        AddIds(layout, "staging_locations", "id", ids);
        AddIds(layout, "docks", "id", ids);
        AddIds(layout, "stations", "id", ids);
        AddIds(layout, "path_nodes", "node_id", ids);
        AddIds(layout, "aisles", "id", ids);
        AddIds(layout, "racks", "id", ids);
        AddIds(layout, "zones", "id", ids);

        return ids;
    }

    private static void AddIds(JsonElement parent, string arrayName, string propertyName, HashSet<string> ids)
    {
        foreach (var item in parent.GetProperty(arrayName).EnumerateArray())
        {
            ids.Add(String(item, propertyName));
        }
    }

    private static string String(JsonElement element, string propertyName)
        => element.GetProperty(propertyName).GetString()!;

    private static void AssertNoDuplicates(IEnumerable<string> values)
    {
        var duplicates = values
            .GroupBy(v => v, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }
}
