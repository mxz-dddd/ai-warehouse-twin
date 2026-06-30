using System.IO;
using AIWarehouseTwin.Artifact;
using UnityEngine;

namespace AIWarehouseTwin.UI
{
    public sealed class StreamingAssetsArtifactSource
    {
        private readonly string artifactFileName;

        public StreamingAssetsArtifactSource(string artifactFileName = "run-artifact.v1.json")
        {
            this.artifactFileName = artifactFileName;
        }

        public string Path => System.IO.Path.Combine(Application.streamingAssetsPath, artifactFileName);

        public RunArtifactDto Load()
        {
            return RunArtifactLoader.LoadFromFile(Path);
        }
    }
}
