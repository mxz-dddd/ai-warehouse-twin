using System;

namespace AIWarehouseTwin.UI
{
    public sealed class RunArtifactPlayerState
    {
        public string ScenarioId { get; init; } = string.Empty;

        public int Seed { get; init; }

        public long CurrentTimeMs { get; init; }

        public long StartTimeMs { get; init; }

        public long EndTimeMs { get; init; }

        public bool IsPlaying { get; init; }

        public string[] KpiRows { get; init; } = Array.Empty<string>();

        public string[] EventRows { get; init; } = Array.Empty<string>();
    }
}
