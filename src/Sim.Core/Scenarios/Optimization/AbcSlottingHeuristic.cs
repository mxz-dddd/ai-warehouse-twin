using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sim.Core.Domain;
using Sim.Core.Spatial;

namespace Sim.Core.Scenarios.Optimization;

public static class AbcSlottingHeuristic
{
    public const string ConfigSchemaVersion = "abc-slotting-config.v1";
    public const string HeuristicName = "r3-phase1-demo-abc-slotting";
    public const string HeuristicVersion = "a5a-demo-v1";
    public const string DefaultBaselineScenarioPath = "datasets/medium-warehouse/scenario.json";
    public const string DefaultAnchorNodeId = "aisle-a-main";

    private static readonly StringComparer IdComparer = StringComparer.Ordinal;

    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        WriteIndented = true,
    };

    public static AbcSlottingConfig GenerateConfig(
        string scenarioJson,
        string baselineScenarioPath = DefaultBaselineScenarioPath)
    {
        if (string.IsNullOrWhiteSpace(scenarioJson))
        {
            throw new DomainRuleViolationException("ABC slotting scenario JSON cannot be empty.");
        }

        using var document = JsonDocument.Parse(scenarioJson);
        var root = document.RootElement;
        var scenarioId = RequiredString(root, "scenario_id");
        var layout = root.GetProperty("layout");
        var graph = LayoutGraphLoader.Load(layout.GetRawText());
        var skuVelocityById = ReadSkuVelocity(root);
        var currentLocationBySkuId = ReadCurrentLocations(root);
        var skuRanking = RankSkus(root, skuVelocityById, currentLocationBySkuId);
        var locationRanking = RankLocations(layout, graph);
        var assignments = BuildAssignments(skuRanking, locationRanking);

        return new AbcSlottingConfig(
            ConfigSchemaVersion,
            "medium-warehouse-abc-slotting-demo-v1",
            new BaselineScenarioReference(
                scenarioId,
                baselineScenarioPath,
                Sha256(scenarioJson)),
            new AbcSlottingHeuristicInfo(
                HeuristicName,
                HeuristicVersion,
                "sum outbound and each-pick order quantity by sku_id",
                "order by total_ordered_quantity desc, order_line_count desc, sku_id asc",
                "rank pick locations by shortest modeled distance to the A-zone pick-path anchor",
                "distance_m asc, location_id asc, node_id asc",
                "This is a deterministic demo ABC slotting heuristic for R3 Phase-1. It is not a globally optimal slotting solution. It does not include A/B comparison or improvement claims.",
                true),
            new SlottingAnchor(
                DefaultAnchorNodeId,
                "pick_path",
                "Selected from medium-warehouse slotting_notes: high-velocity SKUs in zone-b-pick are intentionally non-optimal, so the A-zone pick path is the deterministic demo anchor."),
            skuRanking,
            locationRanking,
            assignments,
            skuRanking.Skip(assignments.Count).Select(sku => sku.SkuId).ToArray(),
            "Deterministic output is produced by stable sorting on numeric scores and ordinal id tie-breakers.");
    }

    public static string GenerateConfigJson(
        string scenarioJson,
        string baselineScenarioPath = DefaultBaselineScenarioPath)
    {
        return JsonSerializer.Serialize(
            GenerateConfig(scenarioJson, baselineScenarioPath),
            OutputOptions) + "\n";
    }

    private static IReadOnlyList<SkuRankingEntry> RankSkus(
        JsonElement root,
        IReadOnlyDictionary<string, string> skuVelocityById,
        IReadOnlyDictionary<string, string> currentLocationBySkuId)
    {
        var scores = new Dictionary<string, SkuScore>(IdComparer);

        AddOrders(root.GetProperty("outbound").GetProperty("orders"), scores);
        AddOrders(root.GetProperty("each_pick").GetProperty("orders"), scores);

        return scores.Values
            .OrderByDescending(score => score.TotalOrderedQuantity)
            .ThenByDescending(score => score.OrderLineCount)
            .ThenBy(score => score.SkuId, IdComparer)
            .Select((score, index) =>
            {
                if (!skuVelocityById.TryGetValue(score.SkuId, out var velocityClass))
                {
                    throw new DomainRuleViolationException(
                        $"ABC slotting order references unknown SKU. SkuId: {score.SkuId}.");
                }

                if (!currentLocationBySkuId.TryGetValue(score.SkuId, out var currentLocationId))
                {
                    throw new DomainRuleViolationException(
                        $"ABC slotting cannot find current inventory location for SKU. SkuId: {score.SkuId}.");
                }

                return new SkuRankingEntry(
                    index + 1,
                    score.SkuId,
                    score.TotalOrderedQuantity,
                    score.OrderLineCount,
                    velocityClass,
                    currentLocationId);
            })
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> ReadSkuVelocity(JsonElement root)
    {
        return root.GetProperty("sku_master")
            .EnumerateArray()
            .OrderBy(sku => RequiredString(sku, "sku_id"), IdComparer)
            .ToDictionary(
                sku => RequiredString(sku, "sku_id"),
                sku => RequiredString(sku, "velocity_class"),
                IdComparer);
    }

    private static IReadOnlyDictionary<string, string> ReadCurrentLocations(JsonElement root)
    {
        var locations = new Dictionary<string, string>(IdComparer);
        AddInventoryLocations(root.GetProperty("outbound").GetProperty("inventory"), locations);
        AddInventoryLocations(root.GetProperty("each_pick").GetProperty("inventory"), locations);
        return locations;
    }

    private static void AddInventoryLocations(
        JsonElement inventory,
        IDictionary<string, string> locations)
    {
        foreach (var item in inventory.EnumerateArray()
                     .OrderBy(item => RequiredString(item, "inventory_id"), IdComparer))
        {
            var skuId = RequiredString(item, "sku_id");
            locations.TryAdd(skuId, RequiredString(item, "location_id"));
        }
    }

    private static void AddOrders(
        JsonElement orders,
        IDictionary<string, SkuScore> scores)
    {
        foreach (var order in orders.EnumerateArray())
        {
            var skuId = RequiredString(order, "sku_id");
            var quantity = order.GetProperty("quantity").GetDecimal();
            if (!scores.TryGetValue(skuId, out var score))
            {
                score = new SkuScore(skuId, 0, 0);
            }

            scores[skuId] = score with
            {
                TotalOrderedQuantity = score.TotalOrderedQuantity + quantity,
                OrderLineCount = score.OrderLineCount + 1,
            };
        }
    }

    private static IReadOnlyList<LocationRankingEntry> RankLocations(
        JsonElement layout,
        PathGraph graph)
    {
        var anchor = graph.Nodes.SingleOrDefault(
            node => IdComparer.Equals(node.NodeId, DefaultAnchorNodeId));
        if (anchor is null)
        {
            throw new DomainRuleViolationException(
                $"ABC slotting anchor node is missing from layout graph. NodeId: {DefaultAnchorNodeId}.");
        }

        var aisleNodes = graph.Nodes
            .Where(node => IdComparer.Equals(node.NodeType, "aisle"))
            .OrderBy(node => node.NodeId, IdComparer)
            .ToArray();
        if (aisleNodes.Length == 0)
        {
            throw new DomainRuleViolationException(
                "ABC slotting requires at least one aisle node for pick-location mapping.");
        }

        return layout.GetProperty("locations")
            .EnumerateArray()
            .Where(location => RequiredString(location, "id").StartsWith("pick-", StringComparison.Ordinal))
            .Select(location => ToLocationCandidate(location, graph, aisleNodes, DefaultAnchorNodeId))
            .OrderBy(location => location.DistanceToAnchorM)
            .ThenBy(location => location.LocationId, IdComparer)
            .ThenBy(location => location.NodeId, IdComparer)
            .Select((location, index) => new LocationRankingEntry(
                index + 1,
                location.LocationId,
                location.NodeId,
                location.ZoneId,
                location.DistanceToAnchorM,
                location.LocalOffsetM,
                location.RouteDistanceM))
            .ToArray();
    }

    private static LocationCandidate ToLocationCandidate(
        JsonElement location,
        PathGraph graph,
        IReadOnlyList<PathGraphNode> aisleNodes,
        string anchorNodeId)
    {
        var locationId = RequiredString(location, "id");
        var zoneId = RequiredString(location, "zone_id");
        var x = location.GetProperty("x").GetInt64();
        var y = location.GetProperty("y").GetInt64();
        var nearestNode = aisleNodes
            .Select(node => new
            {
                Node = node,
                DistanceMm = EuclideanDistanceMm(x, y, node.XMm, node.YMm),
            })
            .OrderBy(entry => entry.DistanceMm)
            .ThenBy(entry => entry.Node.NodeId, IdComparer)
            .First();

        var route = graph.GetRoute(nearestNode.Node.NodeId, anchorNodeId);
        var localOffsetM = Math.Round(nearestNode.DistanceMm / 1000.0, 3);
        var routeDistanceM = Math.Round(route.TotalDistanceMm / 1000.0, 3);

        return new LocationCandidate(
            locationId,
            nearestNode.Node.NodeId,
            zoneId,
            Math.Round(localOffsetM + routeDistanceM, 3),
            localOffsetM,
            routeDistanceM);
    }

    private static IReadOnlyList<SlottingAssignment> BuildAssignments(
        IReadOnlyList<SkuRankingEntry> skuRanking,
        IReadOnlyList<LocationRankingEntry> locationRanking)
    {
        return skuRanking
            .Zip(locationRanking)
            .Select(pair => new SlottingAssignment(
                pair.First.Rank,
                pair.First.SkuId,
                pair.First.CurrentLocationId,
                pair.Second.LocationId,
                pair.Second.NodeId,
                pair.Second.DistanceToAnchorM,
                IdComparer.Equals(pair.First.CurrentLocationId, pair.Second.LocationId) ? "keep" : "move"))
            .ToArray();
    }

    private static double EuclideanDistanceMm(long x1, long y1, long x2, long y2)
    {
        var deltaX = Convert.ToDouble(x2 - x1);
        var deltaY = Convert.ToDouble(y2 - y1);
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        var value = element.GetProperty(propertyName).GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleViolationException(
                $"ABC slotting requires non-empty property '{propertyName}'.");
        }

        return value;
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed record SkuScore(
        string SkuId,
        decimal TotalOrderedQuantity,
        int OrderLineCount);

    private sealed record LocationCandidate(
        string LocationId,
        string NodeId,
        string ZoneId,
        double DistanceToAnchorM,
        double LocalOffsetM,
        double RouteDistanceM);
}

