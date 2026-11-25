# Design

## Context

The last verified InspectCode report found 91 unique invalid XML documentation warnings in GameWorld. It also flagged repeated base types on partial creature classes. Refresh and deduplicate the report against the current working tree before editing.

## Goals / Non-Goals

- Resolve at least 100 distinct warning instances while preserving production contracts.
- Keep the previous import cleanups and SpawnBox test fix out of the warning count.

## Decisions

- Correct parameter names, references, and misplaced comments against the declarations they describe.
- Keep the base type on the partial declaration that owns the type definition. Remove duplicate base clauses only where the same partial type retains that declaration.
- Use Rider for static inspections and source search for test discovery. Use bounded non-coverage test runs to avoid the previously observed Rider coverage workers.
- Add and run representative characterization tests for affected production APIs before edits. Verify the final hierarchy and runtime behavior through tests and compare the compiled GameWorld assemblies if the edits allow byte-for-byte comparison.

## Risks / Trade-offs

- Partial declarations can span several files. Review each full partial type before editing its base clause.
- XML comments can contain useful contract details. Repair inaccurate references while retaining applicable descriptions.
