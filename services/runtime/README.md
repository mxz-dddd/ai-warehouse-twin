# Runtime Service

The runtime service is the current orchestration scaffold for local run records.
It records deterministic dry-run outputs, Sim.Cli command plans, artifact
indexes, and run manifests. It does not run the simulation core yet.

## Current Capabilities

- `run-dry` creates a deterministic local dry-run result.
- `plan-simcli` writes a deterministic Sim.Cli command plan.
- `inspect-run` reads an existing `run-manifest.json`.
- Runtime outputs include:
  - `runtime-result.json`
  - `artifact-index.json`
  - `simcli-plan.json`
  - `run-manifest.json`
- Run manifests record scenario file identity, scenario content hash, runner
  name, runner mode, status, artifact index path, and output summary.
- The local run store can create deterministic run directories, write and read
  manifests, and list runs in stable order.
- Generated JSON is deterministic and avoids local absolute paths, timestamps,
  random values, and machine-specific noise.

## CLI

Run commands from `services/runtime` after installing the package or with the
service directory on `PYTHONPATH`.

```bash
python -m runtime_service --help
python -m runtime_service run-dry --scenario <scenario.json> --output <out-dir>
python -m runtime_service plan-simcli --scenario <scenario.json> --output <out-dir>
python -m runtime_service inspect-run --run-dir <out-dir>
```

Current command boundaries:

- `run-dry` writes local runtime scaffolding artifacts and does not call the
  real simulation core.
- `plan-simcli` writes `simcli-plan.json` and does not execute `dotnet`.
- `inspect-run` only reads `<out-dir>/run-manifest.json`.
- The runtime service does not parse or generate MovementArtifact output.
- The runtime service does not perform a real simulation run.

## Smoke

Use the runtime smoke script from the repository root:

```bash
bash scripts/smoke-runtime.sh
```

The smoke script runs `ruff`, `mypy`, `pytest`, CLI help, `run-dry`,
`plan-simcli`, and `inspect-run`. It verifies byte-stable repeated outputs and
checks generated files for local path, timestamp, and random noise. The script
uses a temporary directory and removes it on exit.

## Boundaries

Runtime work currently belongs to:

```text
services/runtime/**
scripts/smoke-runtime.sh
.github/workflows/runtime-ci.yml
```

Runtime changes should not touch:

```text
src/**
packages/contracts/**
services/ingestion/**
engine/**
```

This documentation closeout only documents the runtime service and does not
change smoke or CI behavior.

## Next Steps

- RUNTIME-004: add a real Sim.Cli adapter after the core-side MovementArtifact
  and CLI output surfaces are stable.
- RUNTIME-005: add runtime artifact handoff or report handoff once artifact
  ownership is explicit.
- RUNTIME-006: evaluate an API or job service boundary.

These are future slices. The current runtime service is not a real Sim.Cli
integration, not a real WMS integration, and not a complete simulation
orchestrator.
