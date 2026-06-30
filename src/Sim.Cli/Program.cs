using System.Text.Json;
using Sim.Contracts.Artifacts;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Unified;
using Sim.Core.Scenarios.Json;
using Sim.Core.Scenarios.Samples;
using Sim.Report;

if (args.Length == 5 &&
    args[0] == "render-report" &&
    args[3] == "-o")
{
    var markdown = CustomerReportService.RenderFromFiles(
        args[1],
        args[2]);

    File.WriteAllText(
        args[4],
        NormalizeMarkdown(markdown));

    Console.WriteLine($"Exported customer report: {args[4]}");
    return 0;
}

if (args.Length == 9 &&
    args[0] == "render-report" &&
    args[3] == "-o" &&
    args[5] == "--run-runner" &&
    args[7] == "--comparison-runner")
{
    if (!TryParseRunnerProvenanceMode(args[6], out var runRunnerMode, out var runRunnerErrorMessage))
    {
        Console.Error.WriteLine(runRunnerErrorMessage);
        return 1;
    }

    if (!TryParseRunnerProvenanceMode(args[8], out var comparisonRunnerMode, out var comparisonRunnerErrorMessage))
    {
        Console.Error.WriteLine(comparisonRunnerErrorMessage);
        return 1;
    }

    var markdown = CustomerReportService.RenderFromFiles(
        args[1],
        args[2],
        new CustomerReportRenderOptions(
            runRunnerMode,
            comparisonRunnerMode));

    File.WriteAllText(
        args[4],
        NormalizeMarkdown(markdown));

    Console.WriteLine($"Exported customer report: {args[4]}");
    return 0;
}

if (args.Length > 5 &&
    args[0] == "render-report" &&
    (args.Contains("--run-runner") ||
     args.Contains("--comparison-runner")))
{
    Console.Error.WriteLine(
        "Both --run-runner and --comparison-runner are required when providing runner provenance.");
    return 1;
}

if (args.Length == 5 &&
    args[0] == "compare-files" &&
    args[3] == "-o")
{
    var baseline = WarehouseScenarioJsonLoader.Load(args[1]);
    var candidate = WarehouseScenarioJsonLoader.Load(args[2]);
    var comparison = new WarehouseScenarioComparisonRunner().CompareWithUnifiedAdapter(
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

if (args.Length == 7 &&
    args[0] == "compare-files" &&
    args[3] == "-o" &&
    args[5] == "--runner")
{
    if (!TryParseRunnerMode(args[6], out var useUnifiedComparisonRunner, out var errorMessage))
    {
        Console.Error.WriteLine(errorMessage);
        return 1;
    }

    var baseline = WarehouseScenarioJsonLoader.Load(args[1]);
    var candidate = WarehouseScenarioJsonLoader.Load(args[2]);
    var comparisonRunner = new WarehouseScenarioComparisonRunner();
    var comparison = useUnifiedComparisonRunner
        ? comparisonRunner.CompareWithUnifiedAdapter(baseline, candidate)
        : comparisonRunner.Compare(baseline, candidate);
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
var useUnifiedRunner = true;

if (args.Length == 1 && args[0] == "sample-small-warehouse")
{
    scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
}
else if (args.Length == 3 &&
         args[0] == "sample-small-warehouse" &&
         args[1] == "--runner")
{
    if (!TryParseRunnerMode(args[2], out useUnifiedRunner, out var errorMessage))
    {
        Console.Error.WriteLine(errorMessage);
        return 1;
    }

    scenario = WarehouseSampleScenarioFactory.CreateSmallWarehouse();
}
else if (args.Length == 2 && args[0] == "run-file")
{
    scenario = WarehouseScenarioJsonLoader.Load(args[1]);
}
else if (args.Length == 4 &&
         args[0] == "run-file" &&
         args[2] == "--runner")
{
    if (!TryParseRunnerMode(args[3], out useUnifiedRunner, out var errorMessage))
    {
        Console.Error.WriteLine(errorMessage);
        return 1;
    }

    scenario = WarehouseScenarioJsonLoader.Load(args[1]);
}
else if (args.Length == 4 && args[0] == "export-artifact" && args[2] == "-o")
{
    scenario = WarehouseScenarioJsonLoader.Load(args[1]);
    artifactOutputPath = args[3];
}
else if (args.Length == 6 &&
         args[0] == "export-artifact" &&
         args[2] == "-o" &&
         args[4] == "--runner")
{
    if (!TryParseRunnerMode(args[5], out useUnifiedRunner, out var errorMessage))
    {
        Console.Error.WriteLine(errorMessage);
        return 1;
    }

    scenario = WarehouseScenarioJsonLoader.Load(args[1]);
    artifactOutputPath = args[3];
}
else
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- sample-small-warehouse");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- sample-small-warehouse --runner <legacy|unified>");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- run-file <scenario-json-path>");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- run-file <scenario-json-path> --runner <legacy|unified>");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- export-artifact <scenario-json-path> -o <output-json-path>");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- export-artifact <scenario-json-path> -o <output-json-path> --runner <legacy|unified>");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- compare-files <baseline-scenario-json-path> <candidate-scenario-json-path> -o <output-json-path>");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- compare-files <baseline-scenario-json-path> <candidate-scenario-json-path> -o <output-json-path> --runner <legacy|unified>");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- render-report <run-artifact-json-path> <comparison-artifact-json-path> -o <output-md-path>");
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- render-report <run-artifact-json-path> <comparison-artifact-json-path> -o <output-md-path> --run-runner <legacy|unified|unknown> --comparison-runner <legacy|unified|unknown>");
    Console.Error.WriteLine("Defaults: sample-small-warehouse, run-file, export-artifact, and compare-files use the unified runner. Use --runner legacy to reproduce pre-unified outputs.");
    return 1;
}

