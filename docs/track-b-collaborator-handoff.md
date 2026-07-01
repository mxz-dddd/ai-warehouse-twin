# Track B Collaborator Handoff

This document is the Track B collaborator handoff for environment readiness and
scope control. It does not start Track B implementation work.

## Project

- Repository: https://github.com/mxz-dddd/ai-warehouse-twin
- Product goal: artifact-backed warehouse digital twin product.
- Track B focus: consume artifacts and contracts into visualization and
  customer-facing delivery surfaces.

Track B should not depend on local owner paths or private machine layouts.
Use repository-relative paths in docs, commands, reviews, and handoff notes.
Do not delete, commit, or clean up local notes or generated Unity import output
unless the owner explicitly asks.

## Current State

Recent Track B work:

- APP-010 customer report baseline: merged.
- APP-020 validation baseline: merged; now mostly historical for Track B.
- APP-030 multi-order golden dataset: merged.
- APP-040a Unity skeleton plus RunArtifact loader/timeline logic: merged.
- APP-040b1 Unity player UI state layer: merged.

Any locally observed `main` commit should be treated as historical observed
context unless it is verified against the remote branch at handoff time.

## Track B Boundary

Track B may work in:

```text
src/Sim.Report
src/Sim.Validation
datasets
engine/unity
docs
```

The current primary Track B working area is:

```text
engine/unity
docs
```

Without a dedicated task and reviewer approval, do not modify:

```text
src/Sim.Core
src/Sim.Cli
src/Sim.Contracts
packages/contracts
scripts
tracked golden artifacts
```

Contracts v1 is a frozen product boundary. Any contract change requires a
dedicated `CONTRACT-` PR, version review, generated contract update, drift
check, and artifact-golden review when applicable.

## Critical Product Truths

`RunArtifact v1` fields `layout.resources` and `position_timeline` are
deterministic baseline layout handoff positions. They are not simulated
movement.

Do not claim or implement the following from the current `position_timeline`:

- moving workers
- moving forklifts
- moving inventory
- route animation
- distance traveled
- path optimization
- heatmap based on real movement

Movement visualization must wait for the R2 movement-driven artifact path.
`MovementArtifact` schema, fixtures, and loaders exist, but Track B must not
quietly mix MovementArtifact visualization into a RunArtifact scene task.

Calibration, confidence grades, error intervals, closed-loop optimization
recommendations, WMS execution, and real digital twin sync are planned
capabilities. They are not current implemented product capabilities.

## Required Environment

### Windows

Recommended Windows baseline:

- Windows 11. Windows 10 is acceptable if the required SDK and Unity versions
  are installed.
- Git for Windows, including Git Bash.
- PowerShell.
- .NET SDK 8.0 matching repository `global.json`.
  - Current required SDK version is `8.0.422`.
  - `rollForward` is `latestFeature`.
- Unity Hub for Windows.
- Unity Editor `6000.3.0f1`.
- One editor: VS Code, Rider, or Cursor.

Install only the modules required by the current task. Do not install platform
build support unless a later task explicitly asks for it.

### macOS

Supported macOS baseline:

- macOS 14 or 15.
- Apple Silicon or Intel Mac.
- Xcode Command Line Tools:

```bash
xcode-select --install
```

- Git.
- .NET SDK 8.0.
  - Must satisfy repository `global.json`.
  - Current required SDK version is `8.0.422`.
  - `rollForward` is `latestFeature`.
- Unity Hub for macOS.
- Unity Editor `6000.3.0f1`.
  - Only the macOS Editor base module is required.
  - Windows, Android, iOS, and WebGL build support are not required unless a
    later task explicitly asks for them.
- One editor: VS Code, Rider, or Cursor.
- Optional but useful: Homebrew.

## Known Environment Notes

### Windows .NET and Git Bash

`scripts/check-all.sh` may default `DOTNET_ROOT` to `$HOME/.dotnet`. On Windows
this can point to a directory without the runtime and fail with `hostfxr.dll`
errors. Use:

```powershell
$env:DOTNET_ROOT = "C:/Program Files/dotnet"
$env:PATH = "C:/Program Files/dotnet;$env:PATH"
```

Then run Git Bash scripts via:

```powershell
& "C:\Program Files\Git\bin\bash.exe" scripts/check-all.sh
& "C:\Program Files\Git\bin\bash.exe" scripts/check-consumer-no-core.sh
```

### macOS .NET and Unity

