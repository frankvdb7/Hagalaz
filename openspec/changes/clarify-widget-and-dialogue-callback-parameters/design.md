# Design

## Context

The current working tree includes earlier warning cleanups. The latest inspection has 348 candidate lambda warnings in widgets, 82 in dialogues, and 256 in area scripts. Count only warnings present at the start of this pass.

## Goals / Non-Goals

- Resolve at least 500 additional warnings at distinct source locations.
- Preserve callback behavior, public APIs, and prior work.

## Decisions

- Refresh the solution build and warning-level InspectCode inventory. Select related handlers in complete batches by script or widget.
- Before editing each batch, add and run representative behavior tests against real callback registrations. Discover tests through source search and run them without coverage with the existing hang timeout.
- Match each diagnostic to its lambda parameter. Check single-underscore bindings before changing a name, and preserve references captured by nested lambdas.
- Snapshot affected files before applying edits. Verify the diff against that snapshot so existing changes are preserved and delegate bodies and registration arguments remain identical.
- Repeat the solution rebuild and inspection with matching settings. Run focused and full Game Scripts tests.

## Risks / Trade-offs

- A single underscore can bind as a named parameter. Check that it cannot shadow an existing name or alter a discard in the callback body.
- Identical callback shapes can drive different UI actions. Keep registration IDs and argument order intact, and test representative interactions across the affected groups.
