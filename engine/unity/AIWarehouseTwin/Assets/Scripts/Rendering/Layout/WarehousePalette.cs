using UnityEngine;

namespace AIWarehouseTwin.Rendering.Layout
{
    [CreateAssetMenu(menuName = "AI Warehouse Twin/Warehouse Palette", fileName = "WarehousePalette")]
    public sealed class WarehousePalette : ScriptableObject
    {
        [SerializeField] private Color _genericNodeColor = DefaultGenericNodeColor;
        [SerializeField] private Color _dockNodeColor = DefaultDockNodeColor;
        [SerializeField] private Color _aisleNodeColor = DefaultAisleNodeColor;
        [SerializeField] private Color _shelfNodeColor = DefaultShelfNodeColor;
        [SerializeField] private Color _zoneNodeColor = DefaultZoneNodeColor;
        [SerializeField] private Color _edgeColor = DefaultEdgeColor;
        [SerializeField] private Color _zoneFillColor = DefaultZoneFillColor;
        [SerializeField] private Color _zoneBorderColor = DefaultZoneBorderColor;
        [SerializeField] private Color _toastInfoBackgroundColor = DefaultToastInfoBackgroundColor;
        [SerializeField] private Color _toastSuccessBackgroundColor = DefaultToastSuccessBackgroundColor;
        [SerializeField] private Color _toastWarningBackgroundColor = DefaultToastWarningBackgroundColor;
        [SerializeField] private Color _toastErrorBackgroundColor = DefaultToastErrorBackgroundColor;
        [SerializeField] private Color _toastTextColor = DefaultToastTextColor;

        public static Color DefaultGenericNodeColor => new Color(0.3f, 0.3f, 0.3f, 1f);
        public static Color DefaultDockNodeColor => new Color(0.15f, 0.15f, 0.15f, 1f);
        public static Color DefaultAisleNodeColor => new Color(0.45f, 0.45f, 0.45f, 1f);
        public static Color DefaultShelfNodeColor => new Color(0.65f, 0.65f, 0.65f, 1f);
        public static Color DefaultZoneNodeColor => new Color(0.75f, 0.75f, 0.75f, 1f);
        public static Color DefaultEdgeColor => new Color(0.35f, 0.35f, 0.35f, 1f);
        public static Color DefaultZoneFillColor => new Color(0.78f, 0.78f, 0.78f, 0.2f);
        public static Color DefaultZoneBorderColor => new Color(0.55f, 0.55f, 0.55f, 1f);
        public static Color DefaultToastInfoBackgroundColor => new Color(0.12f, 0.22f, 0.34f, 0.96f);
        public static Color DefaultToastSuccessBackgroundColor => new Color(0.10f, 0.30f, 0.20f, 0.96f);
        public static Color DefaultToastWarningBackgroundColor => new Color(0.36f, 0.25f, 0.08f, 0.96f);
        public static Color DefaultToastErrorBackgroundColor => new Color(0.36f, 0.12f, 0.12f, 0.96f);
        public static Color DefaultToastTextColor => new Color(0.94f, 0.96f, 0.98f, 1f);

        public Color EdgeColor => _edgeColor;
        public Color ZoneFillColor => _zoneFillColor;
        public Color ZoneBorderColor => _zoneBorderColor;
        public Color ToastInfoBackgroundColor => _toastInfoBackgroundColor;
        public Color ToastSuccessBackgroundColor => _toastSuccessBackgroundColor;
        public Color ToastWarningBackgroundColor => _toastWarningBackgroundColor;
        public Color ToastErrorBackgroundColor => _toastErrorBackgroundColor;
        public Color ToastTextColor => _toastTextColor;

        public static Color DefaultNodeColorFor(WarehouseLayoutNodeCategory category)
        {
            return category switch
            {
                WarehouseLayoutNodeCategory.Dock => DefaultDockNodeColor,
                WarehouseLayoutNodeCategory.Aisle => DefaultAisleNodeColor,
                WarehouseLayoutNodeCategory.Shelf => DefaultShelfNodeColor,
                WarehouseLayoutNodeCategory.Zone => DefaultZoneNodeColor,
                _ => DefaultGenericNodeColor
            };
        }

        public Color NodeColorFor(WarehouseLayoutNodeCategory category)
        {
            return category switch
            {
                WarehouseLayoutNodeCategory.Dock => _dockNodeColor,
                WarehouseLayoutNodeCategory.Aisle => _aisleNodeColor,
                WarehouseLayoutNodeCategory.Shelf => _shelfNodeColor,
                WarehouseLayoutNodeCategory.Zone => _zoneNodeColor,
                _ => _genericNodeColor
            };
        }
    }
}
