using System.IO;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.Demo;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class Phase2DemoControllerTests
    {
        [Test]
        public void MediumArtifact_exists_in_streaming_assets()
        {
            Assert.That(File.Exists(MediumArtifactPath()), Is.True);
        }

        [Test]
        public void BuildSummary_counts_actors_and_routes_from_medium_artifact()
        {
            var artifact = RunArtifactLoader.LoadFromFile(MediumArtifactPath());

            var summary = Phase2DemoController.BuildSummary(artifact);

            Assert.That(summary.ActorCount, Is.EqualTo(4));
            Assert.That(summary.RouteCount, Is.EqualTo(17));
        }

        [Test]
        public void FormatLoadedMessage_uses_required_console_format()
        {
            var message = Phase2DemoController.FormatLoadedMessage(new Phase2DemoSummary(4, 17));

            Assert.That(message, Is.EqualTo("Scene loaded. Actors: 4, Routes: 17"));
        }

        [Test]
        public void BuildSummary_falls_back_to_layout_resources_when_timeline_is_empty()
        {
            var artifact = new RunArtifactDto
            {
                schema_version = "run-artifact.v1",
                artifact_kind = "warehouse-simulation-run",
                scenario_id = "demo",
                layout = new RunArtifactLayoutDto
                {
                    resources = new[]
                    {
                        new RunArtifactLayoutResourceDto { resource_id = "dock-1" },
                        new RunArtifactLayoutResourceDto { resource_id = "station-1" },
                    },
                },
                position_timeline = new RunArtifactPositionTimelineEntryDto[0],
                event_log = new string[0],
            };

            var summary = Phase2DemoController.BuildSummary(artifact);

            Assert.That(summary.ActorCount, Is.EqualTo(2));
            Assert.That(summary.RouteCount, Is.EqualTo(0));
        }

        private static string MediumArtifactPath()
        {
            return Path.Combine(
                Application.dataPath,
                "StreamingAssets",
                Phase2DemoController.ArtifactFileName);
        }
    }
}
