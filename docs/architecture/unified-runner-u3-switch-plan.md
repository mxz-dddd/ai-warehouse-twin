# CORE-U3 Default Runner Switch Plan

## Current State

* CORE-U2 added `WarehouseScenarioToUnifiedScenarioAdapter`.
* CORE-U3a added `RunWithUnifiedAdapter(...)` as an explicit opt-in path.
* CORE-U3b readiness tests confirm the current default `Run(...)` path is still legacy.
* `RunWithTrace(...)`, CLI, artifact export, report, validation, and compare paths remain unchanged.

## Proposed CORE-U3 Change

Future CORE-U3 work may need to modify these files, but CORE-U3b does not modify them:

* `src/Sim.Core/Scenarios/WarehouseScenarioRunner.cs`
* possibly `src/Sim.Cli/**`
* artifact/export path if the default artifact source changes
* tests that freeze legacy/default behavior
* architecture docs and customer-facing handoff docs

## Expected Risks

* artifact golden output may change
* trace shape may change
* CLI behavior may change if wired to the unified path
* `compare-files` may need compatibility handling
* validation/report consumers may depend on existing RunArtifact fields
* unified coarse operation mapping may not preserve every legacy stage-level lease detail

## Required Gates Before CORE-U3 Merge

* `dotnet build`
* `dotnet test`
* artifact golden review
* explicit before/after CLI comparison
* `export-artifact` output comparison
* `compare-files` compatibility check
* validation/report compatibility check
* no contracts/schema change unless explicitly approved

## Non-goals for CORE-U3b

* no default runner switch
* no artifact regeneration
* no CLI behavior change
* no contract change
* no Unity change
