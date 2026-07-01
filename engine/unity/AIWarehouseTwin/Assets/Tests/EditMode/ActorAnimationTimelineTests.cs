using System;
using System.IO;
using System.Linq;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Rendering.Actors;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class ActorAnimationTimelineTests
    {
        [Test]
        public void Single_actor_single_segment_interpolates_via_warehouse_graph_nodes()
        {
            var timeline = ActorTimelineBuilder.FromArtifact(
                MovementArtifactLoader.LoadFromJson(MovementJson()));

            var pose = ActorAnimationSampler.Sample(timeline, "forklift-1", 50);

            Assert.That(pose.IsAvailable, Is.True);
            Assert.That(pose.Position.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(pose.Position.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(pose.EvidenceKind, Is.EqualTo("route_segment"));
            Assert.That(pose.EvidenceId, Is.EqualTo("seg-1"));
        }

        [Test]
        public void Multi_segment_actor_is_continuous_at_segment_boundary()
        {
            var timeline = ActorTimelineBuilder.FromArtifact(
                MovementArtifactLoader.LoadFromJson(MovementJson()));

            var endPose = ActorAnimationSampler.Sample(timeline, "forklift-1", 100);
            var nextStartPose = ActorAnimationSampler.Sample(timeline, "forklift-1", 100);

            Assert.That(endPose.Position.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(endPose.Position.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(nextStartPose.Position, Is.EqualTo(endPose.Position));
        }

        [Test]
        public void Segment_time_boundaries_clamp_to_start_and_end_positions()
        {
            var timeline = ActorTimelineBuilder.FromArtifact(
                MovementArtifactLoader.LoadFromJson(MovementJson()));

            var before = ActorAnimationSampler.Sample(timeline, "forklift-1", -10);
            var start = ActorAnimationSampler.Sample(timeline, "forklift-1", 0);
            var end = ActorAnimationSampler.Sample(timeline, "forklift-1", 200);
            var after = ActorAnimationSampler.Sample(timeline, "forklift-1", 250);

            Assert.That(before.Position, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(start.Position, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(end.Position, Is.EqualTo(new Vector2(10f, 10f)));
            Assert.That(after.Position, Is.EqualTo(new Vector2(10f, 10f)));
        }

        [Test]
        public void Missing_movement_artifact_returns_empty_animation()
        {
            var missingPath = Path.Combine(
                Path.GetTempPath(),
                $"awt-b2-missing-{Guid.NewGuid():N}.json");

            var timeline = ActorTimelineBuilder.FromLoadResult(
                MovementArtifactLoader.LoadOptionalFromFile(missingPath));
            var pose = ActorAnimationSampler.Sample(timeline, "forklift-1", 0);

            Assert.That(timeline.IsEmpty, Is.True);
            Assert.That(pose.IsAvailable, Is.False);
        }

        [Test]
        public void Unknown_node_route_segment_is_skipped_and_event_fallback_is_used()
        {
            var timeline = ActorTimelineBuilder.FromArtifact(
                MovementArtifactLoader.LoadFromJson(MovementJson(
                    @"""to_node_id"": ""missing-node""")));

            var pose = ActorAnimationSampler.Sample(timeline, "forklift-1", 80);

            Assert.That(pose.IsAvailable, Is.True);
            Assert.That(pose.EvidenceKind, Is.EqualTo("movement_event"));
            Assert.That(pose.EvidenceId, Is.EqualTo("evt-1"));
            Assert.That(pose.Position, Is.EqualTo(new Vector2(2f, 3f)));
        }

        [Test]
        public void Empty_route_segments_falls_back_to_nearest_movement_event()
        {
            var timeline = ActorTimelineBuilder.FromArtifact(
                MovementArtifactLoader.LoadFromJson(MovementJson(
                    routeSegmentsJson: "[]")));

            var pose = ActorAnimationSampler.Sample(timeline, "forklift-1", 80);

            Assert.That(pose.IsAvailable, Is.True);
            Assert.That(pose.EvidenceKind, Is.EqualTo("movement_event"));
            Assert.That(pose.Position, Is.EqualTo(new Vector2(2f, 3f)));
        }

        [Test]
        public void Load_state_prefers_latest_movement_event_then_actor_state()
        {
            var timeline = ActorTimelineBuilder.FromArtifact(
                MovementArtifactLoader.LoadFromJson(MovementJson()));

            var beforeEvent = ActorAnimationSampler.Sample(timeline, "forklift-1", 10);
            var afterEvent = ActorAnimationSampler.Sample(timeline, "forklift-1", 80);

            Assert.That(beforeEvent.LoadState, Is.EqualTo("empty"));
            Assert.That(afterEvent.LoadState, Is.EqualTo("loaded"));
        }

        [Test]
        public void Evidence_source_and_provenance_are_preserved_for_render_model()
        {
            var timeline = ActorTimelineBuilder.FromArtifact(
                MovementArtifactLoader.LoadFromJson(MovementJson()));

            var pose = ActorAnimationSampler.Sample(timeline, "forklift-1", 50);

            Assert.That(pose.SourceRunArtifact, Is.EqualTo("run-artifact.v1.json"));
            Assert.That(pose.GraphSource, Is.EqualTo("fixture graph"));
            Assert.That(pose.Provenance, Does.Contain("deterministic modeled movement"));
            Assert.That(pose.EvidenceId, Is.EqualTo("seg-1"));
            Assert.That(pose.EvidenceKind, Is.EqualTo("route_segment"));
        }

        [Test]
        public void B2_actor_animation_does_not_reference_backend_core_or_run_position_timeline()
        {
            var source = ReadActorAnimationSource();

            Assert.That(source, Does.Contain("MovementArtifactDto"));
            Assert.That(source, Does.Contain("warehouse_graph"));
            Assert.That(source, Does.Not.Contain("Sim." + "Core"));
            Assert.That(source, Does.Not.Contain("Path" + "Graph"));
            Assert.That(source, Does.Not.Contain("Layout" + "GraphLoader"));
            Assert.That(source, Does.Not.Contain("position" + "_timeline"));
        }

        private static string ReadActorAnimationSource()
        {
            var directory = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "Scripts",
                "Rendering",
                "Actors"));
            return string.Join(
                "\n",
                Directory.GetFiles(directory, "*.cs")
                    .Select(File.ReadAllText));
        }

        private static string MovementJson(
            string firstSegmentToNodeOverride = null,
            string routeSegmentsJson = null)
        {
            var firstSegmentToNode = firstSegmentToNodeOverride ??
                @"""to_node_id"": ""node-b""";
            routeSegmentsJson ??= $@"[
    {{
      ""segment_id"": ""seg-1"",
      ""actor_id"": ""forklift-1"",
      ""operation_id"": ""op-1"",
      ""from_node_id"": ""node-a"",
      {firstSegmentToNode},
      ""start_ms"": 0,
      ""end_ms"": 100,
      ""distance_m"": 10,
      ""path_node_ids"": [""node-a"", ""node-b""],
      ""edge_ids"": [""edge-a-b""],
      ""travel_time_ms"": 100
    }},
    {{
      ""segment_id"": ""seg-2"",
      ""actor_id"": ""forklift-1"",
      ""operation_id"": ""op-1"",
      ""from_node_id"": ""node-b"",
      ""to_node_id"": ""node-c"",
      ""start_ms"": 100,
      ""end_ms"": 200,
      ""distance_m"": 10,
      ""path_node_ids"": [""node-b"", ""node-c""],
      ""edge_ids"": [""edge-b-c""],
      ""travel_time_ms"": 100
    }}
  ]";

            return $@"{{
  ""schema_version"": ""movement-artifact.v1"",
  ""artifact_kind"": ""warehouse-movement"",
  ""scenario_id"": ""b2-fixture"",
  ""run_id"": ""run-1"",
  ""seed"": 7,
  ""source_run_artifact"": ""run-artifact.v1.json"",
  ""warehouse_graph"": {{
    ""nodes"": [
      {{ ""node_id"": ""node-a"", ""node_type"": ""dock"", ""x"": 0, ""y"": 0 }},
      {{ ""node_id"": ""node-b"", ""node_type"": ""aisle"", ""x"": 10, ""y"": 0 }},
      {{ ""node_id"": ""node-c"", ""node_type"": ""aisle"", ""x"": 10, ""y"": 10 }}
    ],
    ""edges"": []
  }},
  ""actors"": [
    {{
      ""actor_id"": ""forklift-1"",
      ""actor_type"": ""forklift"",
      ""resource_id"": ""forklift-1"",
      ""initial_node_id"": ""node-a"",
      ""capacity"": 1200,
      ""load_state"": ""empty""
    }}
  ],
  ""movement_events"": [
    {{
      ""event_id"": ""evt-1"",
      ""actor_id"": ""forklift-1"",
      ""operation_id"": ""op-1"",
      ""event_type"": ""load_changed"",
      ""at_ms"": 75,
      ""node_id"": ""node-a"",
      ""x"": 2,
      ""y"": 3,
      ""load_state"": ""loaded"",
      ""related_resource_id"": ""dock-1""
    }}
  ],
  ""route_segments"": {routeSegmentsJson},
  ""provenance"": {{
    ""movement_generator_version"": ""test"",
    ""graph_source"": ""fixture graph"",
    ""movement_enabled"": true,
    ""deterministic_generation_policy"": ""deterministic modeled movement, not sensor tracking""
  }}
}}";
        }
    }
}
