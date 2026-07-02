# Track B Collaborator Handoff

## Project

Repository:

```text
https://github.com/mxz-dddd/ai-warehouse-twin
```

Local project path used by current owner:

```text
C:\个人\物流仓储优化\ai-warehouse-twin
```

Product goal: build an artifact-backed warehouse digital twin product for real warehouse customers. Current Track B work focuses on consuming artifacts/contracts into visualization and customer-facing delivery surfaces.

## Current State

Current local main observed:

```text
2de1e61 REPORT-R2b add MovementArtifact loader smoke (#56)
```

Recent completed Track B work:

- APP-010 customer report baseline: merged.
- APP-020 validation baseline: merged, now historical rather than current Track B focus.
- APP-030 multi-order golden dataset: merged.
- APP-040a Unity skeleton plus RunArtifact loader/timeline logic: merged.
- APP-040b1 Unity player UI state layer: merged.

Current local worktree has known untracked files from local notes / Unity import attempts:

```text
STATUS.md
engine/unity/AIWarehouseTwin/Packages/packages-lock.json
engine/unity/AIWarehouseTwin/ProjectSettings/*.asset
```

Do not delete, commit, or "clean up" these unless the owner explicitly asks.

## Track B Boundary

Track B may work in:

```text
src/Sim.Report
src/Sim.Validation
datasets
engine/unity
docs
```

Current Track B should mainly work in:

```text
engine/unity
docs
```

Do not modify without a dedicated task and reviewer agreement:

```text
src/Sim.Core
src/Sim.Cli
src/Sim.Contracts
packages/contracts
scripts
tracked golden artifacts
```

Contracts v1 are frozen product handoff boundaries. Any contract change needs a dedicated `CONTRACT-` PR, version review, generated contract update, drift check, and artifact-golden review where applicable.

## Critical Product Truths

`RunArtifact v1` currently contains `layout.resources` and `position_timeline`, but these are only deterministic baseline layout handoff positions. They are not simulated movement.

Do not claim or implement from current `position_timeline`:

- moving workers
- moving forklifts
- moving inventory
- route animation
- distance traveled
- path optimization
- heatmap based on real movement

Movement visualization must wait for the R2 movement-driven artifact path. `MovementArtifact` schema/fixtures/loaders exist on main, but Track B must not silently fold MovementArtifact visualization into a RunArtifact scene task.

Calibration, confidence grades, error intervals, closed-loop optimization recommendations, WMS execution, and real digital twin sync are planned capabilities, not implemented current product capabilities.

## Required Environment

Minimum shared environment:

- Git.
- .NET SDK compatible with `global.json`:

```text
8.0.422 with rollForward latestFeature
```

- A code editor such as VS Code, Rider, or Visual Studio.
- GitHub repository access.

Windows environment:

- Windows 11.
- Git for Windows, including Git Bash.
- PowerShell.

macOS environment:

- macOS 14 or 15.
- Apple Silicon or Intel Mac is acceptable.
- Xcode Command Line Tools:

```bash
xcode-select --install
```

- Git from Xcode CLT, Homebrew, or another standard installer.
- Optional but useful: Homebrew.

Unity environment for APP-040b2 and later:

```text
Unity Hub
Unity Editor 6000.3.0f1
```

Project version file:

```text
engine/unity/AIWarehouseTwin/ProjectSettings/ProjectVersion.txt
```

Unity install notes:

- Install only the platform editor/base module unless a task says otherwise.
- Do not install Android/iOS/WebGL modules for current work.
- Do not open the Unity project casually on a dirty worktree.
- If Unity generates `ProjectSettings/*.asset` or `Packages/packages-lock.json`, report the files before staging or deleting them.

Known Windows issue:

`scripts/check-all.sh` defaults `DOTNET_ROOT` to `$HOME/.dotnet`. On Windows this may point to a directory without the runtime and fail with `hostfxr.dll` errors. Use:

```powershell
$env:DOTNET_ROOT = "C:/Program Files/dotnet"
$env:PATH = "C:/Program Files/dotnet;$env:PATH"
```

Then run Git Bash scripts via:

```powershell
& "C:\Program Files\Git\bin\bash.exe" scripts/check-all.sh
```

Known macOS notes:

- `bash scripts/check-all.sh` should normally work directly from the repository root.
- If multiple .NET SDKs are installed, confirm the selected SDK with:

```bash
dotnet --version
dotnet --list-sdks
```

- The usual Unity Editor path installed by Unity Hub is:

```text
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity
```

## First Local Setup

Clone on Windows:

```powershell
git clone https://github.com/mxz-dddd/ai-warehouse-twin.git
cd "C:\path\to\ai-warehouse-twin"
```

Clone on macOS:

```bash
git clone https://github.com/mxz-dddd/ai-warehouse-twin.git
cd /path/to/ai-warehouse-twin
```

Restore/build/test:

Windows PowerShell or macOS shell:

```powershell
dotnet build
dotnet test
```

Full local validation on Windows:

```powershell
$env:DOTNET_ROOT = "C:/Program Files/dotnet"
$env:PATH = "C:/Program Files/dotnet;$env:PATH"
& "C:\Program Files\Git\bin\bash.exe" scripts/check-all.sh
& "C:\Program Files\Git\bin\bash.exe" scripts/check-consumer-no-core.sh
```

Full local validation on macOS:

```bash
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

Unity verification after Unity Editor is installed on Windows:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.0f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "C:\path\to\ai-warehouse-twin\engine\unity\AIWarehouseTwin" `
  -runTests -testPlatform EditMode `
  -testResults "C:\path\to\ai-warehouse-twin\engine\unity\AIWarehouseTwin\EditMode.xml" `
  -logFile "C:\path\to\ai-warehouse-twin\engine\unity\AIWarehouseTwin\EditMode.log"
```

Unity verification after Unity Editor is installed on macOS:

```bash
"/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit \
  -projectPath "$PWD/engine/unity/AIWarehouseTwin" \
  -runTests -testPlatform EditMode \
  -testResults "$PWD/engine/unity/AIWarehouseTwin/EditMode.xml" \
  -logFile "$PWD/engine/unity/AIWarehouseTwin/EditMode.log"
```

## Current Next Task

Likely next task: APP-040b2.

Suggested scope:

- Verify Unity installation.
- Run existing Unity EditMode tests from APP-040a/b1.
- Only after tests compile, create a minimal Unity scene/UI shell.
- Use UI Toolkit for the minimal scene/player shell.
- Bind existing RunArtifact player UI state to actual UI.

Do not include:

- APP-050 spatial visualization.
- MovementArtifact route animation.
- Heatmap.
- Layout editor.
- Real movement claims.

If Unity EditMode fails because of the local Unity installation, fix the environment first. Do not work around environment failure by hand-writing complex Unity scene YAML.

## Branch and PR Workflow

For each task:

```bash
git switch main
git pull --ff-only
git switch -c app/<task-id>
```

Rules:

- One task per branch.
- Keep diffs scoped to the task.
- Do not force push unless explicitly approved.
- Prefer merge from `origin/main` into a PR branch if the reviewer asks for sync and force push is not approved.
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

Before changing two or more files, output a plan and wait for owner approval.

For completed work, report in three parts:

1. Modified files.
2. Validation commands plus actual output.
3. Risks and unresolved items.

When blocked, include the exact command and exact error text. Do not silently "fix" by expanding scope.
