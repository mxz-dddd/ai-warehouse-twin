using System;
using System.Collections.Generic;

namespace Sim.Contracts.Artifacts
{
    /// <summary>
    /// Unity-compatible RunArtifact v1 bridge for the demo seam.
    /// </summary>
    public sealed record RunArtifact
    {
        public const string CurrentSchemaVersion = "run-artifact.v1";
        public const string CurrentArtifactKind = "warehouse-simulation-run";

        public string SchemaVersion { get; init; } = string.Empty;

        public string ArtifactKind { get; init; } = string.Empty;

        public string ScenarioId { get; init; } = string.Empty;

        public int Seed { get; init; }

        public long StartedAtMs { get; init; }

        public long FinishedAtMs { get; init; }

        public long FinalWorldTimeMs { get; init; }

        public RunArtifactKpiSummary KpiSummary { get; init; } = new();

        public RunArtifactLayout Layout { get; init; } = RunArtifactLayout.Empty;

        public IReadOnlyList<RunArtifactPositionTimelineEntry> PositionTimeline { get; init; } =
            Array.Empty<RunArtifactPositionTimelineEntry>();

        public IReadOnlyList<string> EventLog { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Unity-compatible KPI summary subset for RunArtifact v1.
    /// </summary>
    public sealed record RunArtifactKpiSummary
    {
        public long TotalDurationMs { get; init; }

        public int TotalCompletedWorkItems { get; init; }

        public int EventLogLineCount { get; init; }

        public decimal ReceiptThroughputPerHour { get; init; }

        public decimal OutboundOrderThroughputPerHour { get; init; }

        public decimal EachPickOrderThroughputPerHour { get; init; }

        public decimal TotalWorkItemThroughputPerHour { get; init; }
    }

    /// <summary>
    /// Unity-compatible layout resource collection for RunArtifact v1.
    /// </summary>
    public sealed record RunArtifactLayout
    {
        public static RunArtifactLayout Empty { get; } = new()
        {
            Resources = Array.Empty<RunArtifactLayoutResource>(),
        };

        public IReadOnlyList<RunArtifactLayoutResource> Resources { get; init; } =
            Array.Empty<RunArtifactLayoutResource>();
    }

    /// <summary>
    /// Unity-compatible static layout resource for RunArtifact v1.
    /// </summary>
    public sealed record RunArtifactLayoutResource
    {
        public RunArtifactLayoutResource(
            string resourceId,
            string nodeId,
            decimal x,
            decimal y)
        {
            ResourceId = resourceId;
            NodeId = nodeId;
            X = x;
            Y = y;
        }

        public string ResourceId { get; }

        public string NodeId { get; }

        public decimal X { get; }

        public decimal Y { get; }
    }

    /// <summary>
    /// Unity-compatible position timeline entry for RunArtifact v1.
    /// </summary>
    public sealed record RunArtifactPositionTimelineEntry
    {
        public RunArtifactPositionTimelineEntry(
            string operationId,
            string operationType,
            string stageType,
            string resourceId,
            long atMs,
            string eventType,
            string nodeId,
            decimal x,
            decimal y)
        {
            OperationId = operationId;
            OperationType = operationType;
            StageType = stageType;
            ResourceId = resourceId;
            AtMs = atMs;
            EventType = eventType;
            NodeId = nodeId;
            X = x;
            Y = y;
        }

        public string OperationId { get; }

        public string OperationType { get; }

        public string StageType { get; }

        public string ResourceId { get; }

        public long AtMs { get; }

        public string EventType { get; }

        public string NodeId { get; }

        public decimal X { get; }

        public decimal Y { get; }
    }
}
