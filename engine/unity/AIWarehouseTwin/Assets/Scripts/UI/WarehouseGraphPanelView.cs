using AIWarehouseTwin.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    // Wires the static path-graph view into a UIDocument canvas and stamps an
    // honesty label. This is a frozen layout floor plan, not simulated movement.
    public sealed class WarehouseGraphPanelView : MonoBehaviour
    {
        // Shown verbatim so the delivered view never implies motion.
        public const string HonestyLabelText =
            "Static warehouse layout (path graph) — baseline positions, not simulated movement.";

        [SerializeField] private UIDocument _document;
        private WarehouseGraphView _graphView;

        private void Awake()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            WarehouseGraph graph = new StreamingAssetsLayoutSource().Load();
            var root = _document.rootVisualElement;

            var canvas = root.Q<VisualElement>("layout-canvas") ?? root;
            _graphView = new WarehouseGraphView();
            canvas.Add(_graphView);
            _graphView.SetGraph(graph);

            var honesty = root.Q<Label>("layout-honesty-label");
            if (honesty != null)
            {
                honesty.text = HonestyLabelText;
            }
        }
    }
}
