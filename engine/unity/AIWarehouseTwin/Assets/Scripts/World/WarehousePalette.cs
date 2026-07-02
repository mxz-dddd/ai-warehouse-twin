using UnityEngine;

namespace AIWarehouseTwin.World
{
    [CreateAssetMenu(fileName = "DefaultPalette", menuName = "Warehouse/Palette")]
    public sealed class WarehousePalette : ScriptableObject
    {
        public Color floor;
        public Color zoneReceive;
        public Color zoneStorage;
        public Color zoneShip;
        public Color shelf;
        public Color worker;
        public Color forklift;
        public Color highlight;
        public Color panelBg;
        public Color panelText;
        public Color btnPrimary;
        public Color btnSecondary;

        private void Reset()
        {
            ApplyDefaultColors();
        }

        public void ApplyDefaultColors()
        {
            floor = DefaultColorFromHex("#F5F0E8");
            zoneReceive = DefaultColorFromHex("#7EC8E3");
            zoneStorage = DefaultColorFromHex("#A8D8A8");
            zoneShip = DefaultColorFromHex("#FFD59E");
            shelf = DefaultColorFromHex("#8B7355");
            worker = DefaultColorFromHex("#4A90D9");
            forklift = DefaultColorFromHex("#F5A623");
            highlight = DefaultColorFromHex("#FFE066");
            panelBg = DefaultColorFromHex("#2C3E50", 0.75f);
            panelText = Color.white;
            btnPrimary = DefaultColorFromHex("#27AE60");
            btnSecondary = DefaultColorFromHex("#4A90D9");
        }

        public static Color DefaultColorFromHex(string hex, float alpha = 1f)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out var color))
            {
                return Color.white;
            }

            color.a = alpha;
            return color;
        }
    }
}
