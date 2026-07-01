using System.Text.Json;
using System.Text.Json.Nodes;
using Sim.Contracts.Artifacts;
using Sim.Core.Domain;

namespace Sim.Core.Scenarios.Optimization;

public static class MediumWarehouseAbComparison
{
    public const string OptimizedScenarioId = "medium-warehouse-optimized-abc-slotting";
    public const string OptimizationNote =
        "ABC slotting is a deterministic demo heuristic for R3 Phase-1. It is not globally optimal.";
    public const string EvidenceStatement =
        "Results are deterministic modeled simulation artifacts, not calibrated WMS or sensor-ground-truth measurements.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly IReadOnlyList<KpiMetricSpec> KpiMetricSpecs =
    [
        new("order_cycle_p50_ms", LowerIsBetter: true),
        new("order_cycle_p90_ms", LowerIsBetter: true),
        new("order_cycle_p95_ms", LowerIsBetter: true),
        new("avg_wait_ms", LowerIsBetter: true),
        new("total_duration_ms", LowerIsBetter: true),
        new("total_completed_work_items", LowerIsBetter: false),
        new("outbound_order_throughput_per_hour", LowerIsBetter: false),
        new("each_pick_order_throughput_per_hour", LowerIsBetter: false),
        new("total_work_item_throughput_per_hour", LowerIsBetter: false),
    ];

    public static string GenerateOptimizedScenarioJson(
        string baselineScenarioJson,
        string configJson)
    {
        if (string.IsNullOrWhiteSpace(baselineScenarioJson))
        {
            throw new DomainRuleViolationException(
                "A5b optimized scenario generation requires baseline scenario JSON.");
        }

        if (string.IsNullOrWhiteSpace(configJson))
        {
            throw new DomainRuleViolationException(
                "A5b optimized scenario generation requires ABC slotting config JSON.");
        }

        var scenario = JsonNode.Parse(baselineScenarioJson)?.AsObject()
            ?? throw new DomainRuleViolationException(
                "A5b baseline scenario JSON must be a JSON object.");
        var config = JsonSerializer.Deserialize<AbcSlottingConfig>(
            configJson,
            JsonOptions)
            ?? throw new DomainRuleViolationException(
                "A5b ABC slotting config JSON could not be deserialized.");
        var targetLocationBySkuId = config.Assignments
            .OrderBy(assignment => assignment.SkuId, StringComparer.Ordinal)
            .ToDictionary(
                assignment => assignment.SkuId,
                assignment => assignment.TargetLocationId,
                StringComparer.Ordinal);

        scenario["scenario_id"] = OptimizedScenarioId;
        scenario["description"] =
            "R3 Phase-1 medium warehouse optimized scenario generated from the A5a ABC slotting demo config. " +
            OptimizationNote + " " + EvidenceStatement;

        SetNestedScenarioId(scenario, "inbound", $"{OptimizedScenarioId}.inbound");
        SetNestedScenarioId(scenario, "outbound", $"{OptimizedScenarioId}.outbound");
        SetNestedScenarioId(scenario, "each_pick", $"{OptimizedScenarioId}.each-pick");

        ApplyInventoryLocations(scenario, "outbound", targetLocationBySkuId);
        ApplyInventoryLocations(scenario, "each_pick", targetLocationBySkuId);
        ApplyOutboundOrderLocations(scenario, targetLocationBySkuId);
        ApplyEachPickOrderLocations(scenario, targetLocationBySkuId);
        ApplyInboundReceiptStorageLocations(scenario, targetLocationBySkuId);

        return scenario.ToJsonString(JsonOptions) + "\n";
    }

