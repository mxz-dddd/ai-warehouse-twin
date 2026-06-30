namespace AIWarehouseTwin.Rendering
{
    public readonly struct LayoutResourcePoint
    {
        public readonly string ResourceId;
        public readonly string NodeId;
        public readonly float X;
        public readonly float Y;
        public LayoutResourcePoint(string resourceId, string nodeId, float x, float y)
        {
            ResourceId = resourceId;
            NodeId = nodeId;
            X = x;
            Y = y;
        }
    }
}
