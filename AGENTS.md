# Hagalaz Agent Guide

This document provides guidance for AI agents working on the Hagalaz codebase.

## Project Overview

Hagalaz is a modern, open-source recreation of a classic MMORPG. It features a microservices architecture for the backend and an Angular/Electron application for the frontend.

- **Backend**: .NET 10 with ASP.NET Core and .NET Aspire for orchestration. The backend is composed of multiple microservices, as can be seen in the `Hagalaz.sln` solution file.
- **Frontend**: Angular with Angular Material and Tailwind CSS. The frontend is located in the `Hagalaz.Web.App` directory and is also an Electron application.
- **Database**: The `README.md` mentions MySQL. It is safe to assume a SQL database is used with Entity Framework.
- **Infrastructure**: The project uses Docker for containerization, RabbitMQ for messaging, and Redis for caching.

## Engineering Guardrails for AI Agents

These rules apply to implementation and refactoring work. They are intended to keep
solutions proportional to the problem and prevent accidental architecture expansion.

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

## Getting Started

### Backend

The backend services are orchestrated by .NET Aspire. To run the backend:

1.  Navigate to the `Hagalaz.AppHost` directory:
    ```bash
    cd Hagalaz.AppHost
    ```
2.  Run the application:
    ```bash
    dotnet run
    ```

This will start all the backend services as defined in the `Hagalaz.AppHost` project.

### Frontend

The frontend is an Angular application. To run the frontend:

1.  Navigate to the `Hagalaz.Web.App` directory:
    ```bash
    cd Hagalaz.Web.App
    ```
2.  Install the dependencies using pnpm:
    ```bash
    pnpm install
    ```
3.  Start the development server:

    ```bash
    pnpm start
    ```

    This will run the web application, which can be accessed in a browser.

4.  To run the Electron application:
    ```bash
    pnpm run launcher:start
    ```

## Building the Project

### Backend

To build the entire .NET solution, run the following command from the root directory:

```bash
dotnet build Hagalaz.sln
```

### Frontend

To build the Angular application, navigate to the `Hagalaz.Web.App` directory and run:

```bash
pnpm run build
```

For a production build of the launcher, use:

```bash
pnpm run launcher:build
```

## Testing

### Backend

The backend has a suite of unit tests. To run them, execute the following command from the root directory:

```bash
dotnet test Hagalaz.sln
```

The CI pipeline in `.github/workflows/ci.yml` runs these tests on every push and pull request.

### Frontend

The frontend has unit tests that can be run with Vitest. To run them, navigate to the `Hagalaz.Web.App` directory and run:

```bash
pnpm test
```

The CI pipeline in `.github/workflows/ci.yml` also runs these tests.

## Key Files

- `README.md`: General information about the project.
- `Hagalaz.sln`: The main solution file for the .NET projects.
- `global.json`: Specifies the .NET SDK version.
- `Hagalaz.AppHost/Hagalaz.AppHost.csproj`: The entry point for running the backend services with .NET Aspire.
- `Hagalaz.Web.App/package.json`: Defines the dependencies and scripts for the frontend application.
- `.github/workflows/ci.yml`: The CI pipeline definition for GitHub Actions.
