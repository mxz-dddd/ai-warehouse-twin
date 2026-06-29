from __future__ import annotations

from ingestion.quality import QualityIssue, QualityReport


def test_quality_report_sorts_issues_and_renders_markdown_table() -> None:
    report = QualityReport(
        source_name="unit-test",
        input_files=("orders.csv", "inventory.csv"),
        record_counts={"orders": 1, "inventory": 1},
        issues=(
            QualityIssue(
                severity="warning",
                code="late_release",
                source_file="orders.csv",
                row_number=2,
                field="released_at_ms",
                message="release is later than expected",
            ),
            QualityIssue(
                severity="error",
                code="unknown_sku",
                source_file="orders.csv",
                row_number=2,
                field="sku_id",
                message="sku_id references unknown sku 'sku-x'",
            ),
            QualityIssue(
                severity="error",
                code="negative_quantity",
                source_file="inventory.csv",
                row_number=3,
                field="quantity",
                message="quantity must be a positive integer; got -1",
            ),
        ),
        coverage_summary={"skus": "1", "order_skus_covered": "0/1"},
        scenario_output_summary={"orders": "1", "inventory_items": "1"},
    )

    assert [issue.code for issue in report.sorted_issues] == [
        "negative_quantity",
        "unknown_sku",
        "late_release",
    ]

    markdown = report.render_markdown()

    assert "| severity | code | file | row | field | message |" in markdown
    assert "| error | negative_quantity | inventory.csv | 3 | quantity |" in markdown
    assert "| warning | late_release | orders.csv | 2 | released_at_ms |" in markdown
    assert "## Coverage Summary" in markdown
    assert "## Scenario Output Summary" in markdown
