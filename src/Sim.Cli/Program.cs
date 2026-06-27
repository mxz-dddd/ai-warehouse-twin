using System.Text.Json;
using Sim.Contracts.Artifacts;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Json;
using Sim.Core.Scenarios.Samples;

WarehouseScenario scenario;
string? artifactOutputPath = null;

if (args.Length == 1 && args[0] == "sample-small-warehouse")
{
    scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
}
else if (args.Length == 2 && args[0] == "run-file")
{
    scenario = WarehouseScenarioJsonLoader.Load(args[1]);
}
else if (args.Length == 4 && args[0] == "export-artifact" && args[2] == "-o")
{
    scenario = WarehouseScenarioJsonLoader.Load(args[1]);
    artifactOutputPath = args[3];
}
else
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- sample-small-warehouse");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- run-file <scenario-json-path>");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- export-artifact <scenario-json-path> -o <output-json-path>");
    return 1;
}

var result = new WarehouseScenarioRunner().Run(scenario);

if (artifactOutputPath is not null)
{
    File.WriteAllText(
        artifactOutputPath,
        JsonSerializer.Serialize(
            ToRunArtifact(result),
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            }) + Environment.NewLine);

    Console.WriteLine($"Exported run artifact: {artifactOutputPath}");
    return 0;
}

Console.WriteLine(JsonSerializer.Serialize(
    ToPayload(result),
    new JsonSerializerOptions
    {
        WriteIndented = true
    }));

return 0;

static object ToPayload(WarehouseRunResult result)
{
    return new
    {
        scenario_id = result.ScenarioId,
        seed = result.Seed,
        started_at_ms = result.StartedAtMs,
        finished_at_ms = result.FinishedAtMs,
        completed_receipts = result.CompletedReceipts,
        completed_outbound_orders = result.CompletedOutboundOrders,
        completed_each_pick_orders = result.CompletedEachPickOrders,
        total_quantity_available = result.TotalQuantityAvailable,
        total_quantity_shipped = result.TotalQuantityShipped,
        total_quantity_picked = result.TotalQuantityPicked,
        final_world_time_ms = result.FinalWorldState.TimeMs,
        event_log_text = result.EventLogText,
        kpi_summary = new
        {
            total_duration_ms = result.KpiSummary.TotalDurationMs,
            total_completed_work_items = result.KpiSummary.TotalCompletedWorkItems,
            event_log_line_count = result.KpiSummary.EventLogLineCount,
            receipt_throughput_per_hour = RoundKpi(result.KpiSummary.ReceiptThroughputPerHour),
            outbound_order_throughput_per_hour = RoundKpi(result.KpiSummary.OutboundOrderThroughputPerHour),
            each_pick_order_throughput_per_hour = RoundKpi(result.KpiSummary.EachPickOrderThroughputPerHour),
            total_work_item_throughput_per_hour = RoundKpi(result.KpiSummary.TotalWorkItemThroughputPerHour)
        }
    };
}

static RunArtifact ToRunArtifact(WarehouseRunResult result)
{
    var kpi = result.KpiSummary;

    return new RunArtifact
    {
        SchemaVersion = RunArtifact.CurrentSchemaVersion,
        ArtifactKind = RunArtifact.CurrentArtifactKind,
        ScenarioId = result.ScenarioId,
        Seed = result.Seed,
        StartedAtMs = result.StartedAtMs,
        FinishedAtMs = result.FinishedAtMs,
        FinalWorldTimeMs = result.FinalWorldState.TimeMs,
        KpiSummary = new RunArtifactKpiSummary
        {
            TotalDurationMs = kpi.TotalDurationMs,
            TotalCompletedWorkItems = kpi.TotalCompletedWorkItems,
            EventLogLineCount = kpi.EventLogLineCount,
            ReceiptThroughputPerHour = RoundKpi(kpi.ReceiptThroughputPerHour),
            OutboundOrderThroughputPerHour = RoundKpi(kpi.OutboundOrderThroughputPerHour),
            EachPickOrderThroughputPerHour = RoundKpi(kpi.EachPickOrderThroughputPerHour),
            TotalWorkItemThroughputPerHour = RoundKpi(kpi.TotalWorkItemThroughputPerHour)
        },
        EventLog = result.EventLogText
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0)
            .ToArray()
    };
}

static decimal RoundKpi(decimal value)
{
    return Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
