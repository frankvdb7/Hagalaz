# Design

## Context

The current branch includes earlier Game Scripts and GameWorld warning cleanups. Confirm a new baseline against the current source before counting any further fixes.

## Goals / Non-Goals

- Resolve at least 100 additional distinct warning locations in Game Scripts.
- Preserve production behavior, dependency versions, and inspection settings.

## Decisions

- Use the existing pinned InspectCode version at warning severity. Confirm source locations and keep only diagnostics in the target project.
- Remove only imports reported redundant. Correct XML parameter names and references against the associated signatures while retaining relevant contract details.
- Add representative tests for affected scripts and run them before editing production files. Use source search for test discovery and a project-scoped non-coverage runner with a hang timeout.
- Repeat the inspection and solution rebuild with matching settings. Compare the deterministic Game Scripts assemblies built without debug symbols and run the full Game Scripts test project.

## Risks / Trade-offs

- Import removal is mechanical but spans multiple files. Review the diff for accidental edits and confirm compiled assembly equivalence.
- XML comments can expose assumptions used by callers. Validate descriptions against production implementations and callers.
