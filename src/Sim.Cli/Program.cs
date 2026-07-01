using System.Text.Json;
using Sim.Contracts.Artifacts;
using Sim.Core.Movement;
using Sim.Core.Scenarios;
using Sim.Core.Scenarios.Unified;
using Sim.Core.Scenarios.Json;
using Sim.Core.Scenarios.Samples;
using Sim.Core.Spatial;
using Sim.Report;

if (args.Length > 0 &&
    args[0] == "export-movement-artifact")
{
    return ExportMovementArtifact(args);
}

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
string? artifactScenarioPath = null;
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
    artifactScenarioPath = args[1];
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
    artifactScenarioPath = args[1];
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
    Console.Error.WriteLine("  dotnet run --project src/Sim.Cli -- export-movement-artifact <scenario-json-path> -o <output-json-path>");
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
    var warehouseGraph = artifactScenarioPath is null
        ? null
        : TryLoadWarehouseGraph(artifactScenarioPath);
    var artifactJson = JsonSerializer.Serialize(
        useUnifiedRunner
            ? ToUnifiedRunArtifact(scenario, runner, warehouseGraph)
            : ToRunArtifact(runner.RunWithTrace(scenario), warehouseGraph),
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

static int ExportMovementArtifact(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine(
            "Usage: dotnet run --project src/Sim.Cli -- export-movement-artifact <scenario-json-path> -o <output-json-path>");
        return 1;
    }

    var scenarioPath = args[1];
    string? outputPath = null;
    string? runId = null;
    var sourceRunArtifact = "not-provided";
    var graphSource = "cli-r2b-scenario-layout";
    var generatorVersion = "cli-r2b";

    for (var i = 2; i < args.Length; i++)
    {
        var argument = args[i];
        if (argument is "-o" or "--output")
        {
            if (!TryReadOptionValue(args, ref i, argument, out outputPath))
            {
                return 1;
            }

            continue;
        }

        if (argument == "--run-id")
        {
            if (!TryReadOptionValue(args, ref i, argument, out runId))
            {
                return 1;
            }

            continue;
        }

        if (argument == "--source-run-artifact")
        {
            if (!TryReadOptionValue(args, ref i, argument, out sourceRunArtifact))
            {
                return 1;
            }

            continue;
        }

        if (argument == "--graph-source")
        {
            if (!TryReadOptionValue(args, ref i, argument, out graphSource))
            {
                return 1;
            }

            continue;
        }

        if (argument == "--generator-version")
        {
            if (!TryReadOptionValue(args, ref i, argument, out generatorVersion))
            {
                return 1;
            }

            continue;
        }

        Console.Error.WriteLine(
            $"Unknown export-movement-artifact option '{argument}'.");
        return 1;
    }

    if (string.IsNullOrWhiteSpace(outputPath))
    {
        Console.Error.WriteLine(
            "export-movement-artifact requires -o or --output.");
        return 1;
    }

    try
    {
        var scenario = WarehouseScenarioJsonLoader.Load(scenarioPath);
        var request = MovementArtifactInputAdapter.FromScenario(
            scenario,
            new MovementArtifactInputAdapterOptions(
                sourceRunArtifact,
                graphSource,
                generatorVersion,
                runId));
        var movementArtifactJson = JsonSerializer.Serialize(
            MovementArtifactGenerator.Generate(request),
            ArtifactJsonOptions());

        File.WriteAllText(
            outputPath,
            NormalizeLineEndings(movementArtifactJson) + "\n");

        Console.WriteLine($"Exported movement artifact: {outputPath}");
        return 0;
    }
    catch (Exception exception) when (
        exception is ArgumentException or
        InvalidOperationException or
        IOException or
        UnauthorizedAccessException)
    {
        Console.Error.WriteLine(
            $"Failed to export movement artifact: {exception.Message}");
        return 1;
    }
}

static bool TryReadOptionValue(
    string[] args,
    ref int index,
    string optionName,
    out string value)
{
    if (index + 1 >= args.Length)
    {
        Console.Error.WriteLine($"{optionName} requires a value.");
        value = string.Empty;
        return false;
    }

    var candidate = args[index + 1];
    if (string.IsNullOrWhiteSpace(candidate))
    {
        Console.Error.WriteLine($"{optionName} requires a non-empty value.");
        value = string.Empty;
        return false;
    }

    index++;
    value = candidate;
    return true;
}

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
    WarehouseScenarioRunner runner,
    RunArtifactWarehouseGraph? warehouseGraph)
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
        unifiedScenario.Operations,
        warehouseGraph);
}

static RunArtifact ToRunArtifact(
    WarehouseScenarioTraceResult traceResult,
    RunArtifactWarehouseGraph? warehouseGraph = null)
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
        KpiSummary = ToRunArtifactKpiSummary(kpi),
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
        WarehouseGraph = warehouseGraph,
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
    IReadOnlyList<WarehouseUnifiedOperation> operations,
    RunArtifactWarehouseGraph? warehouseGraph = null)
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
        KpiSummary = ToRunArtifactKpiSummary(kpi, unifiedResult.RichKpiSummary),
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
        WarehouseGraph = warehouseGraph,
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

