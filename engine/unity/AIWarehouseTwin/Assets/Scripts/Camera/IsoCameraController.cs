using UnityEngine;

namespace AIWarehouseTwin.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class IsoCameraController : MonoBehaviour
    {
        [SerializeField]
        private float orthoSize = 12f;

        [SerializeField]
        private float panSpeed = 0.5f;

        [SerializeField]
        private float zoomSpeed = 2f;

        [SerializeField]
        private float minOrthoSize = 4f;

        [SerializeField]
        private float maxOrthoSize = 30f;

        private UnityEngine.Camera cam;

        public float OrthoSize
        {
            get => orthoSize;
            set => orthoSize = Mathf.Clamp(value, minOrthoSize, maxOrthoSize);
        }

        public float PanSpeed
        {
            get => panSpeed;
            set => panSpeed = value;
        }

        public float ZoomSpeed
        {
            get => zoomSpeed;
            set => zoomSpeed = value;
        }

        public float MinOrthoSize
        {
            get => minOrthoSize;
            set
            {
                minOrthoSize = Mathf.Max(0.01f, value);
                maxOrthoSize = Mathf.Max(maxOrthoSize, minOrthoSize);
                OrthoSize = orthoSize;
            }
        }

        public float MaxOrthoSize
        {
            get => maxOrthoSize;
            set
            {
                maxOrthoSize = Mathf.Max(minOrthoSize, value);
                OrthoSize = orthoSize;
            }
        }

        private void Awake()
        {
            ApplyDefaults();
        }

        private void Update()
        {
            if (Input.GetMouseButton(2))
            {
                ApplyPan(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
            }

            var scrollDelta = Input.mouseScrollDelta.y;
            if (!Mathf.Approximately(scrollDelta, 0f))
            {
                ApplyZoom(scrollDelta);
            }
        }

        public void ApplyDefaults()
        {
            cam = GetComponent<UnityEngine.Camera>();
            OrthoSize = orthoSize;

            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            transform.rotation = Quaternion.Euler(35f, 45f, 0f);
        }

        public void ApplyPan(Vector2 delta)
        {
            var offset = (-transform.right * delta.x) + (-transform.up * delta.y);
            transform.position += offset * panSpeed;
        }

        public void ApplyZoom(float scrollDelta)
        {
            cam = cam != null ? cam : GetComponent<UnityEngine.Camera>();
            OrthoSize = orthoSize - (scrollDelta * zoomSpeed);
            cam.orthographicSize = orthoSize;
        }
    }
}
