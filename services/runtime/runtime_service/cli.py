"""Command line interface for runtime orchestration scaffolding."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import TextIO

from runtime_service.manifest import RUN_MANIFEST_FILE
from runtime_service.runner import ARTIFACT_INDEX_FILE, RUNTIME_RESULT_FILE, run_dry
from runtime_service.simcli import SIMCLI_PLAN_FILE, write_simcli_plan
from runtime_service.store import read_manifest_from_dir


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

    plan_simcli_parser = subcommands.add_parser(
        "plan-simcli",
        help="Write a deterministic Sim.Cli command plan without executing dotnet.",
    )
    plan_simcli_parser.add_argument(
        "--scenario",
        required=True,
        type=Path,
        help="Scenario JSON file to include in the Sim.Cli command plan.",
    )
    plan_simcli_parser.add_argument(
        "--output",
        required=True,
        type=Path,
        help="Output directory for simcli-plan.json and artifact-index.json.",
    )
    plan_simcli_parser.set_defaults(handler=_plan_simcli)

    inspect_run_parser = subcommands.add_parser(
        "inspect-run",
        help="Print a deterministic run manifest JSON document.",
    )
    inspect_run_parser.add_argument(
        "--run-dir",
        required=True,
        type=Path,
        help="Run output directory containing run-manifest.json.",
    )
    inspect_run_parser.set_defaults(handler=_inspect_run)

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
    except (FileNotFoundError, ValueError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 2


def _run_dry(args: argparse.Namespace, stdout: TextIO) -> int:
    result = run_dry(args.scenario, args.output)
    print(f"job_id: {result.job_id}", file=stdout)
    print(f"status: {result.status.value}", file=stdout)
    print(f"wrote: {RUNTIME_RESULT_FILE}", file=stdout)
    print(f"wrote: {ARTIFACT_INDEX_FILE}", file=stdout)
    print(f"wrote: {RUN_MANIFEST_FILE}", file=stdout)
    return 0


def _plan_simcli(args: argparse.Namespace, stdout: TextIO) -> int:
    plan = write_simcli_plan(scenario_path=args.scenario, output_dir=args.output)
    print(f"mode: {plan.mode}", file=stdout)
    print(f"command: {plan.command.executable}", file=stdout)
    print(f"wrote: {SIMCLI_PLAN_FILE}", file=stdout)
    print(f"wrote: {ARTIFACT_INDEX_FILE}", file=stdout)
    print(f"wrote: {RUN_MANIFEST_FILE}", file=stdout)
    return 0


def _inspect_run(args: argparse.Namespace, stdout: TextIO) -> int:
    manifest = read_manifest_from_dir(args.run_dir)
    json.dump(manifest.to_json_dict(), stdout, indent=2, sort_keys=True)
    print(file=stdout)
    return 0
