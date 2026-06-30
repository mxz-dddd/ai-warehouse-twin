using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Rendering;
using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class WarehouseLayoutRendererTests
    {
        [Test]
        public void BuildPoints_returns_one_point_per_resource()
        {
            var artifact = LoadGolden();
            var points = WarehouseLayoutRenderer.BuildPoints(artifact);
            Assert.That(points.Count, Is.EqualTo(artifact.layout.resources.Length));
        }

        [Test]
        public void BuildPoints_preserves_resource_ids()
        {
            var points = WarehouseLayoutRenderer.BuildPoints(LoadGolden());
            var ids = new System.Collections.Generic.List<string>();
            foreach (var p in points) ids.Add(p.ResourceId);
            Assert.That(ids, Does.Contain("dock-1"));
            Assert.That(ids, Does.Contain("station-1"));
        }

        [Test]
        public void BuildPoints_returns_empty_for_null_artifact()
        {
            var points = WarehouseLayoutRenderer.BuildPoints(null);
            Assert.That(points.Count, Is.EqualTo(0));
        }

        [Test]
        public void Normalize_maps_min_to_zero_max_to_one()
        {
            var (nx, ny) = WarehouseLayoutRenderer.Normalize(0f, 0f, 0f, 0f, 10f, 10f);
            Assert.That(nx, Is.EqualTo(0f).Within(0.001f));
            Assert.That(ny, Is.EqualTo(0f).Within(0.001f));
            (nx, ny) = WarehouseLayoutRenderer.Normalize(10f, 10f, 0f, 0f, 10f, 10f);
            Assert.That(nx, Is.EqualTo(1f).Within(0.001f));
            Assert.That(ny, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Normalize_returns_half_when_range_is_zero()
        {
            var (nx, ny) = WarehouseLayoutRenderer.Normalize(5f, 5f, 5f, 5f, 0f, 0f);
            Assert.That(nx, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(ny, Is.EqualTo(0.5f).Within(0.001f));
        }

        private static RunArtifactDto LoadGolden() =>
            RunArtifactLoader.LoadFromJson(
                File.ReadAllText(Path.Combine(
                    Application.dataPath, "StreamingAssets", "run-artifact.v1.json")));
    }
}
