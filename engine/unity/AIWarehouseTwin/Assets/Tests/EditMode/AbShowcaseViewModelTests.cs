using System.IO;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.UI.Showcase;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class AbShowcaseViewModelTests
    {
        [Test]
        public void Mock_fixture_parses_through_comparison_artifact_loader()
        {
            var artifact = LoadFixtureArtifact();
            var model = AbShowcasePresenter.FromComparisonArtifact(
                artifact,
                AbShowcasePresenter.MockSourceLabel);

            Assert.That(artifact.schema_version, Is.EqualTo("comparison_artifact.v1.r3"));
            Assert.That(model.IsAvailable, Is.True);
            Assert.That(model.IsMock, Is.True);
            Assert.That(model.SourceLabel, Does.Contain("Mock comparison fixture"));
            Assert.That(model.EvidenceLabel, Does.Contain("not a real optimization result"));
        }

        [Test]
        public void Kpi_deltas_display_baseline_candidate_delta_and_lower_is_better()
        {
            var model = LoadFixtureModel();
            var row = FindRow(model, "order_cycle_p50_ms");

            Assert.That(model.KpiRows, Has.Count.GreaterThan(0));
            Assert.That(row.BaselineValue, Is.EqualTo(120000d));
            Assert.That(row.CandidateValue, Is.EqualTo(90000d));
            Assert.That(row.Delta, Is.EqualTo(-30000d));
            Assert.That(row.LowerIsBetter, Is.True);
            Assert.That(row.Direction, Is.EqualTo("lower_is_better"));
        }

        [Test]
        public void Improvement_pct_display_uses_fixture_value()
        {
            var row = FindRow(LoadFixtureModel(), "order_cycle_p50_ms");

            Assert.That(row.ImprovementPct, Is.EqualTo(25d));
            Assert.That(row.ImprovementDisplay, Is.EqualTo("+25.0%"));
        }

        [Test]
        public void Lower_and_higher_is_better_direction_labels_are_distinct()
        {
            var model = LoadFixtureModel();
            var lowerRow = FindRow(model, "order_cycle_p50_ms");
            var higherRow = FindRow(model, "total_work_item_throughput_per_hour");

            Assert.That(lowerRow.DirectionLabel, Is.EqualTo("Lower is better"));
            Assert.That(higherRow.DirectionLabel, Is.EqualTo("Higher is better"));
            Assert.That(higherRow.LowerIsBetter, Is.False);
            Assert.That(higherRow.Direction, Is.EqualTo("higher_is_better"));
        }

        [Test]
        public void Candidate_side_maps_to_optimized_display_label_without_optimized_dto_field()
        {
            var model = LoadFixtureModel();

            Assert.That(model.Baseline.DtoFieldName, Is.EqualTo("baseline"));
            Assert.That(model.Baseline.DisplayLabel, Is.EqualTo("Baseline"));
            Assert.That(model.Baseline.ScenarioId, Is.EqualTo("mock-baseline-demo"));
            Assert.That(model.Candidate.DtoFieldName, Is.EqualTo("candidate"));
            Assert.That(model.Candidate.DisplayLabel, Is.EqualTo("Optimized"));
            Assert.That(model.Candidate.ScenarioId, Is.EqualTo("mock-candidate-slotting-demo"));
        }

        [Test]
        public void Missing_comparison_artifact_returns_unavailable_view_model()
        {
            var missingPath = Path.Combine(Path.GetTempPath(), "awt-missing-b3-comparison.json");

            var model = AbShowcasePresenter.FromFile(missingPath);

            Assert.That(model.IsAvailable, Is.False);
            Assert.That(model.UnavailableReason, Does.Contain("unavailable"));
            Assert.That(model.KpiRows, Is.Empty);
            Assert.That(model.IsMock, Is.True);
        }

        [Test]
        public void Missing_kpi_deltas_returns_empty_rows_without_throwing()
        {
            var model = AbShowcasePresenter.FromJson(MinimalComparisonWithoutKpiDeltas());

            Assert.That(model.IsAvailable, Is.True);
            Assert.That(model.KpiRows, Is.Empty);
        }

        [Test]
        public void Missing_improvement_pct_uses_display_fallback()
        {
            var model = AbShowcasePresenter.FromJson(ComparisonWithoutImprovementPct());
            var row = FindRow(model, "order_cycle_p50_ms");

            Assert.That(row.ImprovementPct, Is.Null);
            Assert.That(row.ImprovementDisplay, Is.EqualTo("N/A"));
            Assert.That(row.TrendLabel, Is.EqualTo("N/A"));
        }

        [Test]
        public void Empty_deltas_are_safe_fallback()
        {
            var model = AbShowcasePresenter.FromJson(ComparisonWithEmptyDeltas());

            Assert.That(model.IsAvailable, Is.True);
            Assert.That(model.HasDeltas, Is.False);
            Assert.That(model.DeltaCount, Is.Zero);
        }

        private static AbShowcaseViewModel LoadFixtureModel() =>
            AbShowcasePresenter.FromComparisonArtifact(
                LoadFixtureArtifact(),
                AbShowcasePresenter.MockSourceLabel);

        private static ComparisonArtifactDto LoadFixtureArtifact() =>
            ComparisonArtifactLoader.LoadFromFile(FixturePath("b3-comparison-artifact.mock.json"));

        private static AbShowcaseKpiRow FindRow(AbShowcaseViewModel model, string metricName)
        {
            foreach (var row in model.KpiRows)
            {
                if (row.MetricName == metricName)
                {
                    return row;
                }
            }

            Assert.Fail($"Expected KPI row '{metricName}' was not found.");
            return null;
        }

        private static string FixturePath(string fileName)
        {
            return Path.Combine(
                Application.dataPath,
                "Tests",
                "EditMode",
                "Fixtures",
                fileName);
        }

        private static string MinimalComparisonWithoutKpiDeltas()
        {
            return @"{
  ""schema_version"": ""comparison_artifact.v1.r3"",
  ""baseline"": { ""scenario_id"": ""mock-baseline"", ""metrics"": {} },
  ""candidate"": { ""scenario_id"": ""mock-candidate"", ""metrics"": {} },
  ""deltas"": [],
  ""improvement_pct"": {}
}";
        }

        private static string ComparisonWithoutImprovementPct()
        {
            return @"{
  ""schema_version"": ""comparison_artifact.v1.r3"",
  ""baseline"": { ""scenario_id"": ""mock-baseline"", ""metrics"": {} },
  ""candidate"": { ""scenario_id"": ""mock-candidate"", ""metrics"": {} },
  ""deltas"": [
    {
      ""metric_name"": ""order_cycle_p50_ms"",
      ""baseline_value"": 120000,
      ""candidate_value"": 90000,
      ""delta"": -30000,
      ""delta_percent"": -25,
      ""direction"": ""lower_is_better""
    }
  ],
  ""kpi_deltas"": {
    ""order_cycle_p50_ms"": {
      ""baseline_value"": 120000,
      ""candidate_value"": 90000,
      ""delta"": -30000,
      ""lower_is_better"": true
    }
  }
}";
        }

        private static string ComparisonWithEmptyDeltas()
        {
            return @"{
  ""schema_version"": ""comparison_artifact.v1.r3"",
  ""baseline"": { ""scenario_id"": ""mock-baseline"", ""metrics"": {} },
  ""candidate"": { ""scenario_id"": ""mock-candidate"", ""metrics"": {} },
  ""deltas"": [],
  ""kpi_deltas"": {
    ""total_work_item_throughput_per_hour"": {
      ""baseline_value"": 720,
      ""candidate_value"": 930,
      ""delta"": 210,
      ""lower_is_better"": false
    }
  },
  ""improvement_pct"": {
    ""total_work_item_throughput_per_hour"": 29.1667
  }
}";
        }
    }
}
