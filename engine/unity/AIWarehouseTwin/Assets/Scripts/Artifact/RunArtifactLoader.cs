using System;
using System.IO;
using UnityEngine;

namespace AIWarehouseTwin.Artifact
{
    public static class RunArtifactLoader
    {
        public const string SupportedSchemaVersion = "run-artifact.v1";
        public const string SupportedArtifactKind = "warehouse-simulation-run";

        public static RunArtifactDto LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("RunArtifact path cannot be empty.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("RunArtifact file was not found.", path);
            }

            return LoadFromJson(File.ReadAllText(path));
        }

        public static RunArtifactDto LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("RunArtifact JSON cannot be empty.", nameof(json));
            }

            var artifact = JsonUtility.FromJson<RunArtifactDto>(json);
            if (artifact == null)
            {
                throw new InvalidOperationException("RunArtifact JSON could not be parsed.");
            }

            Normalize(artifact);
            Validate(artifact);
            return artifact;
        }

        private static void Normalize(RunArtifactDto artifact)
        {
            artifact.kpi_summary ??= new RunArtifactKpiSummaryDto();
            artifact.layout ??= new RunArtifactLayoutDto();
            artifact.layout.resources ??= Array.Empty<RunArtifactLayoutResourceDto>();
            artifact.position_timeline ??= Array.Empty<RunArtifactPositionTimelineEntryDto>();
            artifact.event_log ??= Array.Empty<string>();
        }

        private static void Validate(RunArtifactDto artifact)
        {
            if (artifact.schema_version != SupportedSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported RunArtifact schema_version '{artifact.schema_version}'.");
            }

            if (artifact.artifact_kind != SupportedArtifactKind)
            {
                throw new InvalidOperationException(
                    $"Unsupported RunArtifact artifact_kind '{artifact.artifact_kind}'.");
            }

            if (string.IsNullOrWhiteSpace(artifact.scenario_id))
            {
                throw new InvalidOperationException("RunArtifact scenario_id cannot be empty.");
            }

            if (artifact.kpi_summary.event_log_line_count != artifact.event_log.Length)
            {
                throw new InvalidOperationException(
                    "RunArtifact KPI event_log_line_count must match event_log length.");
            }

            foreach (var entry in artifact.position_timeline)
            {
                if (entry.at_ms < 0)
                {
                    throw new InvalidOperationException("RunArtifact position timeline time cannot be negative.");
                }

                if (entry.event_type != "start" && entry.event_type != "finish")
                {
                    throw new InvalidOperationException(
                        $"RunArtifact position timeline event_type must be start or finish: '{entry.event_type}'.");
                }
            }
        }
    }
}
