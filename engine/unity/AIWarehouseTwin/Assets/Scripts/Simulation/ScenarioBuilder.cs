using System;
using System.Collections.Generic;
using System.Globalization;
using Sim.Contracts.Artifacts;

namespace AIWarehouseTwin.Simulation
{
    /// <summary>
    /// Builds a deterministic RunArtifact from Unity demo configuration inputs.
    /// </summary>
    public static class ScenarioBuilder
    {
        /// <summary>
        /// Seam-1 bridge for the Unity demo. This currently constructs a deterministic
        /// Sim.Contracts demo artifact from inspector inputs; a future task can replace
        /// this boundary with a reviewed real simulation runner integration.
        /// </summary>
        /// <param name="cfg">Warehouse inputs captured from Unity.</param>
        /// <returns>A deterministic RunArtifact suitable for demo handoff tests.</returns>
        public static RunArtifact Build(WarehouseConfig cfg)
        {
            if (cfg == null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            ValidateGreaterThanZero(cfg.lengthM, nameof(cfg.lengthM));
            ValidateGreaterThanZero(cfg.widthM, nameof(cfg.widthM));
            ValidateAtLeast(cfg.shelfRows, 1, nameof(cfg.shelfRows));
            ValidateAtLeast(cfg.skuCount, 1, nameof(cfg.skuCount));
            ValidateAtLeast(cfg.workerCount, 1, nameof(cfg.workerCount));
            ValidateAtLeast(cfg.forkliftCount, 0, nameof(cfg.forkliftCount));
            ValidateAtLeast(cfg.orderCount, 1, nameof(cfg.orderCount));

            var scenarioId = CreateScenarioId(cfg);
            var durationMs = CalculateDurationMs(cfg);
            var eventLog = BuildEventLog(cfg, scenarioId);

            return new RunArtifact
            {
                SchemaVersion = RunArtifact.CurrentSchemaVersion,
                ArtifactKind = RunArtifact.CurrentArtifactKind,
                ScenarioId = scenarioId,
                Seed = 0,
                StartedAtMs = 0,
                FinishedAtMs = durationMs,
                FinalWorldTimeMs = durationMs,
                KpiSummary = new RunArtifactKpiSummary
                {
                    TotalDurationMs = durationMs,
                    TotalCompletedWorkItems = cfg.orderCount,
                    EventLogLineCount = eventLog.Count,
                    ReceiptThroughputPerHour = 0m,
                    OutboundOrderThroughputPerHour = ThroughputPerHour(cfg.orderCount, durationMs),
                    EachPickOrderThroughputPerHour = 0m,
                    TotalWorkItemThroughputPerHour = ThroughputPerHour(cfg.orderCount, durationMs),
                },
                Layout = new RunArtifactLayout
                {
                    Resources = BuildResources(cfg),
                },
                PositionTimeline = Array.Empty<RunArtifactPositionTimelineEntry>(),
                EventLog = eventLog,
            };
        }

        private static void ValidateGreaterThanZero(float value, string name)
        {
            if (!(value > 0f))
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    value,
                    $"{name} must be greater than zero.");
            }
        }

        private static void ValidateAtLeast(int value, int minimum, string name)
        {
            if (value < minimum)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    value,
                    $"{name} must be greater than or equal to {minimum}.");
            }
        }

        private static string CreateScenarioId(WarehouseConfig cfg)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "unity-demo-{0:0.###}x{1:0.###}-r{2}-sku{3}-w{4}-f{5}-o{6}",
                cfg.lengthM,
                cfg.widthM,
                cfg.shelfRows,
                cfg.skuCount,
                cfg.workerCount,
                cfg.forkliftCount,
                cfg.orderCount);
        }

        private static long CalculateDurationMs(WarehouseConfig cfg)
        {
            return (cfg.orderCount * 1_000L)
                + (cfg.skuCount * 10L)
                + (cfg.shelfRows * 100L)
                + (cfg.workerCount * 50L)
                + (cfg.forkliftCount * 50L);
        }

        private static IReadOnlyList<RunArtifactLayoutResource> BuildResources(WarehouseConfig cfg)
        {
            var resources = new List<RunArtifactLayoutResource>(
                cfg.workerCount + cfg.forkliftCount);

            for (var i = 0; i < cfg.workerCount; i++)
            {
                var index = i + 1;
                resources.Add(new RunArtifactLayoutResource(
                    $"worker-{index:000}",
                    $"worker-home-{index:000}",
                    ClampToWidth(index, cfg.workerCount, cfg.lengthM),
                    1m));
            }

            for (var i = 0; i < cfg.forkliftCount; i++)
            {
                var index = i + 1;
                resources.Add(new RunArtifactLayoutResource(
                    $"forklift-{index:000}",
                    $"forklift-home-{index:000}",
                    ClampToWidth(index, Math.Max(1, cfg.forkliftCount), cfg.lengthM),
                    Math.Max(1m, (decimal)cfg.widthM - 1m)));
            }

            return resources;
        }

        private static IReadOnlyList<string> BuildEventLog(WarehouseConfig cfg, string scenarioId)
        {
            return new[]
            {
                $"scenario_id={scenarioId}",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "dimensions_m={0:0.###}x{1:0.###};shelf_rows={2}",
                    cfg.lengthM,
                    cfg.widthM,
                    cfg.shelfRows),
                $"sku_count={cfg.skuCount};order_count={cfg.orderCount}",
                $"worker_count={cfg.workerCount};forklift_count={cfg.forkliftCount}",
            };
        }

        private static decimal ClampToWidth(int index, int count, float lengthM)
        {
            var spacing = (decimal)lengthM / (count + 1);
            return decimal.Round(spacing * index, 3);
        }

        private static decimal ThroughputPerHour(int completedWorkItems, long durationMs)
        {
            return decimal.Round(completedWorkItems * 3_600_000m / durationMs, 3);
        }
    }
}
