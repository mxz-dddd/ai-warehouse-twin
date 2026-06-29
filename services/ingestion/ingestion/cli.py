"""Command line helpers for ingestion scaffolding."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import TextIO

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
