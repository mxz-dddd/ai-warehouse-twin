using System.Text.Json;
using Sim.Contracts.Artifacts;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Json;
using Sim.Core.Scenarios.Samples;

if (args.Length == 5 &&
    args[0] == "compare-files" &&
    args[3] == "-o")
{
    var baseline = WarehouseScenarioJsonLoader.Load(args[1]);
    var candidate = WarehouseScenarioJsonLoader.Load(args[2]);
    var comparison = new WarehouseScenarioComparisonRunner().Compare(
        baseline,
        candidate);
    var comparisonJson = JsonSerializer.Serialize(
        ToComparisonArtifact(comparison),
        ArtifactJsonOptions());

    File.WriteAllText(
        args[4],
        NormalizeLineEndings(comparisonJson) + "\n");

    Console.WriteLine($"Exported comparison artifact: {args[4]}");
    return 0;
}

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
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- compare-files <baseline-scenario-json-path> <candidate-scenario-json-path> -o <output-json-path>");
    return 1;
}

var runner = new WarehouseScenarioRunner();

if (artifactOutputPath is not null)
{
    var traceResult = runner.RunWithTrace(scenario);
    var artifactJson = JsonSerializer.Serialize(
        ToRunArtifact(traceResult),
        ArtifactJsonOptions());

    File.WriteAllText(
        artifactOutputPath,
        NormalizeLineEndings(artifactJson) + "\n");

    Console.WriteLine($"Exported run artifact: {artifactOutputPath}");
    return 0;
}

var result = runner.Run(scenario);

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

static RunArtifact ToRunArtifact(WarehouseScenarioTraceResult traceResult)
{
    var result = traceResult.RunResult;
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
        Layout = new RunArtifactLayout
        {
            Resources = traceResult.Layout.Resources
                .Select(resource => new RunArtifactLayoutResource(
                    resource.ResourceId,
                    resource.Position.NodeId,
                    resource.Position.X,
                    resource.Position.Y))
                .ToArray()
        },
        PositionTimeline = traceResult.PositionTimeline
            .Select(entry => new RunArtifactPositionTimelineEntry(
                entry.OperationId,
                entry.OperationType,
                entry.StageType,
                entry.ResourceId,
                entry.AtMs,
                entry.EventType,
                entry.Position.NodeId,
                entry.Position.X,
                entry.Position.Y))
            .ToArray(),
        EventLog = NormalizeLineEndings(result.EventLogText)
            .Split('\n')
            .Where(line => line.Length > 0)
            .ToArray()
    };
}

static ComparisonArtifact ToComparisonArtifact(
    WarehouseScenarioComparisonResult result)
{
    return new ComparisonArtifact(
        ComparisonArtifact.CurrentSchemaVersion,
        ToComparisonScenarioSummary(
            result.BaselineScenarioId,
            result.BaselineMetrics),
        ToComparisonScenarioSummary(
            result.CandidateScenarioId,
            result.CandidateMetrics),
        result.Deltas
            .Select(delta => new ComparisonArtifactDelta(
                delta.MetricName,
                RoundKpi(delta.BaselineValue),
                RoundKpi(delta.CandidateValue),
                RoundKpi(delta.Delta),
                delta.DeltaPercent is null
                    ? null
                    : RoundKpi(delta.DeltaPercent.Value),
                delta.Direction))
            .ToArray());
}

static ComparisonArtifactScenarioSummary ToComparisonScenarioSummary(
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
            RoundKpi(metrics.InboundReceiptThroughputPerHour),
            RoundKpi(metrics.OutboundOrderThroughputPerHour),
            RoundKpi(metrics.EachPickOrderThroughputPerHour),
            RoundKpi(metrics.TotalWorkItemThroughputPerHour)));
}

static JsonSerializerOptions ArtifactJsonOptions()
{
    return new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

static string NormalizeLineEndings(string value)
{
    return value.Replace("\r\n", "\n").Replace('\r', '\n');
}

static decimal RoundKpi(decimal value)
{
    return Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
