# Design

## Context

The solution contains several independent C# subsystems. The prior warning cleanup is still present in the working tree and serves as the starting point for this pass. Rider's project Problems view surfaced dependency advisories, so use a whole-solution InspectCode report to inventory source inspections.

## Goals / Non-Goals

- Verify a net reduction of at least 500 distinct source warnings, including warning-level Rider inspections.
- Keep the warning threshold, diagnostic settings, dependency versions, and public behavior stable for the comparison.
- Exclude generated files, restore advisories, and diagnostics repeated across multiple build or analysis outputs.

## Decisions

- Record a clean MSBuild rebuild and a pinned JetBrains InspectCode run with the existing solution settings. Deduplicate by file, diagnostic, and source occurrence. Count compiler warnings reported by both tools once.
- Group warnings by runtime boundary and apply one coherent set of fixes at a time. Start with checked nullability contracts, correct asynchronous ownership, and simpler existing code paths.
- Find callers and existing tests through Rider where available. Add tests for production behavior affected by each batch and run them against the baseline. Use Rider semantic refactorings where applicable. Run focused tests again after each batch.
- Repeat the full inspection and rebuild with identical versions and settings. Review new diagnostics and run each affected test project.

## Risks / Trade-offs

- Inspection and build counts can overlap. Compare the same locations and rules before and after and report counts by tool with overlaps removed.
- A single nullability edit can affect downstream callers. Verify normal, optional, and cleanup paths through existing entry points.
- Warning counts may differ with global Rider settings. Preserve the report command, version, and settings used for both runs.
