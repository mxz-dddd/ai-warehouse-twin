using System;
using AIWarehouseTwin.Rendering.Layout;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public enum ToastNotificationLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    public sealed class ToastNotification : MonoBehaviour
    {
        public const string ElementName = "toast-notification";
        public const string MessageLabelName = "toast-notification-message";

        [SerializeField] private UIDocument _document;
        [SerializeField] private WarehousePalette _palette;
        [SerializeField] private float _defaultDurationSeconds = 4f;

        private VisualElement _toastElement;
        private Label _messageLabel;
        private float _hideAt = -1f;

        public bool IsVisible { get; private set; }
        public ToastNotificationLevel Level { get; private set; }
        public string Message => _messageLabel?.text ?? string.Empty;

        private void Awake()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            if (_document?.rootVisualElement != null)
            {
                Bind(_document.rootVisualElement);
            }
        }

        private void Update()
        {
            if (IsVisible && _hideAt >= 0f && Time.unscaledTime >= _hideAt)
            {
                Hide();
            }
        }

        public void Bind(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _toastElement = root.Q<VisualElement>(ElementName);
            if (_toastElement == null)
            {
                _toastElement = CreateElement();
                root.Add(_toastElement);
            }

            _messageLabel = _toastElement.Q<Label>(MessageLabelName);
            if (_messageLabel == null)
            {
                _messageLabel = CreateMessageLabel();
                _toastElement.Add(_messageLabel);
            }

            Hide();
        }

        public void Show(
            string message,
            ToastNotificationLevel level = ToastNotificationLevel.Info,
            float durationSeconds = -1f)
        {
            EnsureBound();

            Level = level;
            Apply(_toastElement, _messageLabel, message ?? string.Empty, level, _palette);
            _toastElement.style.display = DisplayStyle.Flex;
            IsVisible = true;

            var duration = durationSeconds >= 0f ? durationSeconds : _defaultDurationSeconds;
            _hideAt = duration > 0f ? Time.unscaledTime + duration : -1f;
        }

        public void Hide()
        {
            if (_toastElement != null)
            {
                _toastElement.style.display = DisplayStyle.None;
            }

            IsVisible = false;
            _hideAt = -1f;
        }

        public static VisualElement CreateElement()
        {
            var toast = new VisualElement { name = ElementName, pickingMode = PickingMode.Ignore };
            toast.style.position = Position.Absolute;
            toast.style.top = 16f;
            toast.style.right = 16f;
            toast.style.minWidth = 220f;
            toast.style.maxWidth = 420f;
            toast.style.paddingTop = 10f;
            toast.style.paddingRight = 14f;
            toast.style.paddingBottom = 10f;
            toast.style.paddingLeft = 14f;
            toast.style.borderLeftWidth = 4f;
            toast.style.borderTopWidth = 1f;
            toast.style.borderRightWidth = 1f;
            toast.style.borderBottomWidth = 1f;
            toast.style.display = DisplayStyle.None;
            toast.Add(CreateMessageLabel());
            return toast;
        }

        public static void Apply(
            VisualElement toast,
            Label messageLabel,
            string message,
            ToastNotificationLevel level,
            WarehousePalette palette)
        {
            if (toast == null)
            {
                throw new ArgumentNullException(nameof(toast));
            }

            if (messageLabel == null)
            {
                throw new ArgumentNullException(nameof(messageLabel));
            }

            var background = ToastBackgroundFor(level, palette);
            var text = palette != null
                ? palette.ToastTextColor
                : WarehousePalette.DefaultToastTextColor;

            messageLabel.text = message;
            messageLabel.style.color = text;
            toast.style.backgroundColor = background;
            toast.style.borderLeftColor = background;
            toast.style.borderTopColor = background;
            toast.style.borderRightColor = background;
            toast.style.borderBottomColor = background;
        }

        private void EnsureBound()
        {
            if (_toastElement != null && _messageLabel != null)
            {
                return;
            }

            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            if (_document?.rootVisualElement == null)
            {
                throw new InvalidOperationException("ToastNotification requires Bind(root) or a UIDocument.");
            }

            Bind(_document.rootVisualElement);
        }

        private static Label CreateMessageLabel()
        {
            var label = new Label { name = MessageLabelName };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private static Color ToastBackgroundFor(ToastNotificationLevel level, WarehousePalette palette)
        {
            if (palette == null)
            {
                return level switch
                {
                    ToastNotificationLevel.Success => WarehousePalette.DefaultToastSuccessBackgroundColor,
                    ToastNotificationLevel.Warning => WarehousePalette.DefaultToastWarningBackgroundColor,
                    ToastNotificationLevel.Error => WarehousePalette.DefaultToastErrorBackgroundColor,
                    _ => WarehousePalette.DefaultToastInfoBackgroundColor
                };
            }

            return level switch
            {
                ToastNotificationLevel.Success => palette.ToastSuccessBackgroundColor,
                ToastNotificationLevel.Warning => palette.ToastWarningBackgroundColor,
                ToastNotificationLevel.Error => palette.ToastErrorBackgroundColor,
                _ => palette.ToastInfoBackgroundColor
            };
        }
    }
}
