# PR review and remediation details

The detailed guidance below was moved from `docs/agents/` without changing its requirements. This is a task-specific reference: consult the sections relevant to the current task rather than reading it for every edit.

### Production relevance in code review

Before reporting or fixing a code-review finding, inspect the concrete production implementation and trace its real callers. Do not infer production failure behavior from mocks, test doubles, synthetic exceptions, or hypothetical implementations.

Every actionable finding must identify a realistic trigger, the violated invariant, and its observable consequence. Distinguish confirmed regressions from speculative risks.

Prefer KISS and YAGNI. Do not add defensive exception handling, synchronization, abstractions, or recovery mechanisms solely to satisfy unrealistic edge cases. Existing explicit contracts and demonstrable integrity risks remain valid reasons for defensive behavior.

When evidence is insufficient, investigate further rather than presenting speculation as a confirmed bug.

Before changing code for a reported failure path, make the production reachability trace explicit: identify the real caller and operation that creates the state, the relevant guards or synchronization, the operation that encounters the failure, and the observable consequence. Share this trace before editing. Regression tests may prove behavior for a reachable state, but synthetic test setup alone does not prove that production can reach it. If no supported production path can create the state, do not add defensive code for it; investigate further or report the concern as speculative.

### Review remediation and scope retention

Pull-request review comments are findings, not independent implementation tasks. The originating issue/spec and the complete cumulative PR diff remain the primary context throughout the review-fix cycle. Never narrow attention to only the latest comment, latest commit, or commented line.

Before implementing review feedback:

1. Re-read the originating issue/spec, including its acceptance criteria, design decisions, and non-goals.
2. Re-read the complete cumulative diff from the original PR base to `HEAD`. Incremental diffs may be used to locate new changes, but they are not sufficient for architectural decisions.
3. Read all unresolved review threads together before changing code.
4. Group related findings by root cause. Do not fix review comments one by one or treat the review thread list as a task queue.
5. Produce one minimal, coherent remediation plan that remains within the original issue/spec.
6. For every proposed change, identify which original requirement, invariant, or required regression test it serves.
7. Prefer deleting, reverting, or simplifying an earlier attempted fix over adding compensating code on top of it.

Review comments may reveal defects in the implementation, but they do not automatically extend or redefine the originating issue/spec. If a proposed fix introduces a new responsibility, abstraction, lifecycle mechanism, state owner, subsystem, or material scope expansion that is not justified by the original issue/spec, stop and reassess the broader design instead of implementing the local fix.

KISS and YAGNI still apply during review remediation. Do not add generic rollback frameworks, coordinators, wrappers, state machines, recovery mechanisms, or speculative error handling merely to satisfy individual review comments. Prefer the smallest root-cause fix that restores the intended invariant using existing ownership boundaries and project primitives.

After each logical remediation, review the entire affected flow and the cumulative base-to-`HEAD` diff, not only the code that was commented on. Resolve or explicitly supersede stale review threads once their underlying root cause is fixed so later work does not treat obsolete guidance as additional requirements.

Before declaring review feedback complete, explicitly verify:

- The final PR still solves the original issue/spec rather than a collection of review comments.
- Every original acceptance criterion is satisfied and every non-goal remains respected.
- Review-driven changes have not introduced unrelated responsibilities or architecture.
- No second mechanism or duplicate owner was introduced for an existing responsibility.
- Temporary, compensating, or now-obsolete review-driven code has been removed.
- The cumulative diff is still the smallest coherent solution now that all findings are understood together.
