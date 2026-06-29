import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SCHEMA_PATH = (
    ROOT
    / "packages"
    / "contracts"
    / "artifacts"
    / "movement-artifact.v1.schema.json"
)
FIXTURE_ROOT = ROOT / "tests" / "contract" / "fixtures" / "movement_artifact"

TOP_LEVEL_REQUIRED = {
    "schema_version",
    "artifact_kind",
    "scenario_id",
    "seed",
    "source_run_artifact",
    "warehouse_graph",
    "actors",
    "movement_events",
    "route_segments",
    "provenance",
}


def load_json(path: Path):
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def fixture(name: str):
    return load_json(FIXTURE_ROOT / name)


def duplicate_values(values):
    seen = set()
    duplicates = set()
    for value in values:
        if value in seen:
            duplicates.add(value)
        seen.add(value)
    return duplicates


def check_movement_artifact_contract(artifact):
    errors = []

    missing = TOP_LEVEL_REQUIRED - artifact.keys()
    for field in sorted(missing):
        errors.append(f"top_level.missing:{field}")

    if artifact.get("schema_version") != "movement-artifact.v1":
        errors.append("schema_version.invalid")

    if artifact.get("artifact_kind") != "warehouse-movement":
        errors.append("artifact_kind.invalid")

    graph = artifact.get("warehouse_graph") or {}
    nodes = graph.get("nodes") or []
    edges = graph.get("edges") or []
    actors = artifact.get("actors") or []
    events = artifact.get("movement_events") or []
    segments = artifact.get("route_segments") or []

    node_ids = [node.get("node_id") for node in nodes]
    node_id_set = set(node_ids)
    for node_id in duplicate_values(node_ids):
        errors.append(f"node_id.duplicate:{node_id}")

    edge_ids = [edge.get("edge_id") for edge in edges]
    edge_id_set = set(edge_ids)
    for edge_id in duplicate_values(edge_ids):
        errors.append(f"edge_id.duplicate:{edge_id}")

    for edge in edges:
        edge_id = edge.get("edge_id")
        for key in ("from_node_id", "to_node_id"):
            if edge.get(key) not in node_id_set:
                errors.append(f"edge.{key}.missing:{edge_id}")
        if edge.get("distance_m", 0) < 0:
            errors.append(f"edge.distance_m.negative:{edge_id}")
        if edge.get("travel_time_ms", 0) < 0:
            errors.append(f"edge.travel_time_ms.negative:{edge_id}")

    actor_ids = [actor.get("actor_id") for actor in actors]
    actor_id_set = set(actor_ids)
    for actor_id in duplicate_values(actor_ids):
        errors.append(f"actor_id.duplicate:{actor_id}")

    for actor in actors:
        actor_id = actor.get("actor_id")
        if actor.get("initial_node_id") not in node_id_set:
            errors.append(f"actor.initial_node_id.missing:{actor_id}")

    event_ids = [event.get("event_id") for event in events]
    for event_id in duplicate_values(event_ids):
        errors.append(f"event_id.duplicate:{event_id}")

    for event in events:
        event_id = event.get("event_id")
        if event.get("actor_id") not in actor_id_set:
            errors.append(f"event.actor_id.missing:{event_id}")
        if event.get("node_id") not in node_id_set:
            errors.append(f"event.node_id.missing:{event_id}")
        if event.get("at_ms", 0) < 0:
            errors.append(f"event.at_ms.negative:{event_id}")

    segment_ids = [segment.get("segment_id") for segment in segments]
    for segment_id in duplicate_values(segment_ids):
        errors.append(f"segment_id.duplicate:{segment_id}")

    for segment in segments:
        segment_id = segment.get("segment_id")
        if segment.get("actor_id") not in actor_id_set:
            errors.append(f"segment.actor_id.missing:{segment_id}")
        for key in ("from_node_id", "to_node_id"):
            if segment.get(key) not in node_id_set:
                errors.append(f"segment.{key}.missing:{segment_id}")
        for node_id in segment.get("path_node_ids") or []:
            if node_id not in node_id_set:
                errors.append(f"segment.path_node_id.missing:{segment_id}:{node_id}")
        for edge_id in segment.get("edge_ids") or []:
            if edge_id not in edge_id_set:
                errors.append(f"segment.edge_id.missing:{segment_id}:{edge_id}")
        start_ms = segment.get("start_ms", 0)
        end_ms = segment.get("end_ms", 0)
        if start_ms < 0:
            errors.append(f"segment.start_ms.negative:{segment_id}")
        if end_ms < 0:
            errors.append(f"segment.end_ms.negative:{segment_id}")
        if end_ms < start_ms:
            errors.append(f"segment.time.non_monotonic:{segment_id}")
        if segment.get("distance_m", 0) < 0:
            errors.append(f"segment.distance_m.negative:{segment_id}")
        if segment.get("travel_time_ms", 0) < 0:
            errors.append(f"segment.travel_time_ms.negative:{segment_id}")

    return errors


def assert_fixture_fails_with(name: str, expected_error_prefix: str):
    errors = check_movement_artifact_contract(fixture(name))

    assert any(
        error.startswith(expected_error_prefix)
        for error in errors
    ), f"expected {expected_error_prefix!r} in {errors!r}"


def test_movement_artifact_schema_contract_is_present():
    schema = load_json(SCHEMA_PATH)

    assert schema["title"] == "MovementArtifact"
    assert schema["properties"]["schema_version"]["enum"] == [
        "movement-artifact.v1"
    ]
    assert schema["properties"]["artifact_kind"]["enum"] == [
        "warehouse-movement"
    ]
    assert TOP_LEVEL_REQUIRED.issubset(set(schema["required"]))


def test_valid_small_single_actor_route_fixture_passes_contract_checks():
    errors = check_movement_artifact_contract(
        fixture("valid-small-single-actor-route.json")
    )

    assert errors == []


def test_invalid_missing_node_fixture_fails():
    assert_fixture_fails_with(
        "invalid-missing-node.json",
        "actor.initial_node_id.missing",
    )


def test_invalid_edge_references_missing_node_fixture_fails():
    assert_fixture_fails_with(
        "invalid-edge-references-missing-node.json",
        "edge.to_node_id.missing",
    )


def test_invalid_event_references_missing_actor_fixture_fails():
    assert_fixture_fails_with(
        "invalid-event-references-missing-actor.json",
        "event.actor_id.missing",
    )


def test_invalid_segment_non_monotonic_time_fixture_fails():
    assert_fixture_fails_with(
        "invalid-segment-non-monotonic-time.json",
        "segment.time.non_monotonic",
    )


def test_invalid_duplicate_event_id_fixture_fails():
    assert_fixture_fails_with(
        "invalid-duplicate-event-id.json",
        "event_id.duplicate",
    )


def test_invalid_negative_distance_fixture_fails():
    assert_fixture_fails_with(
        "invalid-negative-distance.json",
        "edge.distance_m.negative",
    )
