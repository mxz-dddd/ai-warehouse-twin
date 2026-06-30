from __future__ import annotations

from pathlib import Path

import pytest

from ingestion.schema import get_schema_info, load_scenario_schema, scenario_schema_path

REPO_ROOT = Path(__file__).resolve().parents[3]


def test_schema_loader_reads_repo_schema() -> None:
    schema = load_scenario_schema(REPO_ROOT)

    assert schema["title"] == "WarehouseScenario"
    assert schema["properties"]["schema_version"]["const"] == "warehouse-scenario.v0"
    assert scenario_schema_path(REPO_ROOT).name == "scenario.schema.json"


def test_schema_info_extracts_basic_metadata() -> None:
    info = get_schema_info(REPO_ROOT)

    assert info.title == "WarehouseScenario"
    assert info.schema_version == "warehouse-scenario.v0"
    assert info.required_fields == ("schema_version", "scenario_id", "seed")


def test_schema_loader_reports_missing_schema(tmp_path: Path) -> None:
    with pytest.raises(FileNotFoundError, match="Scenario schema not found"):
        load_scenario_schema(tmp_path)
