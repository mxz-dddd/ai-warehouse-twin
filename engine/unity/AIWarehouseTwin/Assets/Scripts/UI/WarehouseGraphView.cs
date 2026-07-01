using System.Collections.Generic;
using AIWarehouseTwin.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    // Draws a static warehouse path network: edges as line segments, nodes as
    // dots. This is a frozen layout floor plan (a baseline handoff of where the
    // path graph sits), NOT simulated movement — no actor animates along it.
    public sealed class WarehouseGraphView : VisualElement
    {
        private const float NodeRadius = 7f;
        private const float EdgeWidth = 2f;
        private const float Padding = 28f;

        private static readonly Color BackgroundColor = new Color(0.12f, 0.14f, 0.16f);
        private static readonly Color EdgeColor = new Color(0.35f, 0.45f, 0.55f);
        private static readonly Color NodeColor = new Color(0.4f, 0.8f, 1.0f);

        private IReadOnlyList<GraphNodeLayout> _nodes = new List<GraphNodeLayout>();
        private IReadOnlyList<GraphEdgeLayout> _edges = new List<GraphEdgeLayout>();

        public WarehouseGraphView()
        {
            generateVisualContent += OnGenerateVisualContent;
            style.flexGrow = 1;
        }

        public void SetGraph(WarehouseGraph graph)
        {
            _nodes = WarehouseGraphRenderer.BuildNodeLayout(graph);
            _edges = WarehouseGraphRenderer.BuildEdgeLayout(graph);
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;

            // Background — Painter2D has no Rect(); use a closed MoveTo/LineTo path.
            painter.fillColor = BackgroundColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(w, 0));
            painter.LineTo(new Vector2(w, h));
            painter.LineTo(new Vector2(0, h));
            painter.ClosePath();
            painter.Fill();

            float drawW = w - Padding * 2;
            float drawH = h - Padding * 2;

            // Edges first so node dots draw on top of the connecting lines.
            painter.strokeColor = EdgeColor;
            painter.lineWidth = EdgeWidth;
            foreach (var e in _edges)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(Padding + e.FromNx * drawW, Padding + e.FromNy * drawH));
                painter.LineTo(new Vector2(Padding + e.ToNx * drawW, Padding + e.ToNy * drawH));
                painter.Stroke();
            }

            painter.fillColor = NodeColor;
            foreach (var n in _nodes)
            {
                float sx = Padding + n.Nx * drawW;
                float sy = Padding + n.Ny * drawH;
                painter.BeginPath();
                painter.Arc(new Vector2(sx, sy), NodeRadius, 0f, 360f);
                painter.Fill();
            }
        }
    }
}