public sealed record AbcSlottingConfig(
    [property: JsonPropertyName("schema_version")] string SchemaVersion,
    [property: JsonPropertyName("config_id")] string ConfigId,
    [property: JsonPropertyName("source_scenario")] BaselineScenarioReference SourceScenario,
    [property: JsonPropertyName("heuristic")] AbcSlottingHeuristicInfo Heuristic,
    [property: JsonPropertyName("anchor")] SlottingAnchor Anchor,
    [property: JsonPropertyName("sku_ranking")] IReadOnlyList<SkuRankingEntry> SkuRanking,
    [property: JsonPropertyName("location_ranking")] IReadOnlyList<LocationRankingEntry> LocationRanking,
    [property: JsonPropertyName("assignments")] IReadOnlyList<SlottingAssignment> Assignments,
    [property: JsonPropertyName("unassigned_ranked_skus")] IReadOnlyList<string> UnassignedRankedSkus,
    [property: JsonPropertyName("deterministic_policy")] string DeterministicPolicy);

public sealed record BaselineScenarioReference(
    [property: JsonPropertyName("scenario_id")] string ScenarioId,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("sha256")] string Sha256);

public sealed record AbcSlottingHeuristicInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("sku_frequency_basis")] string SkuFrequencyBasis,
    [property: JsonPropertyName("sku_sort")] string SkuSort,
    [property: JsonPropertyName("location_priority_basis")] string LocationPriorityBasis,
    [property: JsonPropertyName("location_sort")] string LocationSort,
    [property: JsonPropertyName("demo_statement")] string DemoStatement,
    [property: JsonPropertyName("not_globally_optimal")] bool NotGloballyOptimal);

