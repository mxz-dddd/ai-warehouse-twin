using AIWarehouseTwin.World;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class GridFloorRendererTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CreateFloor_generates_floor_mesh_with_requested_size()
        {
            root = new GameObject("GridFloorRendererTests");

            var floor = GridFloorRenderer.CreateFloor(
                root.transform,
                "Floor",
                20f,
                40f,
                5f,
                Color.gray,
                Color.white);

            var mesh = floor.GetComponent<MeshFilter>().sharedMesh;

            Assert.That(floor.name, Is.EqualTo("Floor"));
            Assert.That(mesh.bounds.size.x, Is.EqualTo(20f).Within(0.001f));
            Assert.That(mesh.bounds.size.z, Is.EqualTo(40f).Within(0.001f));
            Assert.That(floor.GetComponent<MeshRenderer>().receiveShadows, Is.False);
        }

        [Test]
        public void CreateFloor_generates_grid_lines_from_cell_size()
        {
            root = new GameObject("GridFloorRendererTests");

            var floor = GridFloorRenderer.CreateFloor(
                root.transform,
                "Floor",
                20f,
                10f,
                5f,
                Color.gray,
                Color.white);

            var grid = floor.transform.Find("GridLines");
            var mesh = grid.GetComponent<MeshFilter>().sharedMesh;

            Assert.That(grid, Is.Not.Null);
            Assert.That(mesh.vertexCount, Is.EqualTo(16));
        }
    }
}