    public static ComparisonArtifact BuildComparisonArtifact(
        WarehouseScenarioComparisonResult comparison,
        string baselineRunArtifactJson,
        string optimizedRunArtifactJson)
    {
        ArgumentNullException.ThrowIfNull(comparison);

        return new ComparisonArtifact(
            ComparisonArtifact.CurrentSchemaVersion,
            ToScenarioSummary(
                comparison.BaselineScenarioId,
                comparison.BaselineMetrics),
            ToScenarioSummary(
                comparison.CandidateScenarioId,
                comparison.CandidateMetrics),
            comparison.Deltas
                .Select(delta => new ComparisonArtifactDelta(
                    delta.MetricName,
                    Round(delta.BaselineValue),
                    Round(delta.CandidateValue),
                    Round(delta.Delta),
                    delta.DeltaPercent is null ? null : Round(delta.DeltaPercent.Value),
                    delta.Direction))
                .ToArray(),
            BuildKpiDeltas(baselineRunArtifactJson, optimizedRunArtifactJson),
            BuildImprovementPct(baselineRunArtifactJson, optimizedRunArtifactJson));
    }

    public static string BuildComparisonArtifactJson(
        WarehouseScenarioComparisonResult comparison,
        string baselineRunArtifactJson,
        string optimizedRunArtifactJson)
    {
        var artifact = BuildComparisonArtifact(
            comparison,
            baselineRunArtifactJson,
            optimizedRunArtifactJson);
        var node = JsonSerializer.SerializeToNode(artifact, JsonOptions)?.AsObject()
            ?? throw new DomainRuleViolationException(
                "A5b comparison artifact serialization did not produce a JSON object.");

        node["baseline_run_id"] = comparison.BaselineScenarioId;
        node["optimized_run_id"] = comparison.CandidateScenarioId;
        node["optimization_note"] = OptimizationNote;
        node["evidence_level"] = "deterministic_modeled";
        node["evidence_statement"] = EvidenceStatement;

        return node.ToJsonString(JsonOptions) + "\n";
    }

    public static decimal? CalculateImprovementPct(
        decimal baselineValue,
        decimal optimizedValue,
        bool lowerIsBetter)
    {
        if (baselineValue == 0m)
        {
            return optimizedValue == 0m ? 0m : null;
        }

        var numerator = lowerIsBetter
            ? baselineValue - optimizedValue
            : optimizedValue - baselineValue;
        return Round(numerator / Math.Abs(baselineValue) * 100m);
    }

    public static IReadOnlyDictionary<string, ComparisonArtifactKpiDelta> BuildKpiDeltas(
        string baselineRunArtifactJson,
        string optimizedRunArtifactJson)
    {
        using var baselineDocument = JsonDocument.Parse(baselineRunArtifactJson);
        using var optimizedDocument = JsonDocument.Parse(optimizedRunArtifactJson);

        var baselineKpi = baselineDocument.RootElement.GetProperty("kpi_summary");
        var optimizedKpi = optimizedDocument.RootElement.GetProperty("kpi_summary");
        var result = new SortedDictionary<string, ComparisonArtifactKpiDelta>(
            StringComparer.Ordinal);

        foreach (var metric in KpiMetricSpecs)
        {
            var baselineValue = RequiredDecimal(baselineKpi, metric.Name);
            var optimizedValue = RequiredDecimal(optimizedKpi, metric.Name);
            result[metric.Name] = new ComparisonArtifactKpiDelta
            {
                BaselineValue = Round(baselineValue),
                CandidateValue = Round(optimizedValue),
                Delta = Round(optimizedValue - baselineValue),
                LowerIsBetter = metric.LowerIsBetter,
            };
        }

        return result;
    }

    public static IReadOnlyDictionary<string, decimal> BuildImprovementPct(
        string baselineRunArtifactJson,
        string optimizedRunArtifactJson)
    {
        using var baselineDocument = JsonDocument.Parse(baselineRunArtifactJson);
        using var optimizedDocument = JsonDocument.Parse(optimizedRunArtifactJson);

        var baselineKpi = baselineDocument.RootElement.GetProperty("kpi_summary");
        var optimizedKpi = optimizedDocument.RootElement.GetProperty("kpi_summary");
        var result = new SortedDictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var metric in KpiMetricSpecs)
        {
            var improvement = CalculateImprovementPct(
                RequiredDecimal(baselineKpi, metric.Name),
                RequiredDecimal(optimizedKpi, metric.Name),
                metric.LowerIsBetter);
            if (improvement is not null)
            {
                result[metric.Name] = improvement.Value;
            }
        }

