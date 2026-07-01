using UnityEngine;

namespace AIWarehouseTwin.Rendering.Layout
{
    public readonly struct WarehouseLayoutBounds
    {
        public readonly float MinX;
        public readonly float MinY;
        public readonly float RangeX;
        public readonly float RangeY;

        public WarehouseLayoutBounds(float minX, float minY, float rangeX, float rangeY)
        {
            MinX = minX;
            MinY = minY;
            RangeX = rangeX;
            RangeY = rangeY;
        }
    }

    public sealed class WarehouseLayoutCoordinateMapper
    {
        public Vector2 Scale { get; }
        public Vector2 Offset { get; }

        public WarehouseLayoutCoordinateMapper(Vector2 scale, Vector2 offset)
        {
            Scale = scale;
            Offset = offset;
        }

        public Vector2 Map(float x, float y, WarehouseLayoutBounds bounds)
        {
            var nx = bounds.RangeX > 0f ? (x - bounds.MinX) / bounds.RangeX : 0.5f;
            var ny = bounds.RangeY > 0f ? (y - bounds.MinY) / bounds.RangeY : 0.5f;
            return new Vector2(Offset.x + nx * Scale.x, Offset.y + ny * Scale.y);
        }
    }
}
