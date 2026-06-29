using System.IO;
using AIWarehouseTwin.Artifact;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class RunArtifactLoaderTests
    {
        [Test]
        public void LoadFromJson_reads_run_artifact_v1_golden_fields()
        {
            var artifact = RunArtifactLoader.LoadFromJson(LoadGoldenJson());

            Assert.That(artifact.schema_version, Is.EqualTo("run-artifact.v1"));
            Assert.That(artifact.artifact_kind, Is.EqualTo("warehouse-simulation-run"));
            Assert.That(artifact.scenario_id, Is.EqualTo("sample-small-warehouse"));
            Assert.That(artifact.seed, Is.EqualTo(20240627));
            Assert.That(artifact.started_at_ms, Is.EqualTo(10));
            Assert.That(artifact.finished_at_ms, Is.EqualTo(220));
            Assert.That(artifact.final_world_time_ms, Is.EqualTo(220));
            Assert.That(artifact.kpi_summary.total_duration_ms, Is.EqualTo(210));
            Assert.That(artifact.kpi_summary.total_completed_work_items, Is.EqualTo(3));
            Assert.That(artifact.kpi_summary.event_log_line_count, Is.EqualTo(10));
            Assert.That(artifact.layout.resources, Has.Length.EqualTo(4));
            Assert.That(artifact.position_timeline, Has.Length.EqualTo(12));
            Assert.That(artifact.event_log, Has.Length.EqualTo(10));
        }

        [Test]
        public void LoadFromJson_rejects_unknown_schema_version()
        {
            var json = LoadGoldenJson().Replace("run-artifact.v1", "run-artifact.v2");

            Assert.Throws<System.InvalidOperationException>(() => RunArtifactLoader.LoadFromJson(json));
        }

        private static string LoadGoldenJson()
        {
            return File.ReadAllText(Path.Combine(
                Application.dataPath,
                "StreamingAssets",
                "run-artifact.v1.json"));
        }
    }
}
