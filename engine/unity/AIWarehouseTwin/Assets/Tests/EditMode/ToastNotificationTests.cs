using AIWarehouseTwin.UI;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class ToastNotificationTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Show_sets_message_text()
        {
            var toast = CreateToast(out var label, out _);

            ToastNotification.Show("Saved");

            Assert.That(toast.CurrentMessage, Is.EqualTo("Saved"));
            Assert.That(label.text, Is.EqualTo("Saved"));
        }

        [TestCase(ToastNotification.ToastType.Info, 1f, 1f, 1f)]
        [TestCase(ToastNotification.ToastType.Warning, 1f, 0.9215686f, 0.01568628f)]
        [TestCase(ToastNotification.ToastType.Error, 1f, 0f, 0f)]
        public void Show_sets_color_for_type(
            ToastNotification.ToastType type,
            float r,
            float g,
            float b)
        {
            var toast = CreateToast(out var label, out _);

            ToastNotification.Show("Message", type);

            Assert.That(toast.CurrentType, Is.EqualTo(type));
            AssertColor(toast.CurrentColor, new Color(r, g, b, 1f));
            AssertColor(label.color, new Color(r, g, b, 1f));
        }

        [Test]
        public void Show_sets_canvas_alpha_to_visible()
        {
            var toast = CreateToast(out _, out var canvasGroup);
            canvasGroup.alpha = 0f;

            ToastNotification.Show("Visible");

            Assert.That(canvasGroup.alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(toast.CurrentAlpha, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Show_without_label_does_not_throw()
        {
            var toast = CreateToastWithoutBindings();
            toast.Bind(null, root.AddComponent<CanvasGroup>());

            Assert.DoesNotThrow(() => ToastNotification.Show("No label"));
            Assert.That(toast.CurrentMessage, Is.EqualTo("No label"));
        }

        [Test]
        public void Show_without_canvas_group_does_not_throw()
        {
            var toast = CreateToastWithoutBindings();
            var label = root.AddComponent<TestToastLabel>();
            toast.Bind(label, null);

            Assert.DoesNotThrow(() => ToastNotification.Show("No group"));
            Assert.That(label.text, Is.EqualTo("No group"));
            Assert.That(toast.CurrentAlpha, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Show_without_instance_does_not_throw()
        {
            Assert.DoesNotThrow(() => ToastNotification.Show("No instance"));
        }

        [Test]
        public void Multiple_show_calls_update_message()
        {
            CreateToast(out var label, out _);

            ToastNotification.Show("First");
            ToastNotification.Show("Second", ToastNotification.ToastType.Warning);

            Assert.That(label.text, Is.EqualTo("Second"));
            AssertColor(label.color, Color.yellow);
        }

        private ToastNotification CreateToast(out TestToastLabel label, out CanvasGroup canvasGroup)
        {
            var toast = CreateToastWithoutBindings();
            label = root.AddComponent<TestToastLabel>();
            canvasGroup = root.AddComponent<CanvasGroup>();
            toast.Bind(label, canvasGroup);
            return toast;
        }

        private ToastNotification CreateToastWithoutBindings()
        {
            root = new GameObject("ToastNotificationTests");
            return root.AddComponent<ToastNotification>();
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }

    }

    public sealed class TestToastLabel : MonoBehaviour
    {
        public string text { get; set; } = string.Empty;

        public Color color { get; set; } = Color.clear;
    }
}
