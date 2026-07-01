using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AIWarehouseTwin.Graph
{
    // Thin source adapter: reads the sample warehouse layout document and maps
    // its path_nodes / path_edges into the frozen WarehouseGraph abstraction.
    // This is the only seam that knows about the layout.json field shape; the
    // rendering layer consumes WarehouseGraph and never sees the raw document,
    // so an artifact-backed graph producer can replace this source later.
    public static class LayoutGraphSource
    {
        public static WarehouseGraph FromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Layout document path cannot be empty.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Layout document file was not found.", path);
            }

            return FromJson(File.ReadAllText(path));
        }

        public static WarehouseGraph FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Layout document JSON cannot be empty.", nameof(json));
            }

            var dto = JsonUtility.FromJson<LayoutDocumentDto>(json);
            if (dto == null)
            {
                throw new InvalidOperationException("Layout document JSON could not be parsed.");
            }

            return FromDocument(dto);
        }

        // Pure conversion — no engine or filesystem dependency, EditMode-testable.
        public static WarehouseGraph FromDocument(LayoutDocumentDto document)
        {
            var nodes = new List<WarehouseGraphNode>();
            var edges = new List<WarehouseGraphEdge>();

            if (document?.path_nodes != null)
            {
                foreach (var n in document.path_nodes)
                {
                    if (n == null || string.IsNullOrEmpty(n.node_id)) continue;
                    nodes.Add(new WarehouseGraphNode(
                        n.node_id, n.node_type, (float)n.x, (float)n.y));
                }
            }

            if (document?.path_edges != null)
            {
                foreach (var e in document.path_edges)
                {
                    if (e == null || string.IsNullOrEmpty(e.edge_id)) continue;
                    edges.Add(new WarehouseGraphEdge(
                        e.edge_id, e.from_node_id, e.to_node_id, e.bidirectional));
                }
            }

            return new WarehouseGraph(nodes, edges);
        }
    }
}
