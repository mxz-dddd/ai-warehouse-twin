using System.IO;
using AIWarehouseTwin.Artifact;
using NUnit.Framework;
using UnityEngine;
using System;

namespace AIWarehouseTwin.Tests
{
    public sealed class RunArtifactLoaderTests
    {
        [Test]
        public void LoadFromJson_reads_run_artifact_v1_golden_fields()
        {
            var artifact = RunArtifactLoader.LoadFromJson(LoadGoldenJson());

            Assert.That(artifact.schema_version, Is.EqualTo("run-artifact.v1"));
            Assert.That(artifact.artifact_kind, Is.EqualTo("warehouse-simulation-run"));
            Assert.That(artifact.scenario_id, Is.EqualTo("sample-small-warehouse"));
            Assert.That(artifact.seed, Is.EqualTo(20240627));
            Assert.That(artifact.started_at_ms, Is.EqualTo(10));
            Assert.That(artifact.finished_at_ms, Is.EqualTo(220));
            Assert.That(artifact.final_world_time_ms, Is.EqualTo(220));
            Assert.That(artifact.kpi_summary.total_duration_ms, Is.EqualTo(210));
            Assert.That(artifact.kpi_summary.total_completed_work_items, Is.EqualTo(3));
            Assert.That(artifact.kpi_summary.event_log_line_count, Is.EqualTo(10));
            Assert.That(artifact.layout.resources, Has.Length.EqualTo(4));
            Assert.That(artifact.position_timeline, Has.Length.EqualTo(12));
            Assert.That(artifact.event_log, Has.Length.EqualTo(10));
        }

        [Test]
        public void LoadFromJson_rejects_unknown_schema_version()
        {
            var json = LoadGoldenJson().Replace("run-artifact.v1", "run-artifact.v2");

            Assert.Throws<System.InvalidOperationException>(() => RunArtifactLoader.LoadFromJson(json));
        }

        [Test]
        public void LoadFromJson_reads_contract_r3_warehouse_graph()
        {
            var artifact = RunArtifactLoader.LoadFromJson(LoadContractR3RunJson());

            Assert.That(artifact.schema_version, Is.EqualTo("run-artifact.v1.r3"));
            Assert.That(artifact.warehouse_graph.nodes, Has.Length.EqualTo(4));
            Assert.That(artifact.warehouse_graph.edges, Has.Length.EqualTo(3));

            var node = artifact.warehouse_graph.nodes[0];
            Assert.That(node.node_id, Is.EqualTo("node-dock-in"));
            Assert.That(node.node_type, Is.EqualTo("dock"));
            Assert.That(node.x, Is.EqualTo(0));
            Assert.That(node.y, Is.EqualTo(0));

            var edge = artifact.warehouse_graph.edges[0];
            Assert.That(edge.edge_id, Is.EqualTo("edge-dock-aisle-west"));
            Assert.That(edge.from_node_id, Is.EqualTo("node-dock-in"));
            Assert.That(edge.to_node_id, Is.EqualTo("node-aisle-west"));
            Assert.That(edge.distance_m, Is.EqualTo(2.5d).Within(0.001d));
            Assert.That(edge.travel_time_ms, Is.EqualTo(1200));
            Assert.That(edge.bidirectional, Is.True);
        }

        [Test]
        public void LoadFromJson_reads_contract_r3_kpi_summary_extended_fields()
        {
            var kpi = RunArtifactLoader.LoadFromJson(LoadContractR3RunJson()).kpi_summary;

            Assert.That(kpi.order_cycle_p50_ms, Is.EqualTo(95000d));
            Assert.That(kpi.order_cycle_p90_ms, Is.EqualTo(142000d));
            Assert.That(kpi.order_cycle_p95_ms, Is.EqualTo(158000d));
            Assert.That(kpi.avg_wait_ms, Is.EqualTo(18500d));

            Assert.That(kpi.resource_utilization.TryGetValue("forklift-1", out var forkliftUtilization), Is.True);
            Assert.That(forkliftUtilization, Is.EqualTo(67.5d).Within(0.001d));
            Assert.That(kpi.bottlenecks, Has.Length.EqualTo(2));
            Assert.That(kpi.bottlenecks[0].resource_id, Is.EqualTo("forklift-1"));
            Assert.That(kpi.bottlenecks[0].avg_wait_ms, Is.EqualTo(22000d));
            Assert.That(kpi.travel_distance_m_by_actor_type.TryGetValue("worker", out var workerDistance), Is.True);
            Assert.That(workerDistance, Is.EqualTo(36.25d).Within(0.001d));
        }

