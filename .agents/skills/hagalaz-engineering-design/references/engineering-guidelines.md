# Engineering implementation details

The detailed guidance below was moved from `docs/agents/` without changing its requirements. This is a task-specific reference: consult the sections relevant to the current task rather than reading it for every edit.

### Change specifications and scope memory

- For non-trivial changes, use the repository-local specification workflow in `openspec/`.
- Before implementation, create one change directory containing a proposal, explicit non-goals, acceptance criteria, design decisions, and implementation tasks.
- Treat the proposal as a scope boundary. A related problem is not automatically part of the current change.
- If implementation discovers work outside the proposal, stop and record it as a follow-up instead of silently expanding the current change.
- Every implementation task must map to an acceptance criterion or a required regression test. Remove orphaned tasks before completion.
- Update the design or delta specification when an approved decision changes; do not let code and the change record diverge.
- Archive a completed change only after the acceptance criteria pass and the current behavior specification reflects the result.
- `AGENTS.md` contains permanent working rules; `openspec/specs/` contains current behavior; `openspec/changes/` contains temporary change intent and history. Do not duplicate the same rule in all three places.

### Start with scope and invariants

- State the concrete problem, acceptance criteria, non-goals, and affected runtime boundary before changing code.
- Write down the invariants and failure cases that must hold. Design around those invariants rather than around anticipated features.
- If the requested change grows into a second subsystem, crosses unrelated project areas, or materially exceeds the original scope, stop and present a revised design before continuing.

### Prefer one owner and one mechanism

- Every piece of state must have one clear owner and one authoritative source of truth.
- Do not add a second worker, queue, retry path, cache, lock, or coordinator for a responsibility already handled by existing infrastructure unless a measurable requirement justifies it.
- Prefer existing framework and project primitives, especially BCL collections, channels, timers, dependency injection, and the existing worker services.
- A new abstraction must simplify a real boundary or remove duplication. Do not add interfaces, generic frameworks, or patterns solely for theoretical extensibility or test mocking.

### Complexity and dependency stop rules

- Do not add a third-party package until the existing solution and BCL alternatives have been checked and the dependency cost is justified.
- Do not introduce a background service, custom queue, retry scheduler, persistence mechanism, or state machine without documenting why a current component cannot provide it.
- Prefer a small domain-specific method or coordinator over a generic framework. Avoid boolean mode flags, parallel state stores, and duplicate implementations of the same transition.
- If a feature needs multiple new states, define the legal transitions and ownership of each transition before implementation.

### Design principles and pattern guardrails

- KISS and YAGNI are the defaults. Implement the smallest design that satisfies the current acceptance criteria; do not design speculative extension points.
- Apply SOLID pragmatically, not mechanically:
  - **Single Responsibility**: keep one cohesive reason to change, but do not split every method or create classes that only forward calls.
  - **Open/Closed**: add an extension point only when there are real variants or a demonstrated change boundary; do not pre-build plugin systems.
  - **Liskov Substitution**: implementations must preserve the complete behavioral contract, including failure, cancellation, ownership, and ordering semantics.
  - **Interface Segregation**: define interfaces around consumer needs, but do not create one interface per class or one interface solely to enable mocking.
  - **Dependency Inversion**: depend on stable domain or framework abstractions at real boundaries; do not hide simple in-process code behind unnecessary layers.
- Use a design pattern only when it solves a named problem. Before adding one, state the problem, the forces/trade-offs, the chosen pattern, and why direct composition is insufficient.
- Prefer composition and explicit domain operations over inheritance and generic frameworks. Use patterns such as Strategy, Command, Adapter, Factory, or Coordinator only when there are actual alternatives, external boundaries, complex creation rules, or duplicated orchestration to centralize.
- Do not use a Strategy for one behavior, a Factory for trivial construction, a State Machine for two simple branches, a Repository over an already suitable data abstraction, or a Mediator merely to avoid direct calls.
- DRY means avoiding duplicated business knowledge, not eliminating every similar-looking line. Keep small duplication when abstraction would make the code harder to read.
- Prefer explicit, readable control flow. Use guard clauses and small named methods when they clarify a rule; do not replace a simple flow with nested patterns, flags, or indirection.
- Every abstraction must have a clear owner, a focused contract, and a reason it can be removed or extended. If those cannot be stated, keep the code direct.

### Duplication prevention

- Before adding a helper, service, validator, retry path, mapper, or framework integration, search the repository for existing code that already owns the same behavior. Prefer reuse over parallel implementations.
- Centralize business rules that must change together: state transitions, authorization decisions, validation, cleanup ownership, retry policy, serialization, and error mapping should each have one authoritative implementation.
- Repeated orchestration is a strong signal for a focused coordinator or domain service. Repeated syntax alone is not. Do not extract a generic utility just because two short methods look similar.
- Use this practical threshold: tolerate small duplication when the code has different reasons to change; centralize when the same rule appears in multiple places or every future fix would need synchronized edits.
- When similar operations have genuinely different semantics, keep separate named operations or use a small explicit strategy/command model. Do not merge them behind boolean flags or a misleading shared helper.
- Before deleting or replacing code, search all production callers, tests, configuration, and registrations. Remove obsolete APIs rather than leaving compatibility wrappers with no consumer.
- Test helpers may remove repetitive setup, but must not hide the behavior under test or become a second implementation of production logic.
- During review, search for duplicate method names, log messages, constants, transition checks, and exception handling in addition to visually comparing the diff.

### API and cleanup discipline

- Every new public or internal API must have a production caller or a clearly documented extension point. Remove obsolete APIs and test-only production paths.
- Failure handling should converge back to the authoritative state and be recoverable after restart. Do not rely solely on an in-memory queue for cleanup that must eventually happen.
- Keep cancellation semantics explicit, especially after a state transition has committed. Do not allow best-effort cleanup to undo a successful primary operation.
- Keep unrelated formatting, generated files, solution changes, CI changes, and dependency churn out of focused changes unless they are required.

### Review checkpoints

- Before implementation: provide a minimal design and identify what existing code is reused.
- During implementation: re-check the diff after each logical step and stop when a second mechanism for the same responsibility appears.
- Before completion: review for regressions, duplicate paths, dead code, ownership leaks, cancellation behavior, dependency weight, and readability—not only test failures.
- Validate the actual runtime topology with targeted tests, integration tests where boundaries are distributed, a build, and a clean diff check. Report what was not verified.
- Do not introduce new compiler or analyzer warnings. When a build reports warnings, distinguish warnings introduced by the change from pre-existing warnings.
