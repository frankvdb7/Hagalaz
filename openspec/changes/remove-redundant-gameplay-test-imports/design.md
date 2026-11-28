# Design

## Context

The cleanup spans two gameplay test projects. Rider identifies the directive candidates. The C# compiler resolves imports and the existing test runner verifies the fixtures.

## Goals / Non-Goals

- Keep existing global imports, test discovery, fixture lifetimes, and test assertions intact.
- Limit the changes to ordinary redundant imports and the characterization tests added first.

## Decisions

- Refresh the Rider report and qualify each candidate against the actual directive in the working tree. Remove only unconditional, ordinary namespace imports that Rider marks redundant. Leave aliases, static imports, and conditional directives for separate review.
- Use the solution build and bounded, non-coverage runs of the affected suites to verify the cleanup. Add characterization cases to existing fixtures and run them before editing imports.
- Compare distinct solution diagnostics with the same analyzer settings before and after. An import sorting pass or a severity change would provide weaker evidence of the requested reduction.

## Risks / Trade-offs

- Stale inspection offsets can point to another directive. Refresh the baseline and verify candidate text before removal.
- A directive can affect a symbol binding that a build alone would miss. Review deletions against the baseline and rerun both suites.
- A test may stall. Use a runner with a hang timeout and keep coverage disabled for this pass.
