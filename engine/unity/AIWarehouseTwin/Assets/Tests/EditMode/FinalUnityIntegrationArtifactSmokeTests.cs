using System;
using System.IO;
using System.Linq;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Rendering.Actors;
using AIWarehouseTwin.Rendering.Layout;
using AIWarehouseTwin.UI.Showcase;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class FinalUnityIntegrationSmokeTests
    {
        private const string BaselineRunArtifact =
            "datasets/medium-warehouse/artifacts/run-artifact.v1.json";
        private const string BaselineMovementArtifact =
            "datasets/medium-warehouse/artifacts/movement-artifact.v1.json";
        private const string ComparisonArtifact =
            "datasets/medium-warehouse/optimized/artifacts/comparison-artifact.v1.json";
        private const string RealComparisonSourceLabel = "A5b deterministic comparison artifact";

        [Test]
        public void Final_integration_smoke_loads_medium_artifacts_and_builds_unity_models()
        {
            var runArtifact = RunArtifactLoader.LoadFromFile(RepoPath(BaselineRunArtifact));
            var movementArtifact = MovementArtifactLoader.LoadFromFile(RepoPath(BaselineMovementArtifact));
            var comparisonArtifact = ComparisonArtifactLoader.LoadFromFile(RepoPath(ComparisonArtifact));

            AssertRunArtifact(runArtifact);

            var layout = WarehouseLayoutRenderModelBuilder.Build(runArtifact);
            Assert.That(layout.IsEmpty, Is.False);
            Assert.That(layout.Nodes, Has.Count.EqualTo(runArtifact.warehouse_graph.nodes.Length));
            Assert.That(layout.Edges, Has.Count.GreaterThan(0));
            Assert.That(layout.SkippedEdgeCount, Is.EqualTo(0));
            Assert.That(layout.Nodes.Any(node => node.Category == WarehouseLayoutNodeCategory.Dock), Is.True);
            Assert.That(layout.Nodes.Any(node => node.Category == WarehouseLayoutNodeCategory.Aisle), Is.True);
            Assert.That(layout.Nodes.Any(node => node.Category == WarehouseLayoutNodeCategory.Generic), Is.True);

            AssertMovementArtifact(movementArtifact);

            var actorTimeline = ActorTimelineBuilder.FromArtifact(movementArtifact);
            Assert.That(actorTimeline.IsEmpty, Is.False);
            Assert.That(actorTimeline.ActorIds, Is.Not.Empty);

            var actorId = actorTimeline.ActorIds.OrderBy(id => id, StringComparer.Ordinal).First();
            Assert.That(actorTimeline.TryGetTimeline(actorId, out var timeline), Is.True);
            Assert.That(timeline.Segments, Is.Not.Empty);
            Assert.That(timeline.Provenance, Does.Contain("deterministic modeled movement"));
            Assert.That(timeline.Provenance, Does.Contain("not calibrated real trajectory"));

            var firstSegment = timeline.Segments[0];
            var pose = ActorAnimationSampler.Sample(actorTimeline, actorId, firstSegment.StartMs);
            Assert.That(pose.IsAvailable, Is.True);
            Assert.That(pose.EvidenceKind, Is.EqualTo("route_segment"));
            Assert.That(pose.EvidenceId, Is.EqualTo(firstSegment.SegmentId));
            Assert.That(pose.Provenance, Does.Contain("deterministic modeled movement"));

            AssertComparisonArtifact(comparisonArtifact);

            var showcase = AbShowcasePresenter.FromComparisonArtifact(
                comparisonArtifact,
                RealComparisonSourceLabel);
            Assert.That(showcase.IsAvailable, Is.True);
            Assert.That(showcase.Baseline.DisplayLabel, Is.EqualTo("Baseline"));
            Assert.That(showcase.Candidate.DisplayLabel, Is.EqualTo("Optimized"));
            Assert.That(showcase.SourceLabel, Is.EqualTo(RealComparisonSourceLabel));
            Assert.That(showcase.SourceLabel, Does.Not.Contain("Mock"));
            Assert.That(showcase.EvidenceLabel, Does.Contain("Demo only"));
            Assert.That(showcase.EvidenceLabel, Does.Contain("not a real optimization result"));
            Assert.That(showcase.KpiRows, Is.Not.Empty);
            Assert.That(showcase.KpiRows.Any(row => row.MetricName == "order_cycle_p50_ms"), Is.True);
            Assert.That(showcase.KpiRows.All(row => row.SourceLabel == RealComparisonSourceLabel), Is.True);
        }

        [Test]
        public void Final_integration_smoke_fallbacks_are_safe_and_honest()
        {
            var missingRun = TryLoadRunArtifact(RepoPath("datasets/medium-warehouse/artifacts/missing-run.json"));
            var emptyLayout = WarehouseLayoutRenderModelBuilder.Build(missingRun.Artifact);

            Assert.That(missingRun.IsAvailable, Is.False);
            Assert.That(missingRun.Reason, Does.Contain("not found"));
            Assert.That(emptyLayout.IsEmpty, Is.True);

            var runWithoutGraph = new RunArtifactDto { warehouse_graph = null };
            Assert.That(WarehouseLayoutRenderModelBuilder.Build(runWithoutGraph).IsEmpty, Is.True);

            var missingMovement = MovementArtifactLoader.LoadOptionalFromFile(
                RepoPath("datasets/medium-warehouse/artifacts/missing-movement.json"));
            var missingTimeline = ActorTimelineBuilder.FromLoadResult(missingMovement);
            Assert.That(missingMovement.IsAvailable, Is.False);
            Assert.That(missingTimeline.IsEmpty, Is.True);
            Assert.That(ActorAnimationSampler.Sample(missingTimeline, "forklift-1", 0).IsAvailable, Is.False);

            var emptyRouteTimeline = ActorTimelineBuilder.FromArtifact(new MovementArtifactDto
            {
                actors = Array.Empty<MovementActorDto>(),
                movement_events = Array.Empty<MovementEventDto>(),
                route_segments = Array.Empty<MovementRouteSegmentDto>()
            });
            Assert.That(emptyRouteTimeline.IsEmpty, Is.True);

            var missingComparison = AbShowcasePresenter.FromFile(
                RepoPath("datasets/medium-warehouse/optimized/artifacts/missing-comparison.json"));
            Assert.That(missingComparison.IsAvailable, Is.False);
            Assert.That(missingComparison.KpiRows, Is.Empty);
            Assert.That(missingComparison.EvidenceLabel, Does.Contain("Demo only"));
            Assert.That(missingComparison.EvidenceLabel, Does.Contain("not a real optimization result"));

            var emptyComparison = AbShowcasePresenter.FromComparisonArtifact(
                new ComparisonArtifactDto
                {
                    kpi_deltas = null,
                    improvement_pct = null
                },
                RealComparisonSourceLabel);
            Assert.That(emptyComparison.IsAvailable, Is.True);
            Assert.That(emptyComparison.KpiRows, Is.Empty);
            Assert.That(emptyComparison.SourceLabel, Is.EqualTo(RealComparisonSourceLabel));
        }

        [Test]
        public void Final_integration_smoke_keeps_demo_evidence_labels_explicit()
        {
            var movementArtifact = MovementArtifactLoader.LoadFromFile(RepoPath(BaselineMovementArtifact));
            var comparisonArtifact = ComparisonArtifactLoader.LoadFromFile(RepoPath(ComparisonArtifact));

            Assert.That(movementArtifact.provenance.deterministic_generation_policy,
                Does.Contain("deterministic modeled movement"));
            Assert.That(movementArtifact.provenance.deterministic_generation_policy,
                Does.Contain("not calibrated real trajectory"));

            var comparisonJson = File.ReadAllText(RepoPath(ComparisonArtifact));
            Assert.That(comparisonJson, Does.Contain("deterministic demo heuristic"));
            Assert.That(comparisonJson, Does.Contain("not globally optimal"));
            Assert.That(comparisonJson, Does.Contain("not calibrated WMS or sensor-ground-truth measurements"));

            var showcase = AbShowcasePresenter.FromComparisonArtifact(
                comparisonArtifact,
                RealComparisonSourceLabel);
            Assert.That(showcase.SourceLabel, Does.Contain("A5b deterministic comparison artifact"));
            Assert.That(showcase.EvidenceLabel, Does.Contain("Demo only"));
            Assert.That(showcase.EvidenceLabel, Does.Not.Contain("sensor"));
            Assert.That(showcase.EvidenceLabel, Does.Not.Contain("WMS"));
        }

        private static void AssertRunArtifact(RunArtifactDto artifact)
        {
            Assert.That(artifact, Is.Not.Null);
            Assert.That(artifact.warehouse_graph.nodes, Is.Not.Empty);
            Assert.That(artifact.warehouse_graph.edges, Is.Not.Empty);
            Assert.That(artifact.kpi_summary.order_cycle_p50_ms, Is.Not.Null);
            Assert.That(artifact.kpi_summary.order_cycle_p90_ms, Is.Not.Null);
            Assert.That(artifact.kpi_summary.order_cycle_p95_ms, Is.Not.Null);
            Assert.That(artifact.kpi_summary.avg_wait_ms, Is.Not.Null);
            Assert.That(artifact.kpi_summary.resource_utilization, Is.Not.Empty);
            Assert.That(artifact.kpi_summary.bottlenecks, Is.Not.Empty);
        }

        private static void AssertMovementArtifact(MovementArtifactDto artifact)
        {
            Assert.That(artifact, Is.Not.Null);
            Assert.That(artifact.warehouse_graph.nodes, Is.Not.Empty);
            Assert.That(artifact.warehouse_graph.edges, Is.Not.Empty);
            Assert.That(artifact.actors, Is.Not.Empty);
            Assert.That(artifact.route_segments, Is.Not.Empty);
            Assert.That(artifact.provenance.movement_enabled, Is.True);
            Assert.That(artifact.provenance.deterministic_generation_policy,
                Does.Contain("deterministic modeled movement"));
        }

        private static void AssertComparisonArtifact(ComparisonArtifactDto artifact)
        {
            Assert.That(artifact, Is.Not.Null);
            Assert.That(artifact.schema_version, Is.EqualTo("comparison_artifact.v1"));
            Assert.That(artifact.kpi_deltas, Is.Not.Empty);
            Assert.That(artifact.improvement_pct, Is.Not.Empty);
            Assert.That(artifact.baseline.scenario_id, Is.EqualTo("medium-warehouse"));
            Assert.That(artifact.candidate.scenario_id, Is.EqualTo("medium-warehouse-optimized-abc-slotting"));
            Assert.That(artifact.kpi_deltas.ContainsKey("order_cycle_p50_ms"), Is.True);
            Assert.That(artifact.improvement_pct.ContainsKey("order_cycle_p50_ms"), Is.True);
        }

        private static RunArtifactLoadResult TryLoadRunArtifact(string path)
        {
            try
            {
                return RunArtifactLoadResult.Available(RunArtifactLoader.LoadFromFile(path));
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is FileNotFoundException ||
                ex is InvalidOperationException)
            {
                return RunArtifactLoadResult.Unavailable($"RunArtifact unavailable: {ex.Message}");
            }
        }

        private static string RepoPath(string relativePath)
        {
            var root = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "..",
                ".."));
            return Path.Combine(root, relativePath);
        }

        private sealed class RunArtifactLoadResult
        {
            private RunArtifactLoadResult(bool isAvailable, string reason, RunArtifactDto artifact)
            {
                IsAvailable = isAvailable;
                Reason = reason ?? string.Empty;
                Artifact = artifact;
            }

            public bool IsAvailable { get; }
            public string Reason { get; }
            public RunArtifactDto Artifact { get; }

            public static RunArtifactLoadResult Available(RunArtifactDto artifact) =>
                new RunArtifactLoadResult(true, string.Empty, artifact);

            public static RunArtifactLoadResult Unavailable(string reason) =>
                new RunArtifactLoadResult(false, reason, null);
        }
    }
}
