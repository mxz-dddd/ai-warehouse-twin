using System.Linq;
using AIWarehouseTwin.World;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class WarehouseStructureBuilderTests
    {
        private GameObject root;
        private WarehousePalette palette;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            if (palette != null)
            {
                Object.DestroyImmediate(palette);
            }
        }

        [Test]
        public void Build_creates_stable_warehouse_hierarchy()
        {
            var builder = CreateBuilder();
            var layout = LayoutAutoGenerator.Generate(new AIWarehouseTwin.Artifact.WarehouseGraphDto(), TestSettings());

            var warehouse = builder.Build(root.transform, layout, palette);

            Assert.That(warehouse.name, Is.EqualTo("WarehouseRoot"));
            Assert.That(warehouse.transform.Find("Floor"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Zones/ReceivingZone"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Zones/StorageZone"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Zones/ShippingZone"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Shelves"), Is.Not.Null);
            Assert.That(warehouse.transform.Find("Docks"), Is.Not.Null);
        }

        [Test]
        public void Build_creates_shelves_and_docks_from_layout()
        {
            var builder = CreateBuilder();
            var layout = LayoutAutoGenerator.Generate(new AIWarehouseTwin.Artifact.WarehouseGraphDto(), TestSettings());

            var warehouse = builder.Build(root.transform, layout, palette);
            var shelves = warehouse.transform.Find("Shelves");
            var docks = warehouse.transform.Find("Docks");

            Assert.That(shelves.Cast<Transform>().Count(), Is.EqualTo(4));
            Assert.That(docks.Cast<Transform>().Count(), Is.EqualTo(2));
            Assert.That(shelves.Find("Shelf_R01_C01"), Is.Not.Null);
            Assert.That(docks.Find("ReceivingDock_01"), Is.Not.Null);
            Assert.That(docks.Find("ShippingDock_01"), Is.Not.Null);
        }

        [Test]
        public void Build_applies_palette_and_default_shelf_dimensions()
        {
            var builder = CreateBuilder();
            var layout = LayoutAutoGenerator.Generate(new AIWarehouseTwin.Artifact.WarehouseGraphDto(), TestSettings());

            var warehouse = builder.Build(root.transform, layout, palette);
            var shelf = warehouse.transform.Find("Shelves/Shelf_R01_C01");
            var receiving = warehouse.transform.Find("Zones/ReceivingZone");

            Assert.That(shelf.localScale.x, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(shelf.localScale.y, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(shelf.localScale.z, Is.EqualTo(1.8f).Within(0.001f));
            AssertColor(
                receiving.GetComponent<MeshRenderer>().sharedMaterial.color,
                palette.zoneReceive);
            AssertColor(
                shelf.GetComponent<MeshRenderer>().sharedMaterial.color,
                palette.shelf);
        }

        private WarehouseStructureBuilder CreateBuilder()
        {
            root = new GameObject("WarehouseStructureBuilderTests");
            palette = ScriptableObject.CreateInstance<WarehousePalette>();
            palette.ApplyDefaultColors();
            return root.AddComponent<WarehouseStructureBuilder>();
        }

        private static LayoutAutoGeneratorSettings TestSettings()
        {
            return new LayoutAutoGeneratorSettings
            {
                widthM = 20f,
                lengthM = 40f,
                zoneDepthM = 5f,
                shelfRows = 2,
                shelfColumns = 2,
                dockPairs = 1
            };
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
