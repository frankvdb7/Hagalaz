---
name: hagalaz-pr-review
description: Use for reviewing Hagalaz pull requests, code-review feedback, regressions, production bug reachability, or implementing PR review remediation. Inspect full cumulative base-to-HEAD changes, trace real runtime callers, challenge hypothetical failure states, and retain originating issue/spec scope.
---

# Hagalaz PR review and remediation

Apply for code reviews and review-driven fixes. Read [review and remediation guidelines](references/pr-review-guidelines.md) for the detailed evidence and scope rules.

1. Establish PR base/head, originating issue, current OpenSpec, acceptance criteria, and non-goals. For follow-up reviews inspect the **cumulative base-to-HEAD diff**, using incremental diffs only to locate newer edits.
2. Trace real production callers, state transitions, guards, synchronisation, and failure paths before reporting a defect. Synthetic mocks or artificially constructed test states do not establish reachability.
3. For each actionable finding identify a **realistic trigger**, **violated invariant**, **observable consequence**, severity, and exact code reference. Investigate unsupported assumptions instead of proposing speculative defensive code.
4. Read unresolved review threads together. Group findings by root cause and choose a minimal fix within the original scope, preferring simplification or removal of prior compensating changes.
5. Verify affected runtime behavior and original acceptance criteria; check for new owners, duplicate mechanisms, excess abstractions, dead code, and warnings. Report checks that could not run.

## Efficient full-PR coverage

- Record the actual PR base/head, inventory all changed files and their sizes, and inspect the **complete cumulative diff** in manageable file or subsystem batches. Keep track of what was examined; never treat the latest commit or a truncated diff as full coverage.
- Prefer targeted path/symbol searches and relevant source ranges to broad repository dumps. Expand into **all relevant production callers, tests, registrations, and failure paths** until each material finding is adequately grounded.
- During follow-up rounds, use incremental diffs to locate changes and prioritize reinspection, then recheck the cumulative PR for architectural consistency and regressions. Reuse earlier evidence only if it remains valid; do not skip files that need renewed inspection.
- Keep full diagnostic logs available and report exit status, relevant failures, warning deltas and checks not run. Avoid returning repetitive success output, but never suppress evidence or reduce review scope for token savings.
- See [scoped git/rg and .NET output examples](../hagalaz-engineering-design/references/context-efficient-commands.md) when useful. Avoid rigid tool-call/read limits.

Do not automatically broaden the original feature because review comments request it. If scope must expand materially, stop and explain the design decision. For substantial implementation design also use `hagalaz-engineering-design`.
