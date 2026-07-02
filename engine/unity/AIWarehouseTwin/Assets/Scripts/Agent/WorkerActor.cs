using System;
using AIWarehouseTwin.World;
using AIWarehouseTwin.VFX;
using UnityEngine;

namespace AIWarehouseTwin.Agent
{
    public sealed class WorkerActor : ActorController
    {
        private static readonly Color DefaultWorkerColor = new Color(0.29f, 0.56f, 0.85f, 1f);

        [SerializeField]
        private WarehousePalette palette;

        [SerializeField]
        private float bobAmplitude = 0.08f;

        [SerializeField]
        private float bobFrequency = 8f;

        [SerializeField]
        private PickCompleteVFX pickCompleteVfxPrefab;

        private Transform visualRoot;
        private GameObject body;
        private GameObject head;
        private ActorState state = ActorState.Idle;

        public Func<Vector3, PickCompleteVFX> PickVfxFactory { get; set; }

        public float BobOffset { get; private set; }

        public GameObject Body => body;

        public GameObject Head => head;

        public void SetPickCompleteVfxPrefab(PickCompleteVFX prefab)
        {
            pickCompleteVfxPrefab = prefab;
        }

        public void SetPalette(WarehousePalette nextPalette)
        {
            palette = nextPalette;
            ApplyColor();
        }

        public void EnsureVisuals()
        {
            if (visualRoot == null)
            {
                var root = new GameObject("WorkerVisuals");
                root.transform.SetParent(transform, false);
                visualRoot = root.transform;
            }

            if (body == null)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "WorkerBody";
                body.transform.SetParent(visualRoot, false);
                body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                body.transform.localScale = new Vector3(0.35f, 0.75f, 0.35f);
            }

            if (head == null)
            {
                head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "WorkerHead";
                head.transform.SetParent(visualRoot, false);
                head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                head.transform.localScale = Vector3.one * 0.35f;
            }

            ApplyColor();
        }

        protected override void OnStateChanged(ActorState next)
        {
            state = next;
            if (state == ActorState.Picking)
            {
                TriggerPickCompleteVfx();
            }

            if (state != ActorState.Moving)
            {
                SetBobOffset(0f);
            }
        }

        protected override void OnTicked(float simTime)
        {
            if (state == ActorState.Moving)
            {
                SetBobOffset(Mathf.Sin(simTime * bobFrequency) * bobAmplitude);
            }
        }

        private void Awake()
        {
            EnsureVisuals();
        }

        private void SetBobOffset(float offset)
        {
            BobOffset = offset;
            if (visualRoot != null)
            {
                visualRoot.localPosition = new Vector3(0f, BobOffset, 0f);
            }
        }

        private void TriggerPickCompleteVfx()
        {
            var position = transform.position;
            var effect = PickVfxFactory?.Invoke(position);
            if (effect == null && pickCompleteVfxPrefab != null)
            {
                effect = Instantiate(pickCompleteVfxPrefab, position, Quaternion.identity);
            }

            if (effect != null)
            {
                effect.PlayAt(position);
            }
        }

        private void ApplyColor()
        {
            var color = palette != null ? palette.worker : DefaultWorkerColor;
            ApplyColor(body, color);
            ApplyColor(head, color);
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
