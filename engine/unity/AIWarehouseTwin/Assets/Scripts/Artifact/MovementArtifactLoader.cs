using System;
using System.Collections.Generic;
using System.IO;

namespace AIWarehouseTwin.Artifact
{
    public static class MovementArtifactLoader
    {
        public const string SupportedSchemaVersion = "movement-artifact.v1";
        public const string SupportedArtifactKind = "warehouse-movement";

        public static MovementArtifactDto LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("MovementArtifact path cannot be empty.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("MovementArtifact file was not found.", path);
            }

            return LoadFromJson(File.ReadAllText(path));
        }

        public static MovementArtifactLoadResult LoadOptionalFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return MovementArtifactLoadResult.Unavailable("MovementArtifact path was empty.");
            }

            if (!File.Exists(path))
            {
                return MovementArtifactLoadResult.Unavailable("MovementArtifact file was not found.");
            }

            return MovementArtifactLoadResult.Available(LoadFromJson(File.ReadAllText(path)));
        }

        public static MovementArtifactDto LoadFromJson(string json)
        {
            var root = ArtifactJson.ParseObject(json, nameof(MovementArtifactDto));
            var artifact = MapMovementArtifact(root);
            Normalize(artifact);
            Validate(artifact);
            return artifact;
        }

        private static MovementArtifactDto MapMovementArtifact(Dictionary<string, object> root)
        {
            return new MovementArtifactDto
            {
                schema_version = ArtifactJson.GetString(root, "schema_version"),
                artifact_kind = ArtifactJson.GetString(root, "artifact_kind"),
                scenario_id = ArtifactJson.GetString(root, "scenario_id"),
                run_id = ArtifactJson.GetString(root, "run_id"),
                seed = ArtifactJson.GetInt(root, "seed"),
                source_run_artifact = ArtifactJson.GetString(root, "source_run_artifact"),
                warehouse_graph = ArtifactJson.MapWarehouseGraph(ArtifactJson.GetObject(root, "warehouse_graph")),
                actors = ArtifactJson.MapArray(ArtifactJson.GetArray(root, "actors"), MapActor),
                movement_events = ArtifactJson.MapArray(
                    ArtifactJson.GetArray(root, "movement_events"),
                    MapMovementEvent),
                route_segments = ArtifactJson.MapArray(
                    ArtifactJson.GetArray(root, "route_segments"),
                    MapRouteSegment),
                provenance = MapProvenance(ArtifactJson.GetObject(root, "provenance"))
            };
        }

        private static MovementActorDto MapActor(Dictionary<string, object> actor)
        {
            return new MovementActorDto
            {
                actor_id = ArtifactJson.GetString(actor, "actor_id"),
                actor_type = ArtifactJson.GetString(actor, "actor_type"),
                resource_id = ArtifactJson.GetString(actor, "resource_id"),
                initial_node_id = ArtifactJson.GetString(actor, "initial_node_id"),
                capacity = ArtifactJson.GetDouble(actor, "capacity"),
                load_state = ArtifactJson.GetString(actor, "load_state")
            };
        }

        private static MovementEventDto MapMovementEvent(Dictionary<string, object> movementEvent)
        {
            return new MovementEventDto
            {
                event_id = ArtifactJson.GetString(movementEvent, "event_id"),
                actor_id = ArtifactJson.GetString(movementEvent, "actor_id"),
                operation_id = ArtifactJson.GetString(movementEvent, "operation_id"),
                event_type = ArtifactJson.GetString(movementEvent, "event_type"),
                at_ms = ArtifactJson.GetLong(movementEvent, "at_ms"),
                node_id = ArtifactJson.GetString(movementEvent, "node_id"),
                x = ArtifactJson.GetDouble(movementEvent, "x"),
                y = ArtifactJson.GetDouble(movementEvent, "y"),
                load_state = ArtifactJson.GetString(movementEvent, "load_state"),
                related_resource_id = ArtifactJson.GetString(movementEvent, "related_resource_id")
            };
        }

        private static MovementRouteSegmentDto MapRouteSegment(Dictionary<string, object> segment)
        {
            return new MovementRouteSegmentDto
            {
                segment_id = ArtifactJson.GetString(segment, "segment_id"),
                actor_id = ArtifactJson.GetString(segment, "actor_id"),
                operation_id = ArtifactJson.GetString(segment, "operation_id"),
                from_node_id = ArtifactJson.GetString(segment, "from_node_id"),
                to_node_id = ArtifactJson.GetString(segment, "to_node_id"),
                start_ms = ArtifactJson.GetLong(segment, "start_ms"),
                end_ms = ArtifactJson.GetLong(segment, "end_ms"),
                distance_m = ArtifactJson.GetDouble(segment, "distance_m"),
                path_node_ids = ArtifactJson.ToStringArray(ArtifactJson.GetArray(segment, "path_node_ids")),
                edge_ids = ArtifactJson.ToStringArray(ArtifactJson.GetArray(segment, "edge_ids")),
                travel_time_ms = ArtifactJson.GetLong(segment, "travel_time_ms")
            };
        }

        private static MovementProvenanceDto MapProvenance(Dictionary<string, object> provenance)
        {
            if (provenance == null)
            {
                return new MovementProvenanceDto();
            }

            return new MovementProvenanceDto
            {
                movement_generator_version =
                    ArtifactJson.GetString(provenance, "movement_generator_version"),
                graph_source = ArtifactJson.GetString(provenance, "graph_source"),
                movement_enabled = ArtifactJson.GetBool(provenance, "movement_enabled"),
                deterministic_generation_policy =
                    ArtifactJson.GetString(provenance, "deterministic_generation_policy")
            };
        }

        private static void Normalize(MovementArtifactDto artifact)
        {
            artifact.warehouse_graph ??= new WarehouseGraphDto();
            artifact.warehouse_graph.nodes ??= Array.Empty<WarehouseGraphNodeDto>();
            artifact.warehouse_graph.edges ??= Array.Empty<WarehouseGraphEdgeDto>();
            artifact.actors ??= Array.Empty<MovementActorDto>();
            artifact.movement_events ??= Array.Empty<MovementEventDto>();
            artifact.route_segments ??= Array.Empty<MovementRouteSegmentDto>();
            artifact.provenance ??= new MovementProvenanceDto();
        }

        private static void Validate(MovementArtifactDto artifact)
        {
            if (artifact.schema_version != SupportedSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported MovementArtifact schema_version '{artifact.schema_version}'.");
            }

            if (artifact.artifact_kind != SupportedArtifactKind)
            {
                throw new InvalidOperationException(
                    $"Unsupported MovementArtifact artifact_kind '{artifact.artifact_kind}'.");
            }

            if (string.IsNullOrWhiteSpace(artifact.scenario_id))
            {
                throw new InvalidOperationException("MovementArtifact scenario_id cannot be empty.");
            }
        }
    }

    public sealed class MovementArtifactLoadResult
    {
        private MovementArtifactLoadResult(bool isAvailable, string unavailableReason, MovementArtifactDto artifact)
        {
            IsAvailable = isAvailable;
            UnavailableReason = unavailableReason;
            Artifact = artifact;
        }

        public bool IsAvailable { get; }

        public string UnavailableReason { get; }

        public MovementArtifactDto Artifact { get; }

        public static MovementArtifactLoadResult Available(MovementArtifactDto artifact)
        {
            return new MovementArtifactLoadResult(true, string.Empty, artifact ?? new MovementArtifactDto());
        }

        public static MovementArtifactLoadResult Unavailable(string reason)
        {
            return new MovementArtifactLoadResult(false, reason ?? string.Empty, new MovementArtifactDto());
        }
    }
}
