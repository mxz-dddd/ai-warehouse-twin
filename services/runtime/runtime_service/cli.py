"""Command line interface for runtime orchestration scaffolding."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import TextIO

from runtime_service.runner import ARTIFACT_INDEX_FILE, RUNTIME_RESULT_FILE, run_dry


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="python -m runtime_service",
        description="Runtime orchestration scaffold.",
    )
    subcommands = parser.add_subparsers(dest="command")

    run_dry_parser = subcommands.add_parser(
        "run-dry",
        help="Create deterministic dry-run runtime artifacts without invoking simulation core.",
    )
    run_dry_parser.add_argument(
        "--scenario",
        required=True,
        type=Path,
        help="Scenario JSON file to attach to the runtime dry run.",
    )
    run_dry_parser.add_argument(
        "--output",
        required=True,
        type=Path,
        help="Output directory for runtime-result.json and artifact-index.json.",
    )
    run_dry_parser.set_defaults(handler=_run_dry)

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


def _run_dry(args: argparse.Namespace, stdout: TextIO) -> int:
    result = run_dry(args.scenario, args.output)
    print(f"job_id: {result.job_id}", file=stdout)
    print(f"status: {result.status.value}", file=stdout)
    print(f"wrote: {RUNTIME_RESULT_FILE}", file=stdout)
    print(f"wrote: {ARTIFACT_INDEX_FILE}", file=stdout)
    return 0