static RunArtifactWarehouseGraph? TryLoadWarehouseGraph(string scenarioJsonPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(scenarioJsonPath));
    var root = document.RootElement;
    if (root.ValueKind != JsonValueKind.Object)
    {
        return null;
    }

    if (TryGetLayoutGraphElement(root, out var layoutGraphElement))
    {
        return ToRunArtifactWarehouseGraph(
            LayoutGraphLoader.Load(layoutGraphElement.GetRawText()));
    }

    return null;
}

static bool TryGetLayoutGraphElement(JsonElement root, out JsonElement layoutGraphElement)
{
    if (root.TryGetProperty("layout_graph", out layoutGraphElement) &&
        HasLayoutGraphShape(layoutGraphElement))
    {
        return true;
    }

    if (root.TryGetProperty("layoutGraph", out layoutGraphElement) &&
        HasLayoutGraphShape(layoutGraphElement))
    {
        return true;
    }

    if (root.TryGetProperty("layout", out layoutGraphElement) &&
        HasLayoutGraphShape(layoutGraphElement))
    {
        return true;
    }

    layoutGraphElement = default;
    return false;
}

static bool HasLayoutGraphShape(JsonElement element)
{
    return element.ValueKind == JsonValueKind.Object &&
           element.TryGetProperty("nodes", out var nodes) &&
           nodes.ValueKind == JsonValueKind.Array &&
           element.TryGetProperty("edges", out var edges) &&
           edges.ValueKind == JsonValueKind.Array;
}

static RunArtifactWarehouseGraph ToRunArtifactWarehouseGraph(PathGraph graph)
{
    return new RunArtifactWarehouseGraph
    {
        Nodes = graph.Nodes
            .Select(node => new RunArtifactWarehouseGraphNode
            {
                NodeId = node.NodeId,
                NodeType = node.NodeType,
                X = node.XMm,
                Y = node.YMm
            })
            .ToArray(),
        Edges = graph.Edges
            .Select(edge => new RunArtifactWarehouseGraphEdge
            {
                EdgeId = edge.EdgeId,
                FromNodeId = edge.FromNodeId,
                ToNodeId = edge.ToNodeId,
                DistanceM = MmToMeters(edge.DistanceMm),
                TravelTimeMs = 0,
                Bidirectional = edge.Bidirectional
            })
            .ToArray()
    };
}

static decimal MmToMeters(long distanceMm)
{
    return distanceMm / 1000m;
}

static RunArtifactKpiSummary ToRunArtifactKpiSummary(
    WarehouseKpiSummary kpi,
    WarehouseUnifiedRichKpiSummary? richKpi = null)
{
    var summary = new RunArtifactKpiSummary
    {
        TotalDurationMs = kpi.TotalDurationMs,
        TotalCompletedWorkItems = kpi.TotalCompletedWorkItems,
        EventLogLineCount = kpi.EventLogLineCount,
        ReceiptThroughputPerHour = RoundKpi(kpi.ReceiptThroughputPerHour),
        OutboundOrderThroughputPerHour = RoundKpi(kpi.OutboundOrderThroughputPerHour),
        EachPickOrderThroughputPerHour = RoundKpi(kpi.EachPickOrderThroughputPerHour),
        TotalWorkItemThroughputPerHour = RoundKpi(kpi.TotalWorkItemThroughputPerHour)
    };

    if (richKpi is null)
    {
        return summary;
    }

    return summary with
    {
        OrderCycleP50Ms = ToDecimal(richKpi.OrderCycleP50Ms),
        OrderCycleP90Ms = ToDecimal(richKpi.OrderCycleP90Ms),
        OrderCycleP95Ms = ToDecimal(richKpi.OrderCycleP95Ms),
        AvgWaitMs = RoundOptionalKpi(richKpi.AverageWaitMs),
        ResourceUtilization = ToPercentByKey(richKpi.ResourceUtilization),
        Bottlenecks = richKpi.Bottlenecks
            .Select(bottleneck => new RunArtifactKpiBottleneck
            {
                Rank = bottleneck.Rank,
                ResourceId = bottleneck.ResourceId,
                ResourceType = bottleneck.ResourceType,
                AvgWaitMs = RoundKpi(bottleneck.AverageWaitingTimeMs),
                TotalWaitMs = bottleneck.TotalWaitingTimeMs,
                Utilization = RoundKpi(bottleneck.Utilization * 100m)
            })
            .ToArray(),
        TravelDistanceMByActorType = ToSortedDictionary(richKpi.TravelDistanceMByActorType)
    };
}

static decimal? ToDecimal(long? value)
{
    return value is null ? null : value.Value;
}

static IReadOnlyDictionary<string, decimal> ToPercentByKey(
    IReadOnlyDictionary<string, decimal> ratios)
{
    var result = new SortedDictionary<string, decimal>(StringComparer.Ordinal);

    foreach (var entry in ratios.OrderBy(entry => entry.Key, StringComparer.Ordinal))
    {
        result[entry.Key] = RoundKpi(entry.Value * 100m);
    }

    return result;
}

static IReadOnlyDictionary<string, decimal> ToSortedDictionary(
    IReadOnlyDictionary<string, decimal> values)
{
    var result = new SortedDictionary<string, decimal>(StringComparer.Ordinal);

    foreach (var entry in values.OrderBy(entry => entry.Key, StringComparer.Ordinal))
    {
        result[entry.Key] = entry.Value;
    }

    return result;
}

static decimal? RoundOptionalKpi(decimal? value)
{
    return value is null ? null : RoundKpi(value.Value);
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