public sealed record SlottingAnchor(
    [property: JsonPropertyName("node_id")] string NodeId,
    [property: JsonPropertyName("anchor_type")] string AnchorType,
    [property: JsonPropertyName("selection_reason")] string SelectionReason);

public sealed record SkuRankingEntry(
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("sku_id")] string SkuId,
    [property: JsonPropertyName("total_ordered_quantity")] decimal TotalOrderedQuantity,
    [property: JsonPropertyName("order_line_count")] int OrderLineCount,
    [property: JsonPropertyName("velocity_class")] string VelocityClass,
    [property: JsonPropertyName("current_location_id")] string CurrentLocationId);

public sealed record LocationRankingEntry(
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("location_id")] string LocationId,
    [property: JsonPropertyName("node_id")] string NodeId,
    [property: JsonPropertyName("zone_id")] string ZoneId,
    [property: JsonPropertyName("distance_to_anchor_m")] double DistanceToAnchorM,
    [property: JsonPropertyName("local_offset_m")] double LocalOffsetM,
    [property: JsonPropertyName("route_distance_m")] double RouteDistanceM);

public sealed record SlottingAssignment(
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("sku_id")] string SkuId,
    [property: JsonPropertyName("from_location_id")] string FromLocationId,
    [property: JsonPropertyName("target_location_id")] string TargetLocationId,
    [property: JsonPropertyName("target_node_id")] string TargetNodeId,
    [property: JsonPropertyName("target_distance_to_anchor_m")] double TargetDistanceToAnchorM,
    [property: JsonPropertyName("action")] string Action);
