using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Rendering.Layout;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class WarehousePaletteTests
    {
        [Test]
        public void New_palette_uses_runtime_default_layout_colors()
        {
            var palette = ScriptableObject.CreateInstance<WarehousePalette>();
            try
            {
                AssertColor(palette.NodeColorFor(WarehouseLayoutNodeCategory.Dock), WarehousePalette.DefaultDockNodeColor);
                AssertColor(palette.NodeColorFor(WarehouseLayoutNodeCategory.Aisle), WarehousePalette.DefaultAisleNodeColor);
                AssertColor(palette.NodeColorFor(WarehouseLayoutNodeCategory.Shelf), WarehousePalette.DefaultShelfNodeColor);
                AssertColor(palette.NodeColorFor(WarehouseLayoutNodeCategory.Zone), WarehousePalette.DefaultZoneNodeColor);
                AssertColor(palette.EdgeColor, WarehousePalette.DefaultEdgeColor);
                AssertColor(palette.ZoneFillColor, WarehousePalette.DefaultZoneFillColor);
                AssertColor(palette.ZoneBorderColor, WarehousePalette.DefaultZoneBorderColor);
            }
            finally
            {
                Object.DestroyImmediate(palette);
            }
        }

        [Test]
        public void Builder_uses_supplied_palette_without_changing_graph_shape()
        {
            var palette = ScriptableObject.CreateInstance<WarehousePalette>();
            try
            {
                SetColor(palette, "_dockNodeColor", Color.magenta);
                SetColor(palette, "_edgeColor", Color.cyan);
                SetColor(palette, "_zoneFillColor", new Color(0.1f, 0.2f, 0.3f, 0.4f));

                var graph = new WarehouseGraphDto
                {
                    nodes = new[]
                    {
                        new WarehouseGraphNodeDto { node_id = "dock", node_type = "dock", x = 0, y = 0 },
                        new WarehouseGraphNodeDto { node_id = "zone", node_type = "cold-zone", x = 1, y = 1 }
                    },
                    edges = new[]
                    {
                        new WarehouseGraphEdgeDto
                        {
                            edge_id = "edge",
                            from_node_id = "dock",
                            to_node_id = "zone",
                            distance_m = 1,
                            travel_time_ms = 1,
                            bidirectional = true
                        }
                    }
                };

                var model = WarehouseLayoutRenderModelBuilder.Build(graph, palette: palette);

                Assert.That(model.Nodes, Has.Count.EqualTo(2));
                Assert.That(model.Edges, Has.Count.EqualTo(1));
                Assert.That(model.Zones, Has.Count.EqualTo(1));
                AssertColor(FindNode(model, "dock").Color, Color.magenta);
                AssertColor(model.Edges[0].Color, Color.cyan);
                AssertColor(model.Zones[0].FillColor, new Color(0.1f, 0.2f, 0.3f, 0.4f));
            }
            finally
            {
                Object.DestroyImmediate(palette);
            }
        }

        [Test]
        public void Default_palette_asset_exists_with_runtime_defaults()
        {
            var palette = AssetDatabase.LoadAssetAtPath<WarehousePalette>("Assets/UI/DefaultPalette.asset");

            Assert.That(palette, Is.Not.Null);
            AssertColor(palette.NodeColorFor(WarehouseLayoutNodeCategory.Dock), WarehousePalette.DefaultDockNodeColor);
            AssertColor(palette.EdgeColor, WarehousePalette.DefaultEdgeColor);
            AssertColor(palette.ToastInfoBackgroundColor, WarehousePalette.DefaultToastInfoBackgroundColor);
            AssertColor(palette.ToastTextColor, WarehousePalette.DefaultToastTextColor);
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

        private static void SetColor(Object target, string propertyName, Color color)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
