using System.Text.Json;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Samples;

if (args.Length != 1 || args[0] != "sample-small-warehouse")
{
    Console.Error.WriteLine("Usage: dotnet run --project src/Sim.Cli -- sample-small-warehouse");
    return 1;
}

var scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
var result = new WarehouseScenarioRunner().Run(scenario);

var payload = new
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
    event_log_text = result.EventLogText
};

Console.WriteLine(JsonSerializer.Serialize(
    payload,
    new JsonSerializerOptions
    {
        WriteIndented = true
    }));

return 0;
