using AIWarehouseTwin.World;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class WarehousePaletteTests
    {
        private WarehousePalette palette;

        [TearDown]
        public void TearDown()
        {
            if (palette != null)
            {
                Object.DestroyImmediate(palette);
            }
        }

        [Test]
        public void ApplyDefaultColors_sets_two_point_isometric_palette()
        {
            palette = ScriptableObject.CreateInstance<WarehousePalette>();

            palette.ApplyDefaultColors();

            AssertColor(palette.floor, "#F5F0E8");
            AssertColor(palette.zoneReceive, "#7EC8E3");
            AssertColor(palette.zoneStorage, "#A8D8A8");
            AssertColor(palette.zoneShip, "#FFD59E");
            AssertColor(palette.shelf, "#8B7355");
            AssertColor(palette.worker, "#4A90D9");
            AssertColor(palette.forklift, "#F5A623");
            AssertColor(palette.highlight, "#FFE066");
            AssertColor(palette.panelBg, "#2C3E50", 0.75f);
            Assert.That(palette.panelText, Is.EqualTo(Color.white));
            AssertColor(palette.btnPrimary, "#27AE60");
            AssertColor(palette.btnSecondary, "#4A90D9");
        }

        [Test]
        public void DefaultColorFromHex_applies_requested_alpha()
        {
            var color = WarehousePalette.DefaultColorFromHex("#2C3E50", 0.75f);

            Assert.That(color.a, Is.EqualTo(0.75f).Within(0.001f));
        }

        private static void AssertColor(Color actual, string expectedHex, float expectedAlpha = 1f)
        {
            ColorUtility.TryParseHtmlString(expectedHex, out var expected);
            expected.a = expectedAlpha;

            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
