# Design

## Context

JetBrains InspectCode 2026.3.0.2 reported 559 redundant-import warnings in GameWorld and zero analysis errors in that project. The solution-wide inspector works with the .NET SDK MSBuild path explicitly selected.

## Goals / Non-Goals

- Remove at least 500 unique GameWorld source warnings.
- Keep inspection rules, dependencies, and service behavior stable.

## Decisions

- Refresh the warning inventory against the current working tree and select confirmed redundant imports in GameWorld. Preserve existing edits and count each source occurrence once.
- Use Rider to locate relevant existing tests, add cases for representative touched behavior, and run them before the import cleanup. Verify the batch with the GameWorld test suite and compare deterministic production builds without debug symbols.
- Rerun the solution rebuild and InspectCode using the baseline settings. Report new diagnostics and any existing analyzer limitations.

## Risks / Trade-offs

- The changes span many files because imports are local declarations. Only remove the reported standalone imports and review the diff for drift.
- Treat discrepancies in compiled output as a failed equivalence check and investigate their cause before counting the batch complete.
