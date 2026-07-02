using System.IO;
using System.Linq;
using AIWarehouseTwin.Artifact;
using AIWarehouseTwin.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.Tests
{
    public sealed class KpiHudPanelTests
    {
        [Test]
        public void Structured_kpi_rows_include_extended_contract_r3_fields()
        {
            var rows = KpiSummaryFormatter.FormatStructured(LoadContractR3RunArtifact());

            AssertRow(rows, "Cycle time", "Order cycle p50", "95000 ms");
            AssertRow(rows, "Cycle time", "Average wait", "18500 ms");
            AssertRow(rows, "Resource utilization", "forklift-1", "67.5%");
            AssertRow(rows, "Actor distance (artifact)", "worker", "36.25 m");

            Assert.That(rows.Any(row =>
                row.Section == "Bottlenecks" &&
                row.Label == "#1 forklift-1 (forklift)" &&
                row.Value.Contains("22000 ms avg wait")), Is.True);
        }

        [Test]
        public void Structured_kpi_rows_use_na_for_missing_nullable_fields()
        {
            var rows = KpiSummaryFormatter.FormatStructured(new RunArtifactDto
            {
                scenario_id = "minimal",
                kpi_summary = new RunArtifactKpiSummaryDto()
            });

            AssertRow(rows, "Cycle time", "Order cycle p50", "N/A");
            AssertRow(rows, "Cycle time", "Order cycle p90", "N/A");
            AssertRow(rows, "Cycle time", "Order cycle p95", "N/A");
            AssertRow(rows, "Cycle time", "Average wait", "N/A");
        }

        [Test]
        public void RefreshUi_replaces_existing_rows_without_accumulating_labels()
        {
            var root = new VisualElement();
            var first = new RunArtifactPlayerState
            {
                KpiHudRows = new[] { new KpiSummaryRow("Overview", "Scenario", "first") }
            };
            var second = new RunArtifactPlayerState
            {
                KpiHudRows = new[] { new KpiSummaryRow("Overview", "Scenario", "second") }
            };

            KpiHudPanel.RefreshUi(first, root);
            KpiHudPanel.RefreshUi(second, root);

            var labels = root.Query<Label>().ToList().Select(label => label.text).ToArray();
            Assert.That(labels, Has.Member("second"));
            Assert.That(labels, Has.No.Member("first"));
        }

        [Test]
        public void Controller_state_exposes_structured_kpi_hud_rows()
        {
            var controller = new RunArtifactPlayerController(LoadGoldenArtifact());

            var rows = controller.State.KpiHudRows;

            AssertRow(rows, "Overview", "Scenario", "sample-small-warehouse");
            AssertRow(rows, "Throughput", "Total/hour", "51428.571");
        }

        private static void AssertRow(
            KpiSummaryRow[] rows,
            string section,
            string label,
            string value)
        {
            Assert.That(rows.Any(row =>
                row.Section == section &&
                row.Label == label &&
                row.Value == value), Is.True);
        }

        private static RunArtifactDto LoadContractR3RunArtifact() =>
            RunArtifactLoader.LoadFromJson(File.ReadAllText(ContractFixturePath("contract_r3_run_fixture.json")));

        private static RunArtifactDto LoadGoldenArtifact() =>
            RunArtifactLoader.LoadFromJson(File.ReadAllText(
                Path.Combine(Application.dataPath, "StreamingAssets", "run-artifact.v1.json")));

        private static string ContractFixturePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "..",
                "..",
                "packages",
                "contracts",
                "fixtures",
                "contract-r3",
                fileName));
        }
    }
}
