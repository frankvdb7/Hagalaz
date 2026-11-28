# Design

## Context

Rider's solution-wide inspection identifies 557 redundant using directives. The warning type applies to imports that the project code does not require.

## Goals / Non-Goals

- Remove at least 500 distinct confirmed redundant-using warnings.
- Avoid changes to executable statements, declaration signatures, and project configuration.

## Decisions

- Use refreshed issue offsets and directive text to select ordinary namespace imports only.
- Keep imports inside conditional compilation regions and skip aliases and static imports.
- Verify the source diff contains only selected import deletions, then run the solution build and compare warning counts using identical Rider settings.

## Risks / Trade-offs

- Inspection offsets can become stale after editing. Apply changes from a single captured report and verify every removed line against that report.
- Removing imports may expose an ambiguous or unresolved name. The solution rebuild detects binding changes.
