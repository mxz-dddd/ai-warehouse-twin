using System.Text.Json;
using System.Text.Json.Serialization;
using Sim.Core.Domain;
using Sim.Core.Processes.EachPick;
using Sim.Core.Processes.Inbound;
using Sim.Core.Processes.Outbound;

namespace Sim.Core.Scenarios.Json;

public static class WarehouseScenarioJsonLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static WarehouseScenario Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Scenario JSON path cannot be empty.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    public static WarehouseScenario FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Scenario JSON cannot be empty.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<WarehouseScenarioDto>(json, Options)
            ?? throw new ArgumentException("Scenario JSON cannot be deserialized.");

        return ToScenario(dto);
    }

    private static WarehouseScenario ToScenario(WarehouseScenarioDto dto)
    {
        return new WarehouseScenario(
            Required(dto.ScenarioId, "scenario_id"),
            dto.Seed,
            dto.Inbound is null ? null : ToInboundScenario(dto.Inbound),
            dto.Outbound is null ? null : ToOutboundScenario(dto.Outbound),
            dto.EachPick is null ? null : ToEachPickScenario(dto.EachPick));
    }

    private static InboundScenario ToInboundScenario(InboundScenarioDto dto)
    {
        var process = Required(dto.Process, "inbound.process");

        return new InboundScenario(
            Required(dto.ScenarioId, "inbound.scenario_id"),
            dto.Seed,
            (dto.Receipts ?? []).Select(ToInboundReceipt).ToArray(),
            new InboundProcessParameters(
                process.UnloadDurationMs,
                process.QcDurationMs,
                process.PutawayDurationMs),
            dto.DockCount,
            dto.ForkliftCount);
    }

    private static InboundReceipt ToInboundReceipt(InboundReceiptDto dto)
    {
        return new InboundReceipt(
            Required(dto.ReceiptId, "inbound.receipts[].receipt_id"),
            Required(dto.WarehouseId, "inbound.receipts[].warehouse_id"),
            Required(dto.SkuId, "inbound.receipts[].sku_id"),
            dto.Quantity,
            Required(dto.StagingLocationId, "inbound.receipts[].staging_location_id"),
            Required(dto.StorageLocationId, "inbound.receipts[].storage_location_id"),
            dto.ArrivesAtMs);
    }

    private static OutboundScenario ToOutboundScenario(OutboundScenarioDto dto)
    {
        var process = Required(dto.Process, "outbound.process");

        return new OutboundScenario(
            Required(dto.ScenarioId, "outbound.scenario_id"),
            dto.Seed,
            (dto.Orders ?? []).Select(ToOutboundOrder).ToArray(),
            (dto.Inventory ?? []).Select(ToOutboundInventoryItem).ToArray(),
            new OutboundProcessParameters(
                process.PickDurationMs,
                process.StageDurationMs,
                process.DockTravelDurationMs,
                process.LoadDurationMs),
            dto.WorkerCount,
            dto.DockCount);
    }

    private static OutboundOrder ToOutboundOrder(OutboundOrderDto dto)
    {
        return new OutboundOrder(
            Required(dto.OrderId, "outbound.orders[].order_id"),
            Required(dto.WarehouseId, "outbound.orders[].warehouse_id"),
            Required(dto.SkuId, "outbound.orders[].sku_id"),
            dto.Quantity,
            Required(dto.PickLocationId, "outbound.orders[].pick_location_id"),
            Required(dto.StagingLocationId, "outbound.orders[].staging_location_id"),
            Required(dto.DockId, "outbound.orders[].dock_id"),
            dto.ReleasedAtMs);
    }

    private static OutboundInventoryItem ToOutboundInventoryItem(InventoryItemDto dto)
    {
        return new OutboundInventoryItem(
            Required(dto.InventoryId, "outbound.inventory[].inventory_id"),
            Required(dto.SkuId, "outbound.inventory[].sku_id"),
            dto.Quantity,
            Required(dto.LocationId, "outbound.inventory[].location_id"),
            ParseInventoryStatus(Required(dto.Status, "outbound.inventory[].status")));
    }

    private static EachPickScenario ToEachPickScenario(EachPickScenarioDto dto)
    {
        var process = Required(dto.Process, "each_pick.process");

        return new EachPickScenario(
            Required(dto.ScenarioId, "each_pick.scenario_id"),
            dto.Seed,
            (dto.Orders ?? []).Select(ToEachPickOrder).ToArray(),
            (dto.Inventory ?? []).Select(ToEachPickInventoryItem).ToArray(),
            new EachPickProcessParameters(
                process.ToteBindDurationMs,
                process.TravelToStationDurationMs,
                process.PickServiceDurationMs,
                process.MoveToStagingDurationMs),
            dto.StationCount,
            dto.WorkerCount);
    }

    private static EachPickOrder ToEachPickOrder(EachPickOrderDto dto)
    {
        return new EachPickOrder(
            Required(dto.OrderId, "each_pick.orders[].order_id"),
            Required(dto.WarehouseId, "each_pick.orders[].warehouse_id"),
            Required(dto.SkuId, "each_pick.orders[].sku_id"),
            dto.Quantity,
            Required(dto.PickFaceLocationId, "each_pick.orders[].pick_face_location_id"),
            Required(dto.PickStationId, "each_pick.orders[].pick_station_id"),
            Required(dto.StagingLocationId, "each_pick.orders[].staging_location_id"),
            dto.ReleasedAtMs);
    }

    private static EachPickInventoryItem ToEachPickInventoryItem(InventoryItemDto dto)
    {
        return new EachPickInventoryItem(
            Required(dto.InventoryId, "each_pick.inventory[].inventory_id"),
            Required(dto.SkuId, "each_pick.inventory[].sku_id"),
            dto.Quantity,
            Required(dto.LocationId, "each_pick.inventory[].location_id"),
            ParseInventoryStatus(Required(dto.Status, "each_pick.inventory[].status")));
    }

    private static string Required(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Required scenario JSON field is missing: {fieldName}.");
        }

        return value;
    }

    private static T Required<T>(T? value, string fieldName)
        where T : class
    {
        return value ?? throw new ArgumentException(
            $"Required scenario JSON section is missing: {fieldName}.");
    }

    private static InventoryStatus ParseInventoryStatus(string value)
    {
        var normalized = ToPascalCase(value);

        if (Enum.TryParse<InventoryStatus>(normalized, ignoreCase: true, out var status))
        {
            return status;
        }

        throw new ArgumentException($"Unsupported inventory status in scenario JSON: {value}.");
    }

    private static string ToPascalCase(string value)
    {
        return string.Concat(
            value.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }

    private sealed class WarehouseScenarioDto
    {
        [JsonPropertyName("scenario_id")]
        public string? ScenarioId { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("inbound")]
        public InboundScenarioDto? Inbound { get; set; }

        [JsonPropertyName("outbound")]
        public OutboundScenarioDto? Outbound { get; set; }

        [JsonPropertyName("each_pick")]
        public EachPickScenarioDto? EachPick { get; set; }
    }

    private sealed class InboundScenarioDto
    {
        [JsonPropertyName("scenario_id")]
        public string? ScenarioId { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("dock_count")]
        public int DockCount { get; set; }

        [JsonPropertyName("forklift_count")]
        public int ForkliftCount { get; set; }

        [JsonPropertyName("process")]
        public InboundProcessDto? Process { get; set; }

        [JsonPropertyName("receipts")]
        public List<InboundReceiptDto>? Receipts { get; set; }
    }

    private sealed class InboundProcessDto
    {
        [JsonPropertyName("unload_duration_ms")]
        public long UnloadDurationMs { get; set; }

        [JsonPropertyName("qc_duration_ms")]
        public long QcDurationMs { get; set; }

        [JsonPropertyName("putaway_duration_ms")]
        public long PutawayDurationMs { get; set; }
    }

    private sealed class InboundReceiptDto
    {
        [JsonPropertyName("receipt_id")]
        public string? ReceiptId { get; set; }

        [JsonPropertyName("warehouse_id")]
        public string? WarehouseId { get; set; }

        [JsonPropertyName("sku_id")]
        public string? SkuId { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("staging_location_id")]
        public string? StagingLocationId { get; set; }

        [JsonPropertyName("storage_location_id")]
        public string? StorageLocationId { get; set; }

        [JsonPropertyName("arrives_at_ms")]
        public long ArrivesAtMs { get; set; }
    }

    private sealed class OutboundScenarioDto
    {
        [JsonPropertyName("scenario_id")]
        public string? ScenarioId { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("worker_count")]
        public int WorkerCount { get; set; }

        [JsonPropertyName("dock_count")]
        public int DockCount { get; set; }

        [JsonPropertyName("process")]
        public OutboundProcessDto? Process { get; set; }

        [JsonPropertyName("inventory")]
        public List<InventoryItemDto>? Inventory { get; set; }

        [JsonPropertyName("orders")]
        public List<OutboundOrderDto>? Orders { get; set; }
    }

    private sealed class OutboundProcessDto
    {
        [JsonPropertyName("pick_duration_ms")]
        public long PickDurationMs { get; set; }

        [JsonPropertyName("stage_duration_ms")]
        public long StageDurationMs { get; set; }

        [JsonPropertyName("dock_travel_duration_ms")]
        public long DockTravelDurationMs { get; set; }

        [JsonPropertyName("load_duration_ms")]
        public long LoadDurationMs { get; set; }
    }

    private sealed class OutboundOrderDto
    {
        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        [JsonPropertyName("warehouse_id")]
        public string? WarehouseId { get; set; }

        [JsonPropertyName("sku_id")]
        public string? SkuId { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("pick_location_id")]
        public string? PickLocationId { get; set; }

        [JsonPropertyName("staging_location_id")]
        public string? StagingLocationId { get; set; }

        [JsonPropertyName("dock_id")]
        public string? DockId { get; set; }

        [JsonPropertyName("released_at_ms")]
        public long ReleasedAtMs { get; set; }
    }

    private sealed class EachPickScenarioDto
    {
        [JsonPropertyName("scenario_id")]
        public string? ScenarioId { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("station_count")]
        public int StationCount { get; set; }

        [JsonPropertyName("worker_count")]
        public int WorkerCount { get; set; }

        [JsonPropertyName("process")]
        public EachPickProcessDto? Process { get; set; }

        [JsonPropertyName("inventory")]
        public List<InventoryItemDto>? Inventory { get; set; }

        [JsonPropertyName("orders")]
        public List<EachPickOrderDto>? Orders { get; set; }
    }

    private sealed class EachPickProcessDto
    {
        [JsonPropertyName("tote_bind_duration_ms")]
        public long ToteBindDurationMs { get; set; }

        [JsonPropertyName("travel_to_station_duration_ms")]
        public long TravelToStationDurationMs { get; set; }

        [JsonPropertyName("pick_service_duration_ms")]
        public long PickServiceDurationMs { get; set; }

        [JsonPropertyName("move_to_staging_duration_ms")]
        public long MoveToStagingDurationMs { get; set; }
    }

    private sealed class EachPickOrderDto
    {
        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        [JsonPropertyName("warehouse_id")]
        public string? WarehouseId { get; set; }

        [JsonPropertyName("sku_id")]
        public string? SkuId { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("pick_face_location_id")]
        public string? PickFaceLocationId { get; set; }

        [JsonPropertyName("pick_station_id")]
        public string? PickStationId { get; set; }

        [JsonPropertyName("staging_location_id")]
        public string? StagingLocationId { get; set; }

        [JsonPropertyName("released_at_ms")]
        public long ReleasedAtMs { get; set; }
    }

    private sealed class InventoryItemDto
    {
        [JsonPropertyName("inventory_id")]
        public string? InventoryId { get; set; }

        [JsonPropertyName("sku_id")]
        public string? SkuId { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("location_id")]
        public string? LocationId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
