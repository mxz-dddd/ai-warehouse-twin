"""Command line helpers for ingestion scaffolding."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import TextIO

from ingestion.adapters import write_csv_scenario_outputs, write_mock_wms_scenario_outputs
from ingestion.schema import get_schema_info


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="python -m ingestion",
        description="Warehouse scenario ingestion scaffolding.",
    )
    subcommands = parser.add_subparsers(dest="command")

    schema_info = subcommands.add_parser(
        "print-schema-info",
        help="Read datasets/templates/scenario.schema.json and print basic metadata.",
    )
    schema_info.add_argument(
        "--repo-root",
        type=Path,
        default=None,
        help="Repository root containing datasets/templates/scenario.schema.json.",
    )
    schema_info.set_defaults(handler=_print_schema_info)

    csv_to_scenario = subcommands.add_parser(
        "csv-to-scenario",
        help=(
            "Convert a directory of standard CSV export tables into scenario.json. "
            "Excel workbook support is reserved for a later ingestion card."
        ),
    )
    csv_to_scenario.add_argument(
        "--input",
        required=True,
        type=Path,
        help=(
            "Input directory containing config.csv, skus.csv, locations.csv, "
            "inventory.csv, and orders.csv."
        ),
    )
    csv_to_scenario.add_argument(
        "--output",
        required=True,
        type=Path,
        help="Output directory for scenario.json and data-quality-report.md.",
    )
    csv_to_scenario.add_argument(
        "--repo-root",
        type=Path,
        default=None,
        help="Repository root containing datasets/templates/scenario.schema.json.",
    )
    csv_to_scenario.set_defaults(handler=_csv_to_scenario)

    mock_wms_to_scenario = subcommands.add_parser(
        "mock-wms-to-scenario",
        help="Convert a directory of local Mock WMS JSON payloads into scenario.json.",
    )
    mock_wms_to_scenario.add_argument(
        "--input",
        required=True,
        type=Path,
        help=(
            "Input directory containing config.json, skus.json, locations.json, "
            "inventory.json, and orders.json."
        ),
    )
    mock_wms_to_scenario.add_argument(
        "--output",
        required=True,
        type=Path,
        help="Output directory for scenario.json and data-quality-report.md.",
    )
    mock_wms_to_scenario.add_argument(
        "--repo-root",
        type=Path,
        default=None,
        help="Repository root containing datasets/templates/scenario.schema.json.",
    )
    mock_wms_to_scenario.set_defaults(handler=_mock_wms_to_scenario)

    return parser


def main(argv: list[str] | None = None, stdout: TextIO = sys.stdout) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    handler = getattr(args, "handler", None)
    if handler is None:
        parser.print_help(stdout)
        return 0

    try:
        return int(handler(args, stdout))
    except FileNotFoundError as error:
        print(f"error: {error}", file=sys.stderr)
        return 2


def _print_schema_info(args: argparse.Namespace, stdout: TextIO) -> int:
    info = get_schema_info(args.repo_root)
    required = ", ".join(info.required_fields)

    print(f"title: {info.title}", file=stdout)
    print(f"schema_id: {info.schema_id}", file=stdout)
    print(f"schema_version: {info.schema_version}", file=stdout)
    print(f"required: {required}", file=stdout)
    return 0


def _csv_to_scenario(args: argparse.Namespace, stdout: TextIO) -> int:
    exit_code = write_csv_scenario_outputs(args.input, args.output, args.repo_root)
    report_path = args.output / "data-quality-report.md"
    if exit_code:
        print(f"CSV ingestion failed; see {report_path}", file=sys.stderr)
        return exit_code

    print(f"wrote {args.output / 'scenario.json'}", file=stdout)
    print(f"wrote {report_path}", file=stdout)
    return 0


def _mock_wms_to_scenario(args: argparse.Namespace, stdout: TextIO) -> int:
    exit_code = write_mock_wms_scenario_outputs(args.input, args.output, args.repo_root)
    report_path = args.output / "data-quality-report.md"
    if exit_code:
        print(f"Mock WMS ingestion failed; see {report_path}", file=sys.stderr)
        return exit_code

    print(f"wrote {args.output / 'scenario.json'}", file=stdout)
    print(f"wrote {report_path}", file=stdout)
    return 0
