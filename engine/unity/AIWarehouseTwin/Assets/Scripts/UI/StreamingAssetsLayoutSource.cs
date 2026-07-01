using AIWarehouseTwin.Graph;
using UnityEngine;

namespace AIWarehouseTwin.UI
{
    // Loads the warehouse path graph from a layout document shipped in
    // StreamingAssets. Read-only: the source document is never written back.
    public sealed class StreamingAssetsLayoutSource
    {
        private readonly string layoutFileName;

        public StreamingAssetsLayoutSource(string layoutFileName = "layout.json")
        {
            this.layoutFileName = layoutFileName;
        }

        public string Path => System.IO.Path.Combine(Application.streamingAssetsPath, layoutFileName);

        public WarehouseGraph Load()
        {
            return LayoutGraphSource.FromFile(Path);
        }
    }
}
