using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public sealed class WarehouseLayoutPanelView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        private WarehouseLayoutView _layoutView;

        private void Start()
        {
            var artifact = new StreamingAssetsArtifactSource().Load();
            var root = _document.rootVisualElement;
            var canvas = root.Q<VisualElement>("layout-canvas");
            _layoutView = new WarehouseLayoutView();
            canvas.Add(_layoutView);
            _layoutView.SetArtifact(artifact);
        }
    }
}
