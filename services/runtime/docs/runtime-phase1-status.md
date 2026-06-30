# Runtime Phase 1 Status

Runtime phase 1 provides a deterministic local orchestration scaffold. It is a
recording and planning layer, not the final production runner.

## Supported Now

The runtime service currently supports:

- dry local run records with `run-dry`
- Sim.Cli command planning with `plan-simcli`
- manifest inspection with `inspect-run`
- deterministic `runtime-result.json`
- deterministic `artifact-index.json`
- deterministic `simcli-plan.json`
- deterministic `run-manifest.json`
- local run store helpers for creating run directories, reading and writing
  manifests, and stable run listing

Run manifests include:

- manifest schema version
- scenario file name
- scenario content hash
- runner name
- runner mode
- run status
- artifact index relative path
- stable output file summary

The generated outputs are designed to avoid:

- local absolute paths
- current timestamps
- random identifiers
- machine-specific filesystem noise

## Not Supported Yet

The runtime service does not currently:

- call real Sim.Cli
- run `dotnet`
- invoke Sim.Core
- parse or generate MovementArtifact data
- connect to WMS or ingestion sources
- provide a database, queue, API server, or job service
- generate customer reports
- perform a real simulation run

## Parallel Boundaries

Runtime phase 1 is scoped to:

```text
services/runtime/**
scripts/smoke-runtime.sh
.github/workflows/runtime-ci.yml
```

Runtime work should not modify these areas without a separate approved task:

```text
src/**
engine/**
packages/contracts/**
services/ingestion/**
```

## Suggested Next Runtime Slices

- RUNTIME-004: real Sim.Cli adapter, after the core-side MovementArtifact and
  CLI output contract are stable enough for orchestration.
- RUNTIME-005: runtime artifact handoff or report handoff, once the produced
  artifact set and ownership model are explicit.
- RUNTIME-006: API or job service boundary, after the local run model has enough
  operational shape to expose safely.

These future slices should remain separate. The current runtime service should
not be described as a real Sim.Cli integration, a real WMS integration, or a
complete runtime job platform.
