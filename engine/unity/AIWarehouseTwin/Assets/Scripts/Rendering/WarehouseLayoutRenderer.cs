using System.Collections.Generic;
using AIWarehouseTwin.Artifact;

namespace AIWarehouseTwin.Rendering
{
    public static class WarehouseLayoutRenderer
    {
        public static IReadOnlyList<LayoutResourcePoint> BuildPoints(RunArtifactDto artifact)
        {
            var points = new List<LayoutResourcePoint>();
            if (artifact?.layout?.resources == null) return points;
            foreach (var r in artifact.layout.resources)
                points.Add(new LayoutResourcePoint(r.resource_id, r.node_id, (float)r.x, (float)r.y));
            return points;
        }

        // Maps raw coordinate space to [0,1] relative to bounding box.
        // Returns same point if all coords are identical.
        public static (float nx, float ny) Normalize(
            float x, float y, float minX, float minY, float rangeX, float rangeY)
        {
            float nx = rangeX > 0 ? (x - minX) / rangeX : 0.5f;
            float ny = rangeY > 0 ? (y - minY) / rangeY : 0.5f;
            return (nx, ny);
        }
    }
}