var runner = new WarehouseScenarioRunner();

if (artifactOutputPath is not null)
{
    var artifactJson = JsonSerializer.Serialize(
        useUnifiedRunner
            ? ToUnifiedRunArtifact(scenario, runner)
            : ToRunArtifact(runner.RunWithTrace(scenario)),
        ArtifactJsonOptions());

    File.WriteAllText(
        artifactOutputPath,
        NormalizeLineEndings(artifactJson) + "\n");

    Console.WriteLine($"Exported run artifact: {artifactOutputPath}");
    return 0;
}

var result = useUnifiedRunner
    ? runner.RunWithUnifiedAdapter(scenario)
    : runner.Run(scenario);

Console.WriteLine(JsonSerializer.Serialize(
    useUnifiedRunner
        ? ToPayloadWithRunnerMode(result, runnerMode: "unified")
        : ToPayload(result),
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

static object ToPayloadWithRunnerMode(WarehouseRunResult result, string runnerMode)
{
    return new
    {
        runner_mode = runnerMode,
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

static RunArtifact ToUnifiedRunArtifact(
    WarehouseScenario scenario,
    WarehouseScenarioRunner runner)
{
    var unifiedScenario =
        WarehouseScenarioToUnifiedScenarioAdapter.Convert(scenario);
    var result = runner.RunUnified(unifiedScenario);
    var unifiedOperationResult = new WarehouseUnifiedOperationRunner().Run(
        unifiedScenario.InitialInventory,
        unifiedScenario.Operations);

    return ToRunArtifactFromUnifiedResult(
        result,
        unifiedOperationResult,
        unifiedScenario.Operations);
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

static RunArtifact ToRunArtifactFromUnifiedResult(
    WarehouseRunResult result,
    WarehouseUnifiedOperationResult unifiedResult,
    IReadOnlyList<WarehouseUnifiedOperation> operations)
{
    var kpi = result.KpiSummary;
    var operationsById = operations.ToDictionary(
        operation => operation.OperationId,
        StringComparer.Ordinal);

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
            Resources = unifiedResult.PositionTimeline
                .GroupBy(entry => entry.ResourceId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var position = group
                        .OrderBy(entry => entry.AtMs)
                        .ThenBy(entry => entry.OperationId, StringComparer.Ordinal)
                        .ThenBy(entry => entry.EventType, StringComparer.Ordinal)
                        .First()
                        .Position;

                    return new RunArtifactLayoutResource(
                        group.Key,
                        position.NodeId,
                        position.X,
                        position.Y);
                })
                .ToArray()
        },
        // CORE-U3c preserves RunArtifact v1 and maps the opt-in unified
        // runner's deterministic layout handoff into the existing position
        // timeline contract. These coordinates are baseline layout positions,
        // NOT simulated movement.
        PositionTimeline = unifiedResult.PositionTimeline
            .Select(entry =>
            {
                var operation = operationsById[entry.OperationId];
                return new RunArtifactPositionTimelineEntry(
                    entry.OperationId,
                    ToArtifactOperationType(operation.OperationType),
                    "operation",
                    entry.ResourceId,
                    entry.AtMs,
                    entry.EventType,
                    entry.Position.NodeId,
                    entry.Position.X,
                    entry.Position.Y);
            })
            .ToArray(),
        EventLog = NormalizeLineEndings(result.EventLogText)
            .Split('\n')
            .Where(line => line.Length > 0)
            .ToArray()
    };
}

static string ToArtifactOperationType(
    WarehouseUnifiedOperationType operationType)
{
    return operationType switch
    {
        WarehouseUnifiedOperationType.Inbound => "inbound",
        WarehouseUnifiedOperationType.Outbound => "outbound",
        WarehouseUnifiedOperationType.EachPick => "each_pick",
        _ => throw new ArgumentOutOfRangeException(
            nameof(operationType),
            operationType,
            "Unsupported warehouse unified operation type.")
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

static string NormalizeMarkdown(string value)
{
    return NormalizeLineEndings(value).TrimEnd('\n') + "\n";
}

static decimal RoundKpi(decimal value)
{
    return Math.Round(value, 3, MidpointRounding.AwayFromZero);
}

static bool TryParseRunnerMode(
    string value,
    out bool useUnifiedRunner,
    out string errorMessage)
{
    if (string.Equals(value, "legacy", StringComparison.OrdinalIgnoreCase))
    {
        useUnifiedRunner = false;
        errorMessage = string.Empty;
        return true;
    }

    if (string.Equals(value, "unified", StringComparison.OrdinalIgnoreCase))
    {
        useUnifiedRunner = true;
        errorMessage = string.Empty;
        return true;
    }

    useUnifiedRunner = false;
    errorMessage =
        $"Unknown runner mode '{value}'. Allowed values: legacy, unified.";
    return false;
}

static bool TryParseRunnerProvenanceMode(
    string value,
    out string runnerMode,
    out string errorMessage)
{
    if (string.Equals(value, "legacy", StringComparison.OrdinalIgnoreCase))
    {
        runnerMode = "legacy";
        errorMessage = string.Empty;
        return true;
    }

    if (string.Equals(value, "unified", StringComparison.OrdinalIgnoreCase))
    {
        runnerMode = "unified";
        errorMessage = string.Empty;
        return true;
    }

    if (string.Equals(value, "unknown", StringComparison.OrdinalIgnoreCase))
    {
        runnerMode = "unknown";
        errorMessage = string.Empty;
        return true;
    }

    runnerMode = string.Empty;
    errorMessage =
        $"Unknown runner mode '{value}'. Allowed values: legacy, unified, unknown.";
    return false;
}
