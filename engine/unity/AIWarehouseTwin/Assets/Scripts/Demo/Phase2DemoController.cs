using System;
using System.Collections.Generic;
using System.IO;
using AIWarehouseTwin.Artifact;
using UnityEngine;

namespace AIWarehouseTwin.Demo
{
    public sealed class Phase2DemoController : MonoBehaviour
    {
        public const string ArtifactFileName = "medium-warehouse-artifact.json";

        [SerializeField]
        private bool createPlaceholderObjects = true;

        private void Start()
        {
            var artifactPath = Path.Combine(Application.streamingAssetsPath, ArtifactFileName);
            var artifact = RunArtifactLoader.LoadFromFile(artifactPath);
            var summary = BuildSummary(artifact);

            Debug.Log(FormatLoadedMessage(summary));

            if (createPlaceholderObjects)
            {
                CreatePlaceholderObjects(artifact);
            }
        }

        public static Phase2DemoSummary BuildSummary(RunArtifactDto artifact)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            var actors = new HashSet<string>(StringComparer.Ordinal);
            var routes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in artifact.position_timeline)
            {
                AddIfPresent(actors, entry.resource_id);
                AddIfPresent(routes, entry.operation_id);
            }

            if (actors.Count == 0)
            {
                foreach (var resource in artifact.layout.resources)
                {
                    AddIfPresent(actors, resource.resource_id);
                }
            }

            return new Phase2DemoSummary(actors.Count, routes.Count);
        }

        public static string FormatLoadedMessage(Phase2DemoSummary summary)
        {
            return $"Scene loaded. Actors: {summary.ActorCount}, Routes: {summary.RouteCount}";
        }

        private static void AddIfPresent(ISet<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        private static void CreatePlaceholderObjects(RunArtifactDto artifact)
        {
            var root = new GameObject("Phase2 Demo Loaded Artifact");

            foreach (var resource in artifact.layout.resources)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Resource {resource.resource_id}";
                marker.transform.SetParent(root.transform);
                marker.transform.position = new Vector3((float)resource.x, 0f, (float)resource.y);
                marker.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            }
        }
    }

    public readonly struct Phase2DemoSummary
    {
        public Phase2DemoSummary(int actorCount, int routeCount)
        {
            ActorCount = actorCount;
            RouteCount = routeCount;
        }

        public int ActorCount { get; }

        public int RouteCount { get; }
    }
}
