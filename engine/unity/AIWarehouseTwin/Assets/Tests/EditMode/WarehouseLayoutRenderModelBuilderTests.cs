using System.IO;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Rendering.Layout;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class WarehouseLayoutRenderModelBuilderTests
    {
        [Test]
        public void RunArtifactLoader_parses_fixture_warehouse_graph_for_layout_renderer()
        {
            var artifact = RunArtifactLoader.LoadFromJson(RunArtifactWithWarehouseGraphJson());

            Assert.That(artifact.warehouse_graph, Is.Not.Null);
            Assert.That(artifact.warehouse_graph.nodes, Has.Length.EqualTo(4));
            Assert.That(artifact.warehouse_graph.edges, Has.Length.EqualTo(3));
            Assert.That(artifact.warehouse_graph.nodes[0].x, Is.EqualTo(-5d));
            Assert.That(artifact.warehouse_graph.nodes[0].y, Is.EqualTo(10d));
        }

        [Test]
        public void Build_uses_node_x_y_with_stable_scale_and_offset_mapping()
        {
            var artifact = RunArtifactLoader.LoadFromJson(RunArtifactWithWarehouseGraphJson());
            var mapper = new WarehouseLayoutCoordinateMapper(new Vector2(10f, 20f), new Vector2(1f, -2f));

            var first = WarehouseLayoutRenderModelBuilder.Build(artifact, mapper);
            var second = WarehouseLayoutRenderModelBuilder.Build(artifact, mapper);

            Assert.That(first.Nodes.Count, Is.EqualTo(4));
            Assert.That(first.Nodes[0].NodeId, Is.EqualTo("node-aisle-west"));
            Assert.That(first.Nodes[0].LocalPosition, Is.EqualTo(second.Nodes[0].LocalPosition));

            var dock = FindNode(first, "node-dock-in");
            Assert.That(dock.X, Is.EqualTo(-5f).Within(0.001f));
            Assert.That(dock.Y, Is.EqualTo(10f).Within(0.001f));
            Assert.That(dock.LocalPosition.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(dock.LocalPosition.y, Is.EqualTo(-2f).Within(0.001f));

            var shelf = FindNode(first, "node-shelf-a");
            Assert.That(shelf.LocalPosition.x, Is.EqualTo(11f).Within(0.001f));
            Assert.That(shelf.LocalPosition.y, Is.EqualTo(18f).Within(0.001f));
        }

        [Test]
        public void Build_centers_single_node_on_zero_range_axes()
        {
            var graph = new WarehouseGraphDto
            {
                nodes = new[]
                {
                    new WarehouseGraphNodeDto
                    {
                        node_id = "single",
                        node_type = "dock",
                        x = 42,
                        y = -7
                    }
                }
            };

            var model = WarehouseLayoutRenderModelBuilder.Build(
                graph,
                new WarehouseLayoutCoordinateMapper(new Vector2(8f, 4f), new Vector2(-1f, 2f)));

            Assert.That(model.Nodes, Has.Count.EqualTo(1));
            Assert.That(model.Nodes[0].LocalPosition.x, Is.EqualTo(3f).Within(0.001f));
            Assert.That(model.Nodes[0].LocalPosition.y, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void Build_preserves_edge_shape_and_skips_missing_endpoints()
        {
            var model = WarehouseLayoutRenderModelBuilder.Build(
                RunArtifactLoader.LoadFromJson(RunArtifactWithWarehouseGraphJson()));

            Assert.That(model.Edges, Has.Count.EqualTo(2));
            Assert.That(model.SkippedEdgeCount, Is.EqualTo(1));

            var edge = model.Edges[0];
            Assert.That(edge.EdgeId, Is.EqualTo("edge-aisle-shelf"));
            Assert.That(edge.FromNodeId, Is.EqualTo("node-aisle-west"));
            Assert.That(edge.ToNodeId, Is.EqualTo("node-shelf-a"));
            Assert.That(edge.DistanceM, Is.EqualTo(3.75d).Within(0.001d));
            Assert.That(edge.TravelTimeMs, Is.EqualTo(1700));
            Assert.That(edge.Bidirectional, Is.False);
        }

        [Test]
        public void Build_returns_empty_model_when_warehouse_graph_is_missing_without_using_position_timeline()
        {
            var artifact = new RunArtifactDto
            {
                warehouse_graph = null,
                position_timeline = new[]
                {
                    new RunArtifactPositionTimelineEntryDto
                    {
                        node_id = "position-only-node",
                        x = 100,
                        y = 200,
                        event_type = "start"
                    }
                }
            };

            var model = WarehouseLayoutRenderModelBuilder.Build(artifact);

            Assert.That(model.IsEmpty, Is.True);
            Assert.That(model.Nodes, Is.Empty);
            Assert.That(model.Edges, Is.Empty);
        }

        [Test]
        public void Build_allows_node_only_model_when_edges_are_empty()
        {
            var graph = new WarehouseGraphDto
            {
                nodes = new[]
                {
                    new WarehouseGraphNodeDto { node_id = "dock", node_type = "dock", x = 0, y = 0 }
                },
                edges = System.Array.Empty<WarehouseGraphEdgeDto>()
            };

            var model = WarehouseLayoutRenderModelBuilder.Build(graph);

            Assert.That(model.Nodes, Has.Count.EqualTo(1));
            Assert.That(model.Edges, Is.Empty);
            Assert.That(model.SkippedEdgeCount, Is.EqualTo(0));
        }

        [Test]
        public void Build_classifies_dock_aisle_shelf_and_zone_nodes_with_distinct_grayscale_styles()
        {
            var model = WarehouseLayoutRenderModelBuilder.Build(
                RunArtifactLoader.LoadFromJson(RunArtifactWithWarehouseGraphJson()));

            Assert.That(FindNode(model, "node-dock-in").Category, Is.EqualTo(WarehouseLayoutNodeCategory.Dock));
            Assert.That(FindNode(model, "node-aisle-west").Category, Is.EqualTo(WarehouseLayoutNodeCategory.Aisle));
            Assert.That(FindNode(model, "node-shelf-a").Category, Is.EqualTo(WarehouseLayoutNodeCategory.Shelf));
            Assert.That(FindNode(model, "zone-cold").Category, Is.EqualTo(WarehouseLayoutNodeCategory.Zone));
            Assert.That(model.Zones, Has.Count.EqualTo(1));
            Assert.That(FindNode(model, "node-dock-in").Color, Is.Not.EqualTo(FindNode(model, "node-shelf-a").Color));
        }

        [Test]
        public void Renderer_accepts_empty_and_populated_models_without_reading_files()
        {
            var root = new GameObject("layout-renderer-test");
            try
            {
                var renderer = root.AddComponent<WarehouseLayoutRendererComponent>();

                renderer.Render(WarehouseLayoutRenderModel.Empty);
                Assert.That(root.transform.childCount, Is.EqualTo(0));

                renderer.Render(WarehouseLayoutRenderModelBuilder.Build(
                    RunArtifactLoader.LoadFromJson(RunArtifactWithWarehouseGraphJson())));
                Assert.That(root.transform.childCount, Is.EqualTo(7));

                renderer.Clear();
                Assert.That(root.transform.childCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void B1_renderer_does_not_reference_forbidden_runtime_sources_or_json_parser()
        {
            var root = Path.Combine(Application.dataPath, "Scripts", "Rendering", "Layout");
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                Assert.That(source, Does.Not.Contain("Sim." + "Core"), file);
                Assert.That(source, Does.Not.Contain("Path" + "Graph"), file);
                Assert.That(source, Does.Not.Contain("Layout" + "GraphSource"), file);
                Assert.That(source, Does.Not.Contain("JsonUtility." + "FromJson"), file);
                Assert.That(source, Does.Not.Contain("position" + "_timeline"), file);
                Assert.That(source, Does.Not.Contain("distance" + "_mm"), file);
                Assert.That(source, Does.Not.Contain("x" + "_mm"), file);
                Assert.That(source, Does.Not.Contain("y" + "_mm"), file);
            }
        }

        private static WarehouseLayoutNodeRenderModel FindNode(
            WarehouseLayoutRenderModel model,
            string nodeId)
        {
            foreach (var node in model.Nodes)
            {
                if (node.NodeId == nodeId)
                {
                    return node;
                }
            }

            Assert.Fail($"Node '{nodeId}' was not found.");
            return default;
        }

        private static string RunArtifactWithWarehouseGraphJson() =>
            @"{
  ""schema_version"": ""run-artifact.v1.r3"",
  ""artifact_kind"": ""warehouse-simulation-run"",
  ""scenario_id"": ""b1-layout-renderer-fixture"",
  ""seed"": 42,
  ""started_at_ms"": 0,
  ""finished_at_ms"": 10,
  ""final_world_time_ms"": 10,
  ""kpi_summary"": {
    ""total_duration_ms"": 10,
    ""total_completed_work_items"": 0,
    ""event_log_line_count"": 0
  },
  ""layout"": { ""resources"": [] },
  ""warehouse_graph"": {
    ""nodes"": [
      { ""node_id"": ""node-dock-in"", ""node_type"": ""dock"", ""x"": -5, ""y"": 10 },
      { ""node_id"": ""node-aisle-west"", ""node_type"": ""aisle"", ""x"": 0, ""y"": 20 },
      { ""node_id"": ""node-shelf-a"", ""node_type"": ""shelf"", ""x"": 15, ""y"": 40 },
      { ""node_id"": ""zone-cold"", ""node_type"": ""cold-zone"", ""x"": 5, ""y"": 30 }
    ],
    ""edges"": [
      {
        ""edge_id"": ""edge-dock-aisle"",
        ""from_node_id"": ""node-dock-in"",
        ""to_node_id"": ""node-aisle-west"",
        ""distance_m"": 2.5,
        ""travel_time_ms"": 1200,
        ""bidirectional"": true
      },
      {
        ""edge_id"": ""edge-aisle-shelf"",
        ""from_node_id"": ""node-aisle-west"",
        ""to_node_id"": ""node-shelf-a"",
        ""distance_m"": 3.75,
        ""travel_time_ms"": 1700,
        ""bidirectional"": false
      },
      {
        ""edge_id"": ""edge-dangling"",
        ""from_node_id"": ""node-shelf-a"",
        ""to_node_id"": ""ghost-node"",
        ""distance_m"": 9.0,
        ""travel_time_ms"": 9000,
        ""bidirectional"": true
      }
    ]
  },
  ""position_timeline"": [],
  ""event_log"": []
}";
    }
}
