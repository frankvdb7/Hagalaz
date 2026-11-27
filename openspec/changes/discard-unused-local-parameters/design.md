# Design

## Context

Rider's initial solution report identified 4,616 warnings, including unused local parameters and redundant syntax. Prior C# discard refactors in the solution already use the discard identifier for intentionally unused callback inputs.

## Goals / Non-Goals

- Remove at least 500 warnings from the recorded baseline using Rider-confirmed redundant constructs.
- Preserve parameter positions, types, delegate binding, callback bodies, and registration behavior.

## Decisions

- Verify each Rider issue against its declaration and call expression. Use the same InspectCode severity and solution scope for the baseline and final reports.
- Preserve binding and behavior through compiler verification. Use existing namespace imports and containing namespaces to resolve redundant qualifiers. Keep callback parameters in files that trigger clone findings unchanged.
- Run relevant tests and compare warning counts with the same inspection configuration.

## Risks / Trade-offs

- Stale offsets can alter another token. Apply all replacements from one report in descending source order after confirming each identifier and warning location.
- Large callback edits may trigger clone detection. Inspect the resulting duplication report and keep implicated files out of this batch.