        [Test]
        public void MovementArtifactLoader_reads_contract_r3_path_and_route_shape()
        {
            var artifact = MovementArtifactLoader.LoadFromJson(LoadContractR3MovementJson());

            Assert.That(artifact.schema_version, Is.EqualTo("movement-artifact.v1"));
            Assert.That(artifact.artifact_kind, Is.EqualTo("warehouse-movement"));
            Assert.That(artifact.source_run_artifact, Is.EqualTo("contract_r3_run_fixture.json"));
            Assert.That(artifact.warehouse_graph.nodes, Has.Length.EqualTo(3));
            Assert.That(artifact.warehouse_graph.edges, Has.Length.EqualTo(2));
            Assert.That(artifact.actors, Has.Length.EqualTo(1));
            Assert.That(artifact.actors[0].actor_id, Is.EqualTo("forklift-1"));
            Assert.That(artifact.movement_events, Has.Length.EqualTo(4));
            Assert.That(artifact.movement_events[0].event_id, Is.EqualTo("evt-r3-forklift-start-001"));
            Assert.That(artifact.route_segments, Has.Length.EqualTo(2));
            Assert.That(artifact.route_segments[0].path_node_ids, Is.EqualTo(new[]
            {
                "node-dock-in",
                "node-aisle-west"
            }));
            Assert.That(artifact.route_segments[0].edge_ids, Is.EqualTo(new[]
            {
                "edge-dock-aisle-west"
            }));
            Assert.That(artifact.route_segments[0].travel_time_ms, Is.EqualTo(1200));
            Assert.That(artifact.provenance.movement_generator_version, Is.EqualTo("contract-r3-fixture"));
            Assert.That(artifact.provenance.graph_source, Does.Contain("deterministic modeled fixture"));
            Assert.That(artifact.provenance.movement_enabled, Is.True);
            Assert.That(artifact.provenance.deterministic_generation_policy, Does.Contain("fixture_only"));
        }

        [Test]
        public void ComparisonArtifactLoader_reads_contract_r3_kpi_deltas_and_improvement_pct()
        {
            var artifact = ComparisonArtifactLoader.LoadFromJson(LoadContractR3ComparisonJson());

            Assert.That(artifact.schema_version, Is.EqualTo("comparison_artifact.v1.r3"));
            Assert.That(artifact.kpi_deltas.TryGetValue("order_cycle_p50_ms", out var orderCycleDelta), Is.True);
            Assert.That(orderCycleDelta.baseline_value, Is.EqualTo(120000d));
            Assert.That(orderCycleDelta.candidate_value, Is.EqualTo(90000d));
            Assert.That(orderCycleDelta.delta, Is.EqualTo(-30000d));
            Assert.That(orderCycleDelta.lower_is_better, Is.True);
            Assert.That(artifact.improvement_pct.TryGetValue("order_cycle_p50_ms", out var improvement), Is.True);
            Assert.That(improvement, Is.EqualTo(25d));
        }

        [Test]
        public void LoadFromJson_normalizes_missing_run_warehouse_graph_to_empty_graph()
        {
            var artifact = RunArtifactLoader.LoadFromJson(MinimalRunArtifactJson());

            Assert.That(artifact.warehouse_graph, Is.Not.Null);
            Assert.That(artifact.warehouse_graph.nodes, Is.Empty);
            Assert.That(artifact.warehouse_graph.edges, Is.Empty);
            Assert.That(artifact.layout.resources, Is.Empty);
        }

        [Test]
        public void MovementArtifactLoader_optional_file_missing_returns_unavailable_result()
        {
            var missingPath = Path.Combine(Path.GetTempPath(), $"awt-missing-movement-{Guid.NewGuid():N}.json");

            var result = MovementArtifactLoader.LoadOptionalFromFile(missingPath);

            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.UnavailableReason, Does.Contain("not found"));
            Assert.That(result.Artifact, Is.Not.Null);
            Assert.That(result.Artifact.route_segments, Is.Empty);
            Assert.That(result.Artifact.movement_events, Is.Empty);
        }

