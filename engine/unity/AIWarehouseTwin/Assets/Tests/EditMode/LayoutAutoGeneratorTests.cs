using System.Linq;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.World;
using NUnit.Framework;

namespace AIWarehouseTwin.Tests
{
    public sealed class LayoutAutoGeneratorTests
    {
        [Test]
        public void Generate_is_deterministic_for_nodes_without_3d_coordinates()
        {
            var graph = GraphWithShelfAndDockNodes();
            var settings = TestSettings();

            var first = LayoutAutoGenerator.Generate(graph, settings);
            var second = LayoutAutoGenerator.Generate(graph, settings);

            Assert.That(second.Elements.Count, Is.EqualTo(first.Elements.Count));
            for (var i = 0; i < first.Elements.Count; i++)
            {
                Assert.That(second.Elements[i].Id, Is.EqualTo(first.Elements[i].Id));
                Assert.That(second.Elements[i].Center, Is.EqualTo(first.Elements[i].Center));
                Assert.That(second.Elements[i].Size, Is.EqualTo(first.Elements[i].Size));
            }
        }

        [Test]
        public void Generate_places_receiving_storage_shipping_from_bottom_to_top()
        {
            var layout = LayoutAutoGenerator.Generate(new WarehouseGraphDto(), TestSettings());

            var receiving = layout.Find("ReceivingZone");
            var storage = layout.Find("StorageZone");
            var shipping = layout.Find("ShippingZone");

            Assert.That(receiving.Center.z, Is.LessThan(storage.Center.z));
            Assert.That(storage.Center.z, Is.LessThan(shipping.Center.z));
        }

        [Test]
        public void Generate_arranges_shelves_by_stable_rows_and_columns()
        {
            var layout = LayoutAutoGenerator.Generate(new WarehouseGraphDto(), TestSettings());
            var shelves = layout.Shelves.ToArray();

            Assert.That(shelves, Has.Length.EqualTo(6));
            Assert.That(shelves[0].Id, Is.EqualTo("Shelf_R01_C01"));
            Assert.That(shelves[5].Id, Is.EqualTo("Shelf_R02_C03"));
            Assert.That(shelves[0].Row, Is.EqualTo(1));
            Assert.That(shelves[0].Column, Is.EqualTo(1));
            Assert.That(shelves[0].Center.z, Is.LessThan(shelves[5].Center.z));
        }

        [Test]
        public void Generate_uses_artifact_graph_shelf_count_when_present()
        {
            var graph = GraphWithShelfAndDockNodes();
            var layout = LayoutAutoGenerator.Generate(graph, TestSettings());

            Assert.That(layout.Shelves.Count(), Is.EqualTo(6));
            Assert.That(layout.Docks.Count(), Is.EqualTo(2));
        }

        private static LayoutAutoGeneratorSettings TestSettings()
        {
            return new LayoutAutoGeneratorSettings
            {
                widthM = 24f,
                lengthM = 48f,
                zoneDepthM = 8f,
                shelfRows = 2,
                shelfColumns = 3,
                dockPairs = 1
            };
        }

        private static WarehouseGraphDto GraphWithShelfAndDockNodes()
        {
            return new WarehouseGraphDto
            {
                nodes = new[]
                {
                    new WarehouseGraphNodeDto { node_id = "receiving-dock", node_type = "dock" },
                    new WarehouseGraphNodeDto { node_id = "shipping-dock", node_type = "dock" },
                    new WarehouseGraphNodeDto { node_id = "shelf-a", node_type = "shelf" },
                    new WarehouseGraphNodeDto { node_id = "shelf-b", node_type = "shelf" },
                    new WarehouseGraphNodeDto { node_id = "shelf-c", node_type = "shelf" },
                    new WarehouseGraphNodeDto { node_id = "shelf-d", node_type = "shelf" },
                    new WarehouseGraphNodeDto { node_id = "shelf-e", node_type = "shelf" },
                    new WarehouseGraphNodeDto { node_id = "shelf-f", node_type = "shelf" },
                }
            };
        }
    }
}
