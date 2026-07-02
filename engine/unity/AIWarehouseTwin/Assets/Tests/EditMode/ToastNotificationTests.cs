using AIWarehouseTwin.Rendering.Layout;
using AIWarehouseTwin.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.Tests
{
    public sealed class ToastNotificationTests
    {
        [Test]
        public void Bind_creates_hidden_toast_element()
        {
            var root = new VisualElement();
            var host = new GameObject("toast-test");
            try
            {
                var toast = host.AddComponent<ToastNotification>();

                toast.Bind(root);

                var element = root.Q<VisualElement>(ToastNotification.ElementName);
                Assert.That(element, Is.Not.Null);
                Assert.That(element.style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(element.Q<Label>(ToastNotification.MessageLabelName), Is.Not.Null);
                Assert.That(toast.IsVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Show_sets_message_level_and_palette_style()
        {
            var root = new VisualElement();
            var host = new GameObject("toast-test");
            try
            {
                var toast = host.AddComponent<ToastNotification>();
                toast.Bind(root);

                toast.Show("Saved scenario", ToastNotificationLevel.Success, 0f);

                var element = root.Q<VisualElement>(ToastNotification.ElementName);
                var label = element.Q<Label>(ToastNotification.MessageLabelName);
                Assert.That(toast.IsVisible, Is.True);
                Assert.That(toast.Level, Is.EqualTo(ToastNotificationLevel.Success));
                Assert.That(toast.Message, Is.EqualTo("Saved scenario"));
                Assert.That(label.text, Is.EqualTo("Saved scenario"));
                Assert.That(element.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                AssertColor(element.style.backgroundColor.value, WarehousePalette.DefaultToastSuccessBackgroundColor);
                AssertColor(label.style.color.value, WarehousePalette.DefaultToastTextColor);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Hide_keeps_element_but_marks_toast_invisible()
        {
            var root = new VisualElement();
            var host = new GameObject("toast-test");
            try
            {
                var toast = host.AddComponent<ToastNotification>();
                toast.Bind(root);
                toast.Show("Warning", ToastNotificationLevel.Warning, 0f);

                toast.Hide();

                Assert.That(root.Q<VisualElement>(ToastNotification.ElementName), Is.Not.Null);
                Assert.That(toast.IsVisible, Is.False);
                Assert.That(root.Q<VisualElement>(ToastNotification.ElementName).style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Toast_prefab_exists_and_references_default_palette()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ToastNotification.prefab");
            var palette = AssetDatabase.LoadAssetAtPath<WarehousePalette>("Assets/UI/DefaultPalette.asset");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(palette, Is.Not.Null);

            var toast = prefab.GetComponent<ToastNotification>();
            Assert.That(toast, Is.Not.Null);

            var serialized = new SerializedObject(toast);
            Assert.That(serialized.FindProperty("_palette").objectReferenceValue, Is.SameAs(palette));
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
