using Sim.Core.Resources;

namespace Sim.Core.Scenarios;

public sealed record WarehouseScenarioTraceResult(
    WarehouseRunResult RunResult,
    IReadOnlyList<ResourceLeaseTimelineEntry> ResourceLeaseTimeline);
