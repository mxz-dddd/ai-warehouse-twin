# Runtime Operator Smoke

This note describes the current runtime smoke check. It is intentionally local
and deterministic. It does not start services, call Sim.Cli, run `dotnet`, or
use external infrastructure.

## Command

Run from the repository root:

```bash
bash scripts/smoke-runtime.sh
```

## Coverage

The smoke script checks:

- `ruff check services/runtime`
- `mypy services/runtime`
- `pytest services/runtime`
- `python -m runtime_service --help`
- `python -m runtime_service run-dry --scenario <scenario.json> --output <out-dir>`
- `python -m runtime_service plan-simcli --scenario <scenario.json> --output <out-dir>`
- `python -m runtime_service inspect-run --run-dir <out-dir>`
- `runtime-result.json` is written by `run-dry`
- `artifact-index.json` is written by runtime output commands
- `simcli-plan.json` is written by `plan-simcli`
- `run-manifest.json` is written by both `run-dry` and `plan-simcli`
- repeated `run-dry` outputs are byte-identical
- repeated `plan-simcli` outputs are byte-identical
- generated output does not contain local absolute paths, timestamps, or random
  noise

The script creates a temporary directory with `mktemp -d` and removes it with a
`trap` on exit.

## Current Boundaries

The smoke check validates runtime scaffolding only:

- `run-dry` does not call real Sim.Core.
- `plan-simcli` writes a command plan but does not execute `dotnet`.
- `inspect-run` reads only `run-manifest.json`.
- MovementArtifact is not parsed or generated.
- No HTTP server, watcher, database, queue, or long-running background process
  is started.

If a future runtime task needs to call real Sim.Cli, add that behavior in a
separate task with explicit review of core, contract, and artifact boundaries.
