"""Mock WMS adapter backed by local JSON payloads."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from ingestion.adapters.common import (
    CONFIG_KEYS,
    NON_NEGATIVE_CONFIG_KEYS,
    POSITIVE_CONFIG_KEYS,
    VALID_LOCATION_TYPES,
    VALID_STATUSES,
    DataQualityIssue,
    DataQualityReport,
    IngestionInputError,
    add_issue,
    build_outbound_scenario,
    build_quality_report,
    parse_int,
    require_known,
    require_known_location_type,
    require_location_type_coverage,
    require_text_value,
    write_scenario_outputs,
)
from ingestion.sources import ScenarioDocument

MOCK_WMS_FILES = (
    "config.json",
    "inventory.json",
    "locations.json",
    "orders.json",
    "skus.json",
)


class MockWmsIngestSource:
    """Read local WMS-like JSON payloads and produce an outbound scenario document."""

    def __init__(self, input_dir: Path) -> None:
        self.input_dir = input_dir

    @property
    def source_name(self) -> str:
        return f"mock-wms:{self.input_dir.name}"

    def describe(self) -> str:
        return "Mock WMS JSON directory adapter for deterministic ingestion tests."

    def to_scenario(self) -> ScenarioDocument:
        scenario, report = self._convert()
        if report.error_count:
            raise IngestionInputError(report, "Mock WMS")
        return scenario

    def build_report(self) -> DataQualityReport:
        _scenario, report = self._convert()
        return report

    def _convert(self) -> tuple[ScenarioDocument, DataQualityReport]:
        issues: list[DataQualityIssue] = []
        payloads = {
            file_name: _read_json_payload(self.input_dir / file_name, issues)
            for file_name in MOCK_WMS_FILES
        }
        config_records = _object_payload(payloads["config.json"], "config.json", issues)
        inventory_records = _list_payload(payloads["inventory.json"], "inventory.json", issues)
        location_records = _list_payload(payloads["locations.json"], "locations.json", issues)
        order_records = _list_payload(payloads["orders.json"], "orders.json", issues)
        sku_records = _list_payload(payloads["skus.json"], "skus.json", issues)
        record_counts = {
            "config": len(config_records),
            "inventory": len(inventory_records),
            "locations": len(location_records),
            "orders": len(order_records),
            "skus": len(sku_records),
        }

        config = _read_config(config_records, issues)
        skus = _id_set(sku_records, "sku_id", "skus.json", issues)
        location_types = _location_types(location_records, issues)
        inventory = _inventory_rows(inventory_records, skus, location_types, issues)
        orders = _order_rows(order_records, skus, location_types, issues)

        scenario = build_outbound_scenario(config, inventory, orders)
        report = build_quality_report(
            source_name=self.source_name,
            input_files=MOCK_WMS_FILES,
            record_counts=record_counts,
            issues=issues,
            skus=skus,
            location_types=location_types,
            inventory=inventory,
            orders=orders,
        )
        return scenario, report


def write_mock_wms_scenario_outputs(
    input_dir: Path,
    output_dir: Path,
    repo_root: Path | None = None,
) -> int:
    source = MockWmsIngestSource(input_dir)
    return write_scenario_outputs(output_dir, source._convert, repo_root, "Mock WMS")


def _read_json_payload(path: Path, issues: list[DataQualityIssue]) -> Any:
    if not path.is_file():
        add_issue(
            issues,
            code="missing_required_file",
            source_file=path.name,
            message="required input file is missing",
        )
        return None

    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        add_issue(
            issues,
            code="invalid_json",
            source_file=path.name,
            row_number=error.lineno,
            message=error.msg,
        )
        return None


def _object_payload(
    payload: Any,
    file_name: str,
    issues: list[DataQualityIssue],
) -> dict[str, Any]:
    if isinstance(payload, dict):
        return payload
    if payload is not None:
        add_issue(
            issues,
            code="invalid_payload_type",
            source_file=file_name,
            message="payload must be a JSON object",
        )
    return {}


def _list_payload(
    payload: Any,
    file_name: str,
    issues: list[DataQualityIssue],
) -> list[dict[str, Any]]:
    if not isinstance(payload, list):
        if payload is not None:
            add_issue(
                issues,
                code="invalid_payload_type",
                source_file=file_name,
                message="payload must be a JSON array",
            )
        return []

    rows: list[dict[str, Any]] = []
    for index, item in enumerate(payload, start=1):
        if isinstance(item, dict):
            rows.append(item)
        else:
            add_issue(
                issues,
                code="invalid_record_type",
                source_file=file_name,
                row_number=index,
                message="record must be a JSON object",
            )
    return rows


def _read_config(payload: dict[str, Any], issues: list[DataQualityIssue]) -> dict[str, str]:
    config = {key: _string_value(payload.get(key)) for key in CONFIG_KEYS}
    for key in CONFIG_KEYS:
        if not config[key]:
            add_issue(
                issues,
                code="missing_required_field",
                source_file="config.json",
                field=key,
                message=f"missing required key '{key}'",
            )

    for key in POSITIVE_CONFIG_KEYS:
        parse_int(config.get(key, ""), "config.json", None, key, minimum=1, issues=issues)

    for key in NON_NEGATIVE_CONFIG_KEYS:
        parse_int(config.get(key, ""), "config.json", None, key, minimum=0, issues=issues)

    return config


def _id_set(
    rows: list[dict[str, Any]],
    id_column: str,
    file_name: str,
    issues: list[DataQualityIssue],
) -> set[str]:
    ids: set[str] = set()
    for index, row in enumerate(rows, start=1):
        value = _string_value(row.get(id_column))
        if not require_text_value(
            value,
            file_name=file_name,
            row_number=index,
            field_name=id_column,
            issues=issues,
        ):
            continue
        if value in ids:
            add_issue(
                issues,
                code=f"duplicate_{id_column}",
                source_file=file_name,
                row_number=index,
                field=id_column,
                message=f"duplicate {id_column} '{value}'",
            )
        ids.add(value)
    return ids


def _location_types(
    rows: list[dict[str, Any]],
    issues: list[DataQualityIssue],
) -> dict[str, str]:
    locations: dict[str, str] = {}
    for index, row in enumerate(rows, start=1):
        location_id = _string_value(row.get("location_id"))
        location_type = _string_value(row.get("location_type"))
        if not require_text_value(
            location_id,
            file_name="locations.json",
            row_number=index,
            field_name="location_id",
            issues=issues,
        ):
            continue
        if require_text_value(
            location_type,
            file_name="locations.json",
            row_number=index,
            field_name="location_type",
            issues=issues,
        ) and location_type not in VALID_LOCATION_TYPES:
            add_issue(
                issues,
                code="invalid_location_type",
                source_file="locations.json",
                row_number=index,
                field="location_type",
                message=(
                    "location_type must be one of dock, pick, staging; "
                    f"got '{location_type}'"
                ),
            )
        if location_id in locations:
            add_issue(
                issues,
                code="duplicate_location_id",
                source_file="locations.json",
                row_number=index,
                field="location_id",
                message=f"duplicate location_id '{location_id}'",
            )
        locations[location_id] = location_type
    require_location_type_coverage(locations, "locations.json", issues)
    return locations


def _inventory_rows(
    rows: list[dict[str, Any]],
    skus: set[str],
    location_types: dict[str, str],
    issues: list[DataQualityIssue],
) -> list[dict[str, Any]]:
    inventory: list[dict[str, Any]] = []
    inventory_ids: set[str] = set()
    sku_location_pairs: set[tuple[str, str]] = set()
    for index, row in enumerate(rows, start=1):
        inventory_id = _string_value(row.get("inventory_id"))
        require_text_value(
            inventory_id,
            file_name="inventory.json",
            row_number=index,
            field_name="inventory_id",
            issues=issues,
        )
        if inventory_id in inventory_ids:
            add_issue(
                issues,
                code="duplicate_inventory_id",
                source_file="inventory.json",
                row_number=index,
                field="inventory_id",
                message=f"duplicate inventory_id '{inventory_id}'",
            )
        inventory_ids.add(inventory_id)
        sku_id = _string_value(row.get("sku_id"))
        location_id = _string_value(row.get("location_id"))
        status = _string_value(row.get("status"))
        require_text_value(
            sku_id,
            file_name="inventory.json",
            row_number=index,
            field_name="sku_id",
            issues=issues,
        )
        require_text_value(
            location_id,
            file_name="inventory.json",
            row_number=index,
            field_name="location_id",
            issues=issues,
        )
        require_text_value(
            status,
            file_name="inventory.json",
            row_number=index,
            field_name="status",
            issues=issues,
        )
        require_text_value(
            _string_value(row.get("quantity")),
            file_name="inventory.json",
            row_number=index,
            field_name="quantity",
            issues=issues,
        )
        require_known(sku_id, skus, "inventory.json", index, "sku_id", "sku", issues)
        require_known(
            location_id,
            set(location_types),
            "inventory.json",
            index,
            "location_id",
            "location",
            issues,
        )
        if status not in VALID_STATUSES:
            add_issue(
                issues,
                code="invalid_enum_value",
                source_file="inventory.json",
                row_number=index,
                field="status",
                message=f"status must be one of schema enum values; got '{status}'",
            )
        sku_location_pair = (sku_id, location_id)
        if sku_location_pair in sku_location_pairs:
            add_issue(
                issues,
                code="duplicate_inventory_sku_location",
                source_file="inventory.json",
                row_number=index,
                field="sku_id,location_id",
                message=f"duplicate inventory sku/location pair '{sku_id}'/'{location_id}'",
            )
        sku_location_pairs.add(sku_location_pair)
        quantity = parse_int(
            _string_value(row.get("quantity")),
            "inventory.json",
            index,
            "quantity",
            minimum=1,
            issues=issues,
        )
        inventory.append(
            {
                "inventory_id": inventory_id,
                "sku_id": sku_id,
                "quantity": quantity,
                "location_id": location_id,
                "status": status,
            }
        )

    return sorted(inventory, key=lambda item: str(item["inventory_id"]))


def _order_rows(
    rows: list[dict[str, Any]],
    skus: set[str],
    location_types: dict[str, str],
    issues: list[DataQualityIssue],
) -> list[dict[str, Any]]:
    orders: list[dict[str, Any]] = []
    order_ids: set[str] = set()
    for index, row in enumerate(rows, start=1):
        order_id = _string_value(row.get("order_id"))
        for field_name in (
            "order_id",
            "warehouse_id",
            "sku_id",
            "pick_location_id",
            "staging_location_id",
            "dock_id",
            "quantity",
            "released_at_ms",
        ):
            require_text_value(
                _string_value(row.get(field_name)),
                file_name="orders.json",
                row_number=index,
                field_name=field_name,
                issues=issues,
            )
        if order_id in order_ids:
            add_issue(
                issues,
                code="duplicate_order_id",
                source_file="orders.json",
                row_number=index,
                field="order_id",
                message=f"duplicate order_id '{order_id}'",
            )
        order_ids.add(order_id)
        sku_id = _string_value(row.get("sku_id"))
        pick_location_id = _string_value(row.get("pick_location_id"))
        staging_location_id = _string_value(row.get("staging_location_id"))
        dock_id = _string_value(row.get("dock_id"))
        require_known(sku_id, skus, "orders.json", index, "sku_id", "sku", issues)
        require_known_location_type(
            pick_location_id,
            "pick",
            location_types,
            "orders.json",
            index,
            issues,
        )
        require_known_location_type(
            staging_location_id,
            "staging",
            location_types,
            "orders.json",
            index,
            issues,
        )
        require_known_location_type(dock_id, "dock", location_types, "orders.json", index, issues)
        quantity = parse_int(
            _string_value(row.get("quantity")),
            "orders.json",
            index,
            "quantity",
            minimum=1,
            issues=issues,
        )
        released_at_ms = parse_int(
            _string_value(row.get("released_at_ms")),
            "orders.json",
            index,
            "released_at_ms",
            minimum=0,
            issues=issues,
        )
        orders.append(
            {
                "order_id": order_id,
                "warehouse_id": _string_value(row.get("warehouse_id")),
                "sku_id": sku_id,
                "quantity": quantity,
                "pick_location_id": pick_location_id,
                "staging_location_id": staging_location_id,
                "dock_id": dock_id,
                "released_at_ms": released_at_ms,
            }
        )

    return sorted(orders, key=lambda item: str(item["order_id"]))


def _string_value(value: Any) -> str:
    return "" if value is None else str(value).strip()
