# Hagalaz Agent Guide

Hagalaz is a .NET 10 / ASP.NET Core microservices project with .NET Aspire orchestration, plus an Angular/Electron frontend in `Hagalaz.Web.App`. Consult the repository source and configuration rather than assuming an integration or dependency exists.

## Always-applicable guardrails

- Follow **KISS, YAGNI, and pragmatic SOLID**. Prefer direct, readable code and existing framework/project primitives over speculative layers, abstractions, dependencies, or design patterns.
- Define the problem, acceptance criteria, non-goals, runtime boundary, and invariants before non-trivial changes. Stay within the agreed scope; stop and reassess material scope expansion.
- Give each responsibility and state transition **one clear owner and one authoritative implementation**. Search for existing mechanisms and callers before adding helpers, locks, queues, retries, caches, mappers, or services.
- Centralize duplicated **business rules**, not merely similar syntax. Do not introduce a second mechanism, redundant wrappers, test-only production hooks, or dead APIs.
- Make cancellation, failure recovery, synchronization, and ownership explicit where they affect production correctness. Do not add defensive complexity for hypothetical states unsupported by real production paths.
- For non-trivial work, use the `openspec/` change workflow: record scope, non-goals, acceptance criteria, design decisions, and tasks; keep the spec aligned with implementation; archive only after validation.
- Keep unrelated formatting, generated files, CI, and dependency churn out of a focused change unless required by the goal.

## Investigations and PR reviews

- Trace **real production callers, guards, and state transitions** before reporting or fixing a defect. A mock, synthetic exception, or constructed test state alone does not prove production reachability.
- For actionable findings, explain the realistic trigger, violated invariant, and observable consequence; distinguish confirmed defects from speculative risks.
- For PR review/remediation, retain the original issue/spec, complete **base-to-HEAD diff**, and unresolved review threads as context. Group related findings by root cause; prefer simplification over compensating fixes.
- Do not silently expand the original requirements to satisfy review comments. After remediation, recheck the complete affected flow and cumulative diff.
- For full review or review remediation, read [PR review and remediation guidelines](docs/agents/pr-review-guidelines.md).

## Implementation and verification

- For substantial design/refactoring work, read the applicable parts of [engineering guidelines](docs/agents/engineering-guidelines.md).
- Prefer targeted search and focused tests first, then run the build, integration tests, or broader checks appropriate to the affected boundary. Verify the real runtime topology when distributed behavior matters.
- Review the diff for regressions, duplicate paths, ownership issues, unnecessary complexity, and obsolete code. Report what was not verified.
- Do not introduce new compiler/analyzer warnings; distinguish existing warnings from those caused by the change.

## Useful entry points

- Backend: `Hagalaz.sln`, `Hagalaz.AppHost/`, `dotnet build Hagalaz.sln`, `dotnet test Hagalaz.sln`.
- Frontend: `Hagalaz.Web.App/`, `pnpm run build`, `pnpm test`; see its `package.json` for other scripts.
- Infrastructure and CI: `Hagalaz.AppHost/`, `.github/workflows/ci.yml`, `global.json`.
- Project setup: [README.md](README.md). Current behavior: `openspec/specs/`; proposed changes: `openspec/changes/`.

Avoid reading every referenced document automatically; consult the guide or spec relevant to the current task.
