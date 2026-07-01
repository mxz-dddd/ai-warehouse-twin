using UnityEngine;

namespace AIWarehouseTwin.Rendering.Actors
{
    public readonly struct ActorPose
    {
        public ActorPose(
            string actorId,
            Vector2 position,
            bool isAvailable,
            string loadState,
            string evidenceId,
            string evidenceKind,
            string sourceRunArtifact,
            string graphSource,
            string provenance)
        {
            ActorId = actorId ?? string.Empty;
            Position = position;
            IsAvailable = isAvailable;
            LoadState = loadState ?? string.Empty;
            EvidenceId = evidenceId ?? string.Empty;
            EvidenceKind = evidenceKind ?? string.Empty;
            SourceRunArtifact = sourceRunArtifact ?? string.Empty;
            GraphSource = graphSource ?? string.Empty;
            Provenance = provenance ?? string.Empty;
        }

        public string ActorId { get; }

        public Vector2 Position { get; }

        public bool IsAvailable { get; }

        public string LoadState { get; }

        public string EvidenceId { get; }

        public string EvidenceKind { get; }

        public string SourceRunArtifact { get; }

        public string GraphSource { get; }

        public string Provenance { get; }

        public static ActorPose Unavailable(string actorId = "")
        {
            return new ActorPose(
                actorId,
                Vector2.zero,
                isAvailable: false,
                loadState: string.Empty,
                evidenceId: string.Empty,
                evidenceKind: "unavailable",
                sourceRunArtifact: string.Empty,
                graphSource: string.Empty,
                provenance: string.Empty);
        }
    }
}
