from __future__ import annotations

from ingestion.sources import IngestSource, ScenarioDocument


class StaticSource:
    @property
    def source_name(self) -> str:
        return "static-test-source"

    def describe(self) -> str:
        return "Static test source"

    def to_scenario(self) -> ScenarioDocument:
        return {
            "schema_version": "warehouse-scenario.v0",
            "scenario_id": "static-test",
            "seed": 1,
            "inbound": {
                "scenario_id": "static-test",
                "dock_count": 1,
                "forklift_count": 1,
                "process": {
                    "unload_duration_ms": 0,
                    "qc_duration_ms": 0,
                    "putaway_duration_ms": 0,
                },
                "receipts": [
                    {
                        "receipt_id": "r1",
                        "warehouse_id": "w1",
                        "sku_id": "sku1",
                        "quantity": 1,
                        "staging_location_id": "stage1",
                        "storage_location_id": "bin1",
                        "arrives_at_ms": 0,
                    }
                ],
            },
        }


def test_ingest_source_protocol_accepts_test_double() -> None:
    source: IngestSource = StaticSource()
    scenario = source.to_scenario()

    assert source.source_name == "static-test-source"
    assert source.describe() == "Static test source"
    assert scenario["schema_version"] == "warehouse-scenario.v0"
