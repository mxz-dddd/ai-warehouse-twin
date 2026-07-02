using System.Collections;
using System.Reflection;
using UnityEngine;

namespace AIWarehouseTwin.UI
{
    public class ToastNotification : MonoBehaviour
    {
        public enum ToastType
        {
            Info,
            Warning,
            Error
        }

        [SerializeField]
        private Component label;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private float visibleSeconds = 2.5f;

        [SerializeField]
        private float fadeSeconds = 0.5f;

        private Coroutine fadeCoroutine;
        private string currentMessage = string.Empty;
        private ToastType currentType = ToastType.Info;
        private Color currentColor = Color.white;
        private float currentAlpha;
        private static ToastNotification instance;

        public static ToastNotification Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<ToastNotification>();
                }

                return instance;
            }

            private set => instance = value;
        }

        public string CurrentMessage => currentMessage;

        public ToastType CurrentType => currentType;

        public Color CurrentColor => currentColor;

        public float CurrentAlpha => canvasGroup != null ? canvasGroup.alpha : currentAlpha;

        public static void Show(string msg, ToastType type = ToastType.Info)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.ShowInstance(msg, type);
        }

        private void Awake()
        {
            Instance = this;
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Bind(Component labelComponent, CanvasGroup group)
        {
            label = labelComponent;
            canvasGroup = group;
            currentAlpha = canvasGroup != null ? canvasGroup.alpha : currentAlpha;
        }

        public void ShowInstance(string msg, ToastType type = ToastType.Info)
        {
            currentMessage = msg ?? string.Empty;
            currentType = type;
            currentColor = ColorFor(type);
            currentAlpha = 1f;

            SetLabelText(currentMessage);
            SetLabelColor(currentColor);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            if (isActiveAndEnabled)
            {
                fadeCoroutine = StartCoroutine(FadeOutAfterDelay());
            }
        }

        public static Color ColorFor(ToastType type)
        {
            switch (type)
            {
                case ToastType.Warning:
                    return Color.yellow;
                case ToastType.Error:
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        private IEnumerator FadeOutAfterDelay()
        {
            if (visibleSeconds > 0f)
            {
                yield return new WaitForSeconds(visibleSeconds);
            }

            if (fadeSeconds <= 0f)
            {
                SetAlpha(0f);
                fadeCoroutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / fadeSeconds)));
                yield return null;
            }

            SetAlpha(0f);
            fadeCoroutine = null;
        }

        private void SetAlpha(float alpha)
        {
            currentAlpha = alpha;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }

        private void SetLabelText(string text)
        {
            SetLabelProperty("text", text);
        }

        private void SetLabelColor(Color color)
        {
            SetLabelProperty("color", color);
        }

        private void SetLabelProperty(string propertyName, object value)
        {
            if (label == null)
            {
                return;
            }

            var property = label.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            property.SetValue(label, value, null);
        }
    }
}
