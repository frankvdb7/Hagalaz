# Design

## Context

The remaining inspection findings span 14 tab widgets. Earlier uncommitted changes touch a subset of these files, so compare against a snapshot of the current working tree.

## Goals / Non-Goals

- Verify at least 100 additional warnings removed with matching Rider settings.
- Preserve callback behavior and keep earlier changes intact.

## Decisions

- Refresh InspectCode at warning severity and record the solution build baseline.
- Match each diagnostic to a specific lambda parameter. Use discards for unused inputs, and preserve names referenced in the callback or captured by nested lambdas.
- Add focused characterization cases for representative click, drag, or use-on callbacks. Find tests through source search and run them without coverage before editing production code.
- Review the production diff against the working-tree snapshot. Repeat focused tests, the full Game Scripts suite, the solution build, and InspectCode.

## Risks / Trade-offs

- Some callbacks share arity but serve different widget actions. Keep argument order, handler registration, and body unchanged.
- A checkout-relative diff against HEAD includes prior work. Use the task baseline to attribute edits and warning changes to this pass.
