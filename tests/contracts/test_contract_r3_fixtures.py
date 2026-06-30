import importlib.util
import json
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
FIXTURE_DIR = ROOT / "packages" / "contracts" / "fixtures" / "contract-r3"
ARTIFACT_SCHEMA_DIR = ROOT / "packages" / "contracts" / "artifacts"
GENERATED_PYTHON = (
    ROOT
    / "packages"
    / "contracts"
    / "generated"
    / "python"
    / "contracts_generated.py"
)
GENERATED_CSHARP = (
    ROOT
    / "packages"
    / "contracts"
    / "generated"
    / "csharp"
    / "Contracts.Generated.cs"
)


def load_json(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def load_generated_module():
    spec = importlib.util.spec_from_file_location(
        "contracts_generated",
        GENERATED_PYTHON,
    )
    assert spec is not None
    assert spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def resolve_ref(ref: str, root_schema: dict[str, Any]) -> dict[str, Any]:
    if ref.startswith("#/"):
        target: Any = root_schema
        for part in ref[2:].split("/"):
            target = target[part]
        return target

    path_part, _, pointer = ref.partition("#/")
    target_schema = load_json(ARTIFACT_SCHEMA_DIR / path_part)
    target: Any = target_schema
    if pointer:
        for part in pointer.split("/"):
            target = target[part]
    return target


def validate(instance: Any, schema: dict[str, Any], root_schema: dict[str, Any], path: str = "$") -> None:
    if "$ref" in schema:
        validate(instance, resolve_ref(schema["$ref"], root_schema), root_schema, path)
        return

    if "anyOf" in schema:
        failures = []
        for candidate in schema["anyOf"]:
            try:
                validate(instance, candidate, root_schema, path)
                return
            except AssertionError as exc:
                failures.append(str(exc))
        raise AssertionError(f"{path} did not match anyOf: {failures}")

    if "enum" in schema:
        assert instance in schema["enum"], f"{path} not in enum {schema['enum']}"

    type_value = schema.get("type")
    if isinstance(type_value, list):
        if instance is None and "null" in type_value:
            return
        non_null_types = [value for value in type_value if value != "null"]
        assert non_null_types, f"{path} has no non-null schema type"
        validate(instance, {**schema, "type": non_null_types[0]}, root_schema, path)
        return

    if type_value == "null":
        assert instance is None, f"{path} must be null"
        return
    if type_value == "string":
        assert isinstance(instance, str), f"{path} must be string"
        return
    if type_value == "integer":
        assert isinstance(instance, int) and not isinstance(instance, bool), f"{path} must be integer"
        if "minimum" in schema:
            assert instance >= schema["minimum"], f"{path} below minimum"
        return
    if type_value == "number":
        assert isinstance(instance, (int, float)) and not isinstance(instance, bool), f"{path} must be number"
        if "minimum" in schema:
            assert instance >= schema["minimum"], f"{path} below minimum"
        if "maximum" in schema:
            assert instance <= schema["maximum"], f"{path} above maximum"
        return
    if type_value == "boolean":
        assert isinstance(instance, bool), f"{path} must be boolean"
        return
    if type_value == "array":
        assert isinstance(instance, list), f"{path} must be array"
        item_schema = schema.get("items")
        if item_schema is not None:
            for index, item in enumerate(instance):
                validate(item, item_schema, root_schema, f"{path}[{index}]")
        return
    if type_value == "object":
        assert isinstance(instance, dict), f"{path} must be object"
        for required in schema.get("required", []):
            assert required in instance, f"{path}.{required} is required"
        properties = schema.get("properties", {})
        if schema.get("additionalProperties") is False:
            extra = set(instance) - set(properties)
            assert not extra, f"{path} has additional properties: {sorted(extra)}"
        for name, value in instance.items():
            if name in properties:
                validate(value, properties[name], root_schema, f"{path}.{name}")
            elif isinstance(schema.get("additionalProperties"), dict):
                validate(value, schema["additionalProperties"], root_schema, f"{path}.{name}")
        return


def assert_valid_fixture(fixture_name: str, schema_name: str) -> dict[str, Any]:
    fixture = load_json(FIXTURE_DIR / fixture_name)
    schema = load_json(ARTIFACT_SCHEMA_DIR / schema_name)
    validate(fixture, schema, schema)
    return fixture


def test_contract_r3_fixtures_validate_against_schemas():
    assert_valid_fixture("contract_r3_run_fixture.json", "run-artifact.v1.schema.json")
    assert_valid_fixture("contract_r3_movement_fixture.json", "movement-artifact.v1.schema.json")
    assert_valid_fixture(
        "contract_r3_comparison_fixture.json",
        "comparison-artifact.v1.schema.json",
    )


def test_contract_r3_schema_versions_are_accepted():
    run_schema = load_json(ARTIFACT_SCHEMA_DIR / "run-artifact.v1.schema.json")
    comparison_schema = load_json(ARTIFACT_SCHEMA_DIR / "comparison-artifact.v1.schema.json")

    assert "run-artifact.v1.r3" in run_schema["properties"]["schema_version"]["enum"]
    assert "comparison_artifact.v1.r3" in comparison_schema["properties"]["schema_version"]["enum"]

    assert load_json(FIXTURE_DIR / "contract_r3_run_fixture.json")["schema_version"] == "run-artifact.v1.r3"
    assert (
        load_json(FIXTURE_DIR / "contract_r3_comparison_fixture.json")["schema_version"]
        == "comparison_artifact.v1.r3"
    )


def test_contract_r3_run_fixture_covers_graph_and_kpis():
    fixture = assert_valid_fixture("contract_r3_run_fixture.json", "run-artifact.v1.schema.json")

    graph = fixture["warehouse_graph"]
    assert len(graph["nodes"]) >= 4
    assert len(graph["edges"]) >= 3
    assert {"node_id", "node_type", "x", "y"} <= set(graph["nodes"][0])
    assert {
        "edge_id",
        "from_node_id",
        "to_node_id",
        "distance_m",
        "travel_time_ms",
        "bidirectional",
    } <= set(graph["edges"][0])

    kpis = fixture["kpi_summary"]
    for field in [
        "order_cycle_p50_ms",
        "order_cycle_p90_ms",
        "order_cycle_p95_ms",
        "avg_wait_ms",
        "resource_utilization",
        "bottlenecks",
        "travel_distance_m_by_actor_type",
    ]:
        assert field in kpis

    assert any("evidence_level=fixture_only" in entry for entry in fixture["event_log"])
    assert any("not_sensor_calibrated=true" in entry for entry in fixture["event_log"])


def test_contract_r3_movement_fixture_covers_route_path():
    fixture = assert_valid_fixture(
        "contract_r3_movement_fixture.json",
        "movement-artifact.v1.schema.json",
    )

    assert any(actor["actor_type"] in {"forklift", "worker"} for actor in fixture["actors"])
    assert len(fixture["route_segments"]) >= 2
    first, second = fixture["route_segments"][:2]
    assert first["to_node_id"] == second["from_node_id"]
    assert first["end_ms"] == second["start_ms"]
    assert first["start_ms"] < first["end_ms"] <= second["end_ms"]
    assert {event["node_id"] for event in fixture["movement_events"]} >= {
        first["from_node_id"],
        first["to_node_id"],
        second["to_node_id"],
    }
    assert "not calibrated real trajectory" in fixture["provenance"]["graph_source"]
    assert "evidence_level=fixture_only" in fixture["provenance"]["deterministic_generation_policy"]


def test_contract_r3_comparison_fixture_covers_kpi_deltas_and_improvement():
    fixture = assert_valid_fixture(
        "contract_r3_comparison_fixture.json",
        "comparison-artifact.v1.schema.json",
    )

    assert fixture["baseline"]["scenario_id"] == "contract-r3-baseline-run"
    assert fixture["candidate"]["scenario_id"] == "contract-r3-optimized-run"
    assert len(fixture["deltas"]) >= 3
    assert len(fixture["kpi_deltas"]) >= 3
    assert len(fixture["improvement_pct"]) >= 3
    assert all("metric_name" in delta for delta in fixture["deltas"])
    assert fixture["improvement_pct"]["order_cycle_p50_ms"] > 0


def test_contract_r3_fixtures_are_accessible_from_generated_contracts():
    generated = load_generated_module()

    run = load_json(FIXTURE_DIR / "contract_r3_run_fixture.json")
    movement = load_json(FIXTURE_DIR / "contract_r3_movement_fixture.json")
    comparison = load_json(FIXTURE_DIR / "contract_r3_comparison_fixture.json")

    run_contract = generated.RunArtifact(**run)
    movement_contract = generated.MovementArtifact(**movement)
    comparison_contract = generated.ComparisonArtifact(**comparison)

    assert run_contract.schema_version == "run-artifact.v1.r3"
    assert run_contract.warehouse_graph["nodes"][0]["node_id"] == "node-dock-in"
    assert movement_contract.route_segments[1]["from_node_id"] == "node-aisle-west"
    assert comparison_contract.improvement_pct["avg_wait_ms"] == 25.0

    csharp_text = GENERATED_CSHARP.read_text(encoding="utf-8")
    assert "public sealed record RunArtifact" in csharp_text
    assert "object warehouse_graph" in csharp_text
    assert "object kpi_deltas" in csharp_text
    assert "object improvement_pct" in csharp_text