        [Test]
        public void MovementArtifactLoader_normalizes_missing_route_segments_to_empty_array()
        {
            var artifact = MovementArtifactLoader.LoadFromJson(
                @"{
  ""schema_version"": ""movement-artifact.v1"",
  ""artifact_kind"": ""warehouse-movement"",
  ""scenario_id"": ""fallback-movement"",
  ""run_id"": ""run-1"",
  ""seed"": 7,
  ""source_run_artifact"": ""run.json"",
  ""warehouse_graph"": { ""nodes"": [], ""edges"": [] },
  ""actors"": [],
  ""movement_events"": [],
  ""provenance"": {
    ""movement_generator_version"": ""test"",
    ""graph_source"": ""fixture"",
    ""movement_enabled"": true,
    ""deterministic_generation_policy"": ""test-only""
  }
}");

            Assert.That(artifact.route_segments, Is.Empty);
            Assert.That(artifact.movement_events, Is.Empty);
        }

        [Test]
        public void ComparisonArtifactLoader_normalizes_missing_optional_maps_to_empty_collections()
        {
            var artifact = ComparisonArtifactLoader.LoadFromJson(MinimalComparisonArtifactJson());

            Assert.That(artifact.kpi_deltas, Is.Empty);
            Assert.That(artifact.improvement_pct, Is.Empty);
        }

        private static string LoadGoldenJson()
        {
            return File.ReadAllText(Path.Combine(
                Application.dataPath,
                "StreamingAssets",
                "run-artifact.v1.json"));
        }

        private static string LoadContractR3RunJson() =>
            File.ReadAllText(ContractFixturePath("contract_r3_run_fixture.json"));

        private static string LoadContractR3MovementJson() =>
            File.ReadAllText(ContractFixturePath("contract_r3_movement_fixture.json"));

        private static string LoadContractR3ComparisonJson() =>
            File.ReadAllText(ContractFixturePath("contract_r3_comparison_fixture.json"));

        private static string ContractFixturePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "..",
                "..",
                "packages",
                "contracts",
                "fixtures",
                "contract-r3",
                fileName));
        }

        private static string MinimalRunArtifactJson()
        {
            return @"{
  ""schema_version"": ""run-artifact.v1.r3"",
  ""artifact_kind"": ""warehouse-simulation-run"",
  ""scenario_id"": ""fallback-run"",
  ""seed"": 1,
  ""started_at_ms"": 0,
  ""finished_at_ms"": 10,
  ""final_world_time_ms"": 10,
  ""kpi_summary"": {
    ""total_duration_ms"": 10,
    ""total_completed_work_items"": 0,
    ""event_log_line_count"": 0,
    ""receipt_throughput_per_hour"": 0,
    ""outbound_order_throughput_per_hour"": 0,
    ""each_pick_order_throughput_per_hour"": 0,
    ""total_work_item_throughput_per_hour"": 0
  },
  ""layout"": null,
  ""warehouse_graph"": null,
  ""position_timeline"": [],
  ""event_log"": []
}";
        }

        private static string MinimalComparisonArtifactJson()
        {
            return @"{
  ""schema_version"": ""comparison_artifact.v1.r3"",
  ""baseline"": {
    ""scenario_id"": ""baseline"",
    ""metrics"": {
      ""finished_at_ms"": 10,
      ""completed_receipts"": 0,
      ""completed_outbound_orders"": 0,
      ""completed_each_pick_orders"": 0,
      ""total_quantity_received"": 0,
      ""total_quantity_shipped"": 0,
      ""total_quantity_picked"": 0,
      ""inbound_receipt_throughput_per_hour"": 0,
      ""outbound_order_throughput_per_hour"": 0,
      ""each_pick_order_throughput_per_hour"": 0,
      ""total_work_item_throughput_per_hour"": 0
    }
  },
  ""candidate"": {
    ""scenario_id"": ""candidate"",
    ""metrics"": {
      ""finished_at_ms"": 10,
      ""completed_receipts"": 0,
      ""completed_outbound_orders"": 0,
      ""completed_each_pick_orders"": 0,
      ""total_quantity_received"": 0,
      ""total_quantity_shipped"": 0,
      ""total_quantity_picked"": 0,
      ""inbound_receipt_throughput_per_hour"": 0,
      ""outbound_order_throughput_per_hour"": 0,
      ""each_pick_order_throughput_per_hour"": 0,
      ""total_work_item_throughput_per_hour"": 0
    }
  },
  ""deltas"": []
}";
        }
    }
}
