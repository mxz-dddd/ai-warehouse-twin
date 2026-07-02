using UnityEngine;
using UnityEngine.Rendering;

namespace AIWarehouseTwin.World
{
    public sealed class GridFloorRenderer : MonoBehaviour
    {
        [SerializeField]
        private float widthM = 20f;

        [SerializeField]
        private float lengthM = 40f;

        [SerializeField]
        private float gridCellSizeM = 2f;

        public GameObject Render(Transform parent, Color floorColor, Color gridColor)
        {
            return CreateFloor(parent, "Floor", widthM, lengthM, gridCellSizeM, floorColor, gridColor);
        }

        public static GameObject CreateFloor(
            Transform parent,
            string name,
            float widthM,
            float lengthM,
            float gridCellSizeM,
            Color floorColor,
            Color gridColor)
        {
            var floor = new GameObject(string.IsNullOrWhiteSpace(name) ? "Floor" : name);
            floor.transform.SetParent(parent, false);

            var meshFilter = floor.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateFloorMesh(widthM, lengthM);

            var renderer = floor.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(floorColor);
            DisableShadows(renderer);

            var grid = new GameObject("GridLines");
            grid.transform.SetParent(floor.transform, false);
            var gridFilter = grid.AddComponent<MeshFilter>();
            gridFilter.sharedMesh = CreateGridMesh(widthM, lengthM, gridCellSizeM);
            var gridRenderer = grid.AddComponent<MeshRenderer>();
            gridRenderer.sharedMaterial = CreateMaterial(gridColor);
            DisableShadows(gridRenderer);

            return floor;
        }

        public static Mesh CreateFloorMesh(float widthM, float lengthM)
        {
            var halfWidth = Mathf.Max(0.01f, widthM) * 0.5f;
            var halfLength = Mathf.Max(0.01f, lengthM) * 0.5f;
            var mesh = new Mesh { name = "WarehouseFloorMesh" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -halfLength),
                new Vector3(halfWidth, 0f, -halfLength),
                new Vector3(-halfWidth, 0f, halfLength),
                new Vector3(halfWidth, 0f, halfLength),
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateGridMesh(float widthM, float lengthM, float gridCellSizeM)
        {
            var width = Mathf.Max(0.01f, widthM);
            var length = Mathf.Max(0.01f, lengthM);
            var cell = Mathf.Max(0.01f, gridCellSizeM);
            var halfWidth = width * 0.5f;
            var halfLength = length * 0.5f;
            var xLines = Mathf.FloorToInt(width / cell) + 1;
            var zLines = Mathf.FloorToInt(length / cell) + 1;
            var vertices = new Vector3[(xLines + zLines) * 2];
            var index = 0;

            for (var i = 0; i < xLines; i++)
            {
                var x = -halfWidth + (i * cell);
                if (i == xLines - 1)
                {
                    x = halfWidth;
                }

                vertices[index++] = new Vector3(x, 0.01f, -halfLength);
                vertices[index++] = new Vector3(x, 0.01f, halfLength);
            }

            for (var i = 0; i < zLines; i++)
            {
                var z = -halfLength + (i * cell);
                if (i == zLines - 1)
                {
                    z = halfLength;
                }

                vertices[index++] = new Vector3(-halfWidth, 0.01f, z);
                vertices[index++] = new Vector3(halfWidth, 0.01f, z);
            }

            var mesh = new Mesh { name = "WarehouseGridMesh" };
            mesh.vertices = vertices;
            var indices = new int[vertices.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Material CreateMaterial(Color color)
        {
            var shader =
                Shader.Find("Sprites/Default") ??
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Standard");
            var material = new Material(shader)
            {
                color = color
            };
            return material;
        }

        private static void DisableShadows(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
