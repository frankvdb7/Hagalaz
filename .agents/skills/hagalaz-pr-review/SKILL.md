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

Do not automatically broaden the original feature because review comments request it. If scope must expand materially, stop and explain the design decision. For substantial implementation design also use `hagalaz-engineering-design`.
