"""Ingestion source contracts."""

from __future__ import annotations

from typing import Any, Protocol

ScenarioDocument = dict[str, Any]


class IngestSource(Protocol):
    """Protocol implemented by WMS, database, and file-export ingestion sources."""

    @property
    def source_name(self) -> str:
        """Stable source name for reporting and diagnostics."""

    def describe(self) -> str:
        """Return a short human-readable source description."""

    def to_scenario(self) -> ScenarioDocument:
        """Convert source data into a warehouse scenario document skeleton."""
