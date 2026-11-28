# Design

## Context

The last verified Rider report identified 138 warning locations across unused variables, redundant assignments, redundant default member initializers, and redundant default arguments in GameWorld and Game Scripts. Refresh the inventory against the current branch before editing.

## Goals / Non-Goals

- Resolve at least 100 distinct warnings beyond the current PR baseline.
- Preserve observable behavior, the public API, and the existing analyzer configuration.

## Decisions

- Group diagnostics by owning class and behavior. Remove unused locals while preserving initializers that perform work or can throw. Check argument binding and initializer order before removing redundant defaults.
- Add and run characterization tests for affected behavior before editing each production batch. Use source search for test discovery and bounded project-scoped tests without coverage.
- Use Rider static inspections to verify the fixes. Compare a matching solution rebuild and run the GameWorld and Game Scripts test projects.

## Risks / Trade-offs

- A value can be unused even when evaluating its initializer has visible effects. Check those calls before changing them.
- Optional arguments can bind a different overload when omitted. Verify the resolved call target before and after each applicable edit.