        return result;
    }

    private static void SetNestedScenarioId(
        JsonObject scenario,
        string sectionName,
        string scenarioId)
    {
        if (scenario[sectionName] is JsonObject section)
        {
            section["scenario_id"] = scenarioId;
        }
    }

    private static void ApplyInventoryLocations(
        JsonObject scenario,
        string sectionName,
        IReadOnlyDictionary<string, string> targetLocationBySkuId)
    {
        if (scenario[sectionName]?["inventory"] is not JsonArray inventory)
        {
            return;
        }

        foreach (var item in inventory.OfType<JsonObject>())
        {
            ApplyLocationBySku(item, "location_id", targetLocationBySkuId);
        }
    }

    private static void ApplyOutboundOrderLocations(
        JsonObject scenario,
        IReadOnlyDictionary<string, string> targetLocationBySkuId)
    {
        if (scenario["outbound"]?["orders"] is not JsonArray orders)
        {
            return;
        }

        foreach (var item in orders.OfType<JsonObject>())
        {
            ApplyLocationBySku(item, "pick_location_id", targetLocationBySkuId);
        }
    }

    private static void ApplyEachPickOrderLocations(
        JsonObject scenario,
        IReadOnlyDictionary<string, string> targetLocationBySkuId)
    {
        if (scenario["each_pick"]?["orders"] is not JsonArray orders)
        {
            return;
        }

        foreach (var item in orders.OfType<JsonObject>())
        {
            ApplyLocationBySku(item, "pick_face_location_id", targetLocationBySkuId);
        }
    }

    private static void ApplyInboundReceiptStorageLocations(
        JsonObject scenario,
        IReadOnlyDictionary<string, string> targetLocationBySkuId)
    {
        if (scenario["inbound"]?["receipts"] is not JsonArray receipts)
        {
            return;
        }

        foreach (var item in receipts.OfType<JsonObject>())
        {
            ApplyLocationBySku(item, "storage_location_id", targetLocationBySkuId);
        }
    }

    private static void ApplyLocationBySku(
        JsonObject item,
        string locationPropertyName,
        IReadOnlyDictionary<string, string> targetLocationBySkuId)
    {
        var skuId = item["sku_id"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(skuId) &&
            targetLocationBySkuId.TryGetValue(skuId, out var targetLocationId))
        {
            item[locationPropertyName] = targetLocationId;
        }
    }

    private static ComparisonArtifactScenarioSummary ToScenarioSummary(
        string scenarioId,
        WarehouseScenarioComparisonMetrics metrics)
    {
        return new ComparisonArtifactScenarioSummary(
            scenarioId,
            new ComparisonArtifactMetrics(
                metrics.FinishedAtMs,
                metrics.CompletedReceipts,
                metrics.CompletedOutboundOrders,
                metrics.CompletedEachPickOrders,
                metrics.TotalQuantityReceived,
                metrics.TotalQuantityShipped,
                metrics.TotalQuantityPicked,
                Round(metrics.InboundReceiptThroughputPerHour),
                Round(metrics.OutboundOrderThroughputPerHour),
                Round(metrics.EachPickOrderThroughputPerHour),
                Round(metrics.TotalWorkItemThroughputPerHour)));
    }

    private static decimal RequiredDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            throw new DomainRuleViolationException(
                $"A5b comparison requires RunArtifact KPI field '{propertyName}'.");
        }

        return value.GetDecimal();
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    private sealed record KpiMetricSpec(string Name, bool LowerIsBetter);
}
