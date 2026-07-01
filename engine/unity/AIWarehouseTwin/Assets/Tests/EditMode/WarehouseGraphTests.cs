using System.Collections.Generic;
using System.IO;
using AIWarehouseTwin.Graph;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    // Exercises the pure-C# path-graph source adapter and renderer against the
    // sample warehouse layout document shipped in StreamingAssets. These cover
    // the static layout floor plan only; there is no movement to assert.
    public sealed class WarehouseGraphTests
    {
        [Test]
        public void FromDocument_maps_nodes_and_edges()
        {
            var doc = new LayoutDocumentDto
            {
                path_nodes = new[]
                {
                    new PathNodeDto { node_id = "a", node_type = "dock", x = 0, y = 0 },
                    new PathNodeDto { node_id = "b", node_type = "aisle", x = 10, y = 10 },
                },
                path_edges = new[]
                {
                    new PathEdgeDto
                    {
                        edge_id = "e1", from_node_id = "a", to_node_id = "b",
                        distance_mm = 1000, bidirectional = true,
                    },
                },
            };

            var graph = LayoutGraphSource.FromDocument(doc);

            Assert.That(graph.Nodes.Count, Is.EqualTo(2));
            Assert.That(graph.Edges.Count, Is.EqualTo(1));
            Assert.That(graph.Edges[0].FromNodeId, Is.EqualTo("a"));
            Assert.That(graph.Edges[0].Bidirectional, Is.True);
        }

        [Test]
        public void FromDocument_skips_nodes_without_id()
        {
            var doc = new LayoutDocumentDto
            {
                path_nodes = new[]
                {
                    new PathNodeDto { node_id = "a", x = 0, y = 0 },
                    new PathNodeDto { node_id = "", x = 5, y = 5 },
                },
            };

            var graph = LayoutGraphSource.FromDocument(doc);

            Assert.That(graph.Nodes.Count, Is.EqualTo(1));
        }

        [Test]
        public void FromDocument_handles_null_document()
        {
            var graph = LayoutGraphSource.FromDocument(null);

            Assert.That(graph.Nodes.Count, Is.EqualTo(0));
            Assert.That(graph.Edges.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryGetNode_resolves_present_and_absent_ids()
        {
            var graph = LayoutGraphSource.FromDocument(new LayoutDocumentDto
            {
                path_nodes = new[] { new PathNodeDto { node_id = "a", x = 1, y = 2 } },
            });

            Assert.That(graph.TryGetNode("a", out var node), Is.True);
            Assert.That(node.X, Is.EqualTo(1f).Within(0.001f));
            Assert.That(graph.TryGetNode("missing", out _), Is.False);
        }

        [Test]
        public void BuildNodeLayout_produces_one_entry_per_node()
        {
            var graph = LoadSampleGraph();
            var layout = WarehouseGraphRenderer.BuildNodeLayout(graph);
            Assert.That(layout.Count, Is.EqualTo(graph.Nodes.Count));
        }

        [Test]
        public void BuildNodeLayout_normalizes_into_unit_box()
        {
            var layout = WarehouseGraphRenderer.BuildNodeLayout(LoadSampleGraph());
            foreach (var n in layout)
            {
                Assert.That(n.Nx, Is.InRange(0f, 1f));
                Assert.That(n.Ny, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void BuildEdgeLayout_produces_one_segment_per_resolvable_edge()
        {
            var graph = LoadSampleGraph();
            var edges = WarehouseGraphRenderer.BuildEdgeLayout(graph);
            Assert.That(edges.Count, Is.EqualTo(graph.Edges.Count));
        }

        [Test]
        public void BuildEdgeLayout_skips_dangling_edges()
        {
            var doc = new LayoutDocumentDto
            {
                path_nodes = new[] { new PathNodeDto { node_id = "a", x = 0, y = 0 } },
                path_edges = new[]
                {
                    new PathEdgeDto { edge_id = "e", from_node_id = "a", to_node_id = "ghost" },
                },
            };

            var edges = WarehouseGraphRenderer.BuildEdgeLayout(LayoutGraphSource.FromDocument(doc));

            Assert.That(edges.Count, Is.EqualTo(0));
        }

        [Test]
        public void BuildNodeLayout_centers_single_node()
        {
            var graph = LayoutGraphSource.FromDocument(new LayoutDocumentDto
            {
                path_nodes = new[] { new PathNodeDto { node_id = "a", x = 42, y = 42 } },
            });

            var layout = WarehouseGraphRenderer.BuildNodeLayout(graph);

            Assert.That(layout[0].Nx, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(layout[0].Ny, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void SampleGraph_contains_expected_dock_nodes()
        {
            var graph = LoadSampleGraph();
            var ids = new List<string>();
            foreach (var n in graph.Nodes) ids.Add(n.NodeId);

            Assert.That(ids, Does.Contain("node-dock-in"));
            Assert.That(ids, Does.Contain("node-dock-out"));
        }

        private static WarehouseGraph LoadSampleGraph() =>
            LayoutGraphSource.FromJson(
                File.ReadAllText(Path.Combine(
                    Application.dataPath, "StreamingAssets", "layout.json")));
    }
}
