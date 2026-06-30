"""Scenario schema loading utilities for ingestion."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any

SCENARIO_SCHEMA_RELATIVE_PATH = Path("datasets/templates/scenario.schema.json")


@dataclass(frozen=True)
class ScenarioSchemaInfo:
    title: str
    schema_id: str
    schema_version: str
    required_fields: tuple[str, ...]


def find_repo_root(start: Path | None = None) -> Path:
    search_start = (start or Path.cwd()).resolve()
    if search_start.is_file():
        search_start = search_start.parent

    for candidate in (search_start, *search_start.parents):
        if (candidate / SCENARIO_SCHEMA_RELATIVE_PATH).is_file():
            return candidate

    raise FileNotFoundError(
        "Could not find datasets/templates/scenario.schema.json. "
        "Start from a repository path or pass --repo-root explicitly; "
        f"searched from {search_start}."
    )


def scenario_schema_path(repo_root: Path | None = None) -> Path:
    root = repo_root.resolve() if repo_root is not None else find_repo_root()
    path = root / SCENARIO_SCHEMA_RELATIVE_PATH
    if not path.is_file():
        raise FileNotFoundError(f"Scenario schema not found at {path}.")
    return path


def load_scenario_schema(repo_root: Path | None = None) -> dict[str, Any]:
    path = scenario_schema_path(repo_root)
    with path.open("r", encoding="utf-8") as schema_file:
        loaded = json.load(schema_file)

    if not isinstance(loaded, dict):
        raise ValueError(f"Scenario schema at {path} must be a JSON object.")

    return loaded


def get_schema_info(repo_root: Path | None = None) -> ScenarioSchemaInfo:
    schema = load_scenario_schema(repo_root)
    properties = _object_value(schema.get("properties"))
    schema_version = _object_value(properties.get("schema_version")).get("const", "")

    return ScenarioSchemaInfo(
        title=_string_value(schema.get("title")),
        schema_id=_string_value(schema.get("$id")),
        schema_version=_string_value(schema_version),
        required_fields=_string_tuple(schema.get("required")),
    )


def _object_value(value: Any) -> dict[str, Any]:
    return value if isinstance(value, dict) else {}


def _string_value(value: Any) -> str:
    return value if isinstance(value, str) else ""


def _string_tuple(value: Any) -> tuple[str, ...]:
    if not isinstance(value, list):
        return ()
    return tuple(item for item in value if isinstance(item, str))
