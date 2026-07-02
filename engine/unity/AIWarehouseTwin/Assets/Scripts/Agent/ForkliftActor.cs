using AIWarehouseTwin.World;
using UnityEngine;

namespace AIWarehouseTwin.Agent
{
    public sealed class ForkliftActor : ActorController
    {
        private const float EmptyForkHeight = 0.1f;
        private const float RaisedForkHeight = 1.2f;
        private static readonly Color DefaultForkliftColor = new Color(0.96f, 0.52f, 0.12f, 1f);

        [SerializeField]
        private WarehousePalette palette;

        private Transform visualRoot;
        private GameObject chassis;
        private GameObject mast;
        private GameObject leftFork;
        private GameObject rightFork;

        public float ForkHeight { get; private set; } = EmptyForkHeight;

        public GameObject Chassis => chassis;

        public GameObject Mast => mast;

        public GameObject LeftFork => leftFork;

        public GameObject RightFork => rightFork;

        public void SetPalette(WarehousePalette nextPalette)
        {
            palette = nextPalette;
            ApplyColor();
        }

        public void EnsureVisuals()
        {
            if (visualRoot == null)
            {
                var root = new GameObject("ForkliftVisuals");
                root.transform.SetParent(transform, false);
                visualRoot = root.transform;
            }

            if (chassis == null)
            {
                chassis = CreatePart("ForkliftChassis", new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 0.6f, 0.8f));
            }

            if (mast == null)
            {
                mast = CreatePart("ForkliftMast", new Vector3(0f, 0.95f, 0.45f), new Vector3(0.16f, 1.4f, 0.12f));
            }

            if (leftFork == null)
            {
                leftFork = CreatePart("ForkliftLeftFork", Vector3.zero, new Vector3(0.12f, 0.08f, 0.9f));
            }

            if (rightFork == null)
            {
                rightFork = CreatePart("ForkliftRightFork", Vector3.zero, new Vector3(0.12f, 0.08f, 0.9f));
            }

            SetForkHeight(ForkHeight);
            ApplyColor();
        }

        protected override void OnStateChanged(ActorState next)
        {
            EnsureVisuals();
            SetForkHeight(next == ActorState.Carrying ? RaisedForkHeight : EmptyForkHeight);
        }

        private void Awake()
        {
            EnsureVisuals();
        }

        private GameObject CreatePart(string partName, Vector3 localPosition, Vector3 localScale)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(visualRoot, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            return part;
        }

        private void SetForkHeight(float height)
        {
            ForkHeight = height;
            SetForkPosition(leftFork, -0.28f, height);
            SetForkPosition(rightFork, 0.28f, height);
        }

        private static void SetForkPosition(GameObject fork, float x, float height)
        {
            if (fork != null)
            {
                fork.transform.localPosition = new Vector3(x, height, 0.8f);
            }
        }

        private void ApplyColor()
        {
            var color = palette != null ? palette.forklift : DefaultForkliftColor;
            ApplyColor(chassis, color);
            ApplyColor(mast, color);
            ApplyColor(leftFork, color);
            ApplyColor(rightFork, color);
        }

        private static void ApplyColor(GameObject target, Color color)
        {
            if (target == null)
            {
                return;
            }

            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = color;
            }
        }
    }
}
