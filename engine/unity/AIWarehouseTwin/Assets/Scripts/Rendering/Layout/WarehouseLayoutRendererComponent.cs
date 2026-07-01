using UnityEngine;

namespace AIWarehouseTwin.Rendering.Layout
{
    public sealed class WarehouseLayoutRendererComponent : MonoBehaviour
    {
        [SerializeField] private float lineWidth = 0.015f;

        public void Render(WarehouseLayoutRenderModel model)
        {
            Clear();
            if (model == null || model.IsEmpty)
            {
                return;
            }

            foreach (var zone in model.Zones)
            {
                CreateZone(zone);
            }

            foreach (var edge in model.Edges)
            {
                CreateEdge(edge);
            }

            foreach (var node in model.Nodes)
            {
                CreateNode(node);
            }
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void CreateNode(WarehouseLayoutNodeRenderModel node)
        {
            var child = GameObject.CreatePrimitive(PrimitiveType.Quad);
            child.name = $"layout-node-{node.NodeId}";
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(node.LocalPosition.x, node.LocalPosition.y, 0f);
            child.transform.localScale = new Vector3(node.Size, node.Size, 1f);

            var renderer = child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(node.Color);

            var collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
        }

        private void CreateEdge(WarehouseLayoutEdgeRenderModel edge)
        {
            var child = new GameObject($"layout-edge-{edge.EdgeId}");
            child.transform.SetParent(transform, false);

            var line = child.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.sharedMaterial = CreateMaterial(edge.Color);
            line.SetPosition(0, new Vector3(edge.FromLocalPosition.x, edge.FromLocalPosition.y, 0.05f));
            line.SetPosition(1, new Vector3(edge.ToLocalPosition.x, edge.ToLocalPosition.y, 0.05f));
        }

        private void CreateZone(WarehouseLayoutZoneRenderModel zone)
        {
            var child = GameObject.CreatePrimitive(PrimitiveType.Quad);
            child.name = $"layout-zone-{zone.ZoneId}";
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(zone.Center.x, zone.Center.y, 0.1f);
            child.transform.localScale = new Vector3(zone.Size.x, zone.Size.y, 1f);

            var renderer = child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(zone.FillColor);

            var collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader);
            material.color = color;
            return material;
        }
    }
}