`bash scripts/check-all.sh` should normally work directly from the repository
root. If multiple .NET SDKs are installed, confirm the selected SDK with:

```bash
dotnet --version
dotnet --list-sdks
```

The usual Unity Editor path installed by Unity Hub is:

```text
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity
```

## First Local Setup

### Windows

```powershell
git clone https://github.com/mxz-dddd/ai-warehouse-twin.git
cd "C:\path\to\ai-warehouse-twin"

dotnet --version
dotnet build
dotnet test

$env:DOTNET_ROOT = "C:/Program Files/dotnet"
$env:PATH = "C:/Program Files/dotnet;$env:PATH"
& "C:\Program Files\Git\bin\bash.exe" scripts/check-all.sh
& "C:\Program Files\Git\bin\bash.exe" scripts/check-consumer-no-core.sh
```

### macOS

```bash
git clone https://github.com/mxz-dddd/ai-warehouse-twin.git
cd /path/to/ai-warehouse-twin

dotnet --version
dotnet build
dotnet test
bash scripts/check-all.sh
bash scripts/check-consumer-no-core.sh
```

Expected high-level result:

```text
build: 0 warnings, 0 errors
tests: all passed
check-all: PASS: full local validation
check-consumer-no-core: PASS
```

## Unity Verification

### Windows EditMode

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.0f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "$PWD\engine\unity\AIWarehouseTwin" `
  -runTests -testPlatform EditMode `
  -testResults "$PWD\engine\unity\AIWarehouseTwin\EditMode.xml" `
  -logFile "$PWD\engine\unity\AIWarehouseTwin\EditMode.log"
```

### macOS EditMode

```bash
"/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit \
  -projectPath "$PWD/engine/unity/AIWarehouseTwin" \
  -runTests -testPlatform EditMode \
  -testResults "$PWD/engine/unity/AIWarehouseTwin/EditMode.xml" \
  -logFile "$PWD/engine/unity/AIWarehouseTwin/EditMode.log"
```

## Current Next Task

The likely next task is:

```text
APP-040b2
```

Suggested scope:

- Verify Unity installation.
- Run existing Unity EditMode tests from APP-040a and APP-040b1.
- Only after tests compile, create a minimal Unity scene/UI shell.
- Use UI Toolkit for a minimal scene/player shell.
- Bind the existing RunArtifact player UI state to actual UI.

APP-040b2 does not include:

- APP-050 spatial visualization.
- MovementArtifact route animation.
- Heatmap.
- Layout editor.
- Real movement claims.

If Unity EditMode fails because of the local Unity installation, fix the
environment first. Do not work around environment failure by hand-writing
complex Unity scene YAML.

This handoff is only Track B environment readiness. It does not mean Track B
development has started. Before the owner gives an explicit implementation
handoff instruction, do not implement new Unity features, modify core/runtime/
ingestion/contracts, modify CI, add MovementArtifact functionality, or connect
to real Runtime or Sim.Cli surfaces.

## Branch and PR Workflow

Default workflow:

```bash
git switch main
git pull --ff-only
git switch -c app/<task-id>
```

If the current owner uses detached worktree mode, ask the owner before taking
over shared `main` or changing worktree ownership assumptions.

Rules:

- One task per branch.
- Keep diffs scoped to the task.
- Do not force push unless explicitly approved.
- Prefer merge from `origin/main` into a PR branch if the reviewer asks for sync
  and force push is not approved.
- Before commit, show staged file list.
- Do not stage local notes, generated noise, or unrelated Unity import output.

Every PR should include:

- What changed.
- What did not change.
- Validation commands and actual results.
- Any Unity tests not run and why.
- Any known limitations.

## Useful Files to Read First

```text
AGENTS.md
CLAUDE.md
README.md
docs/track-b-plan.md
docs/architecture/contracts-v1-freeze.md
docs/architecture/golden-update-policy.md
docs/architecture/position-timeline-semantics.md
docs/architecture/movement-artifact-v1-proposal.md
engine/unity/AIWarehouseTwin/Assets/Scripts
engine/unity/AIWarehouseTwin/Assets/Tests/EditMode
```

## Communication Protocol

- Before changing two or more files, output a plan and wait for owner approval.
- Completed work reports must include:
  1. Modified files.
  2. Validation commands plus actual output.
  3. Risks and unresolved items.
- When blocked, include the exact command and exact error text.
- Do not silently fix an issue by expanding scope.
