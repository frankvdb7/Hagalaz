---
name: hagalaz-engineering-design
description: Use for non-trivial Hagalaz .NET/Angular feature design, architecture changes, service ownership, transaction/concurrency changes, cross-service behavior, and substantial refactors. Check invariants, existing mechanisms, scope, dependencies, and minimal design. Do not use for simple edits or PR review; PR reviews use hagalaz-pr-review.
---

# Hagalaz engineering design

Use this workflow for design or implementation decisions where ownership, distributed boundaries, complexity, or refactoring risk matter.

1. Start from the user request and relevant existing OpenSpec specification; define concrete scope, acceptance criteria, non-goals, runtime boundary, and failure invariants.
2. Trace existing production owners, callers, registrations, and dependencies before proposing an abstraction. Prefer existing .NET/framework/project primitives and one owner per state transition.
3. Consult [detailed engineering guidelines](references/engineering-guidelines.md) **only for the relevant sections** (ownership, concurrency, duplication, patterns, cleanup, and validation).
4. For OpenSpec planning/apply/archive, use the existing `openspec-*` skills. Do not duplicate or bypass their planning boundaries.
5. Design the smallest coherent solution; justify any new worker, synchronization mechanism, persistence mechanism, queue, dependency, or abstraction with a concrete requirement.
6. Validate affected behavior with focused tests and the appropriate build/integration gates. Review the final diff for obsolete APIs, second owners, unrelated changes, and new warnings.

For a PR review or remediation of review feedback, switch to `hagalaz-pr-review`. For jscpd-specific clone remediation use `dry-refactoring`; do not extract abstractions solely for syntax similarity.
