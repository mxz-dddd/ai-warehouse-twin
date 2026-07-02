using UnityEngine;
using UnityEngine.Rendering;

namespace AIWarehouseTwin.World
{
    public sealed class WarehouseStructureBuilder : MonoBehaviour
    {
        [SerializeField]
        private Vector3 shelfSize = new Vector3(0.8f, 0.6f, 1.8f);

        [SerializeField]
        private float gridCellSizeM = 2f;

        public Vector3 ShelfSize
        {
            get => shelfSize;
            set => shelfSize = value;
        }

        public float GridCellSizeM
        {
            get => gridCellSizeM;
            set => gridCellSizeM = Mathf.Max(0.01f, value);
        }

        public GameObject Build(
            Transform parent,
            WarehouseStructureLayout layout,
            WarehousePalette palette)
        {
            layout ??= LayoutAutoGenerator.Generate(new AIWarehouseTwin.Artifact.WarehouseGraphDto(), null);
            palette ??= ScriptableObject.CreateInstance<WarehousePalette>();
            palette.ApplyDefaultColors();

            var root = new GameObject("WarehouseRoot");
            root.transform.SetParent(parent, false);

            GridFloorRenderer.CreateFloor(
                root.transform,
                "Floor",
                layout.WidthM,
                layout.LengthM,
                gridCellSizeM,
                palette.floor,
                palette.highlight);

            var zonesRoot = CreateGroup(root.transform, "Zones");
            var shelvesRoot = CreateGroup(root.transform, "Shelves");
            var docksRoot = CreateGroup(root.transform, "Docks");

            foreach (var element in layout.Elements)
            {
                switch (element.Kind)
                {
                    case WarehouseStructureElementKind.Zone:
                        CreateBox(
                            zonesRoot.transform,
                            element.Id,
                            element.Center,
                            element.Size,
                            ZoneColor(element.Role, palette));
                        break;
                    case WarehouseStructureElementKind.Shelf:
                        CreateBox(
                            shelvesRoot.transform,
                            element.Id,
                            new Vector3(element.Center.x, shelfSize.y * 0.5f, element.Center.z),
                            shelfSize,
                            palette.shelf);
                        break;
                    case WarehouseStructureElementKind.Dock:
                        CreateBox(
                            docksRoot.transform,
                            element.Id,
                            element.Center,
                            element.Size,
                            DockColor(element.Role, palette));
                        break;
                }
            }

            return root;
        }

        public static GameObject CreateGroup(Transform parent, string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group;
        }

        public static GameObject CreateBox(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 size,
            Color color)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = center;
            box.transform.localScale = size;

            var collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = box.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GridFloorRenderer.CreateMaterial(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return box;
        }

        private static Color ZoneColor(string role, WarehousePalette palette)
        {
            switch (role)
            {
                case "receiving":
                    return palette.zoneReceive;
                case "storage":
                    return palette.zoneStorage;
                case "shipping":
                    return palette.zoneShip;
                default:
                    return palette.highlight;
            }
        }

        private static Color DockColor(string role, WarehousePalette palette)
        {
            return role == "shipping" ? palette.btnSecondary : palette.btnPrimary;
        }
    }
}
