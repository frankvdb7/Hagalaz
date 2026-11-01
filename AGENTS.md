# Hagalaz Agent Guide

Hagalaz uses .NET 10 / ASP.NET Core, .NET Aspire microservices, and an Angular/Electron frontend under `Hagalaz.Web.App`. Confirm actual dependencies and runtime boundaries in source.

## Core rules

- Apply **KISS, YAGNI, and pragmatic SOLID**. Prefer existing project/framework capabilities over speculative abstractions or dependencies.
- Identify the concrete goal, acceptance criteria, non-goals, and affected runtime boundaries. Keep one authoritative owner for every responsibility and state transition.
- Search existing implementations and production callers before adding or replacing behavior. Centralize duplicated business rules, not merely similar syntax.
- Avoid unsupported defensive paths, second locks/workers/queues/caches/retries, test-only production hooks, dead APIs, and unrelated changes.
- For non-trivial changes follow the existing `openspec/` change workflow and relevant `openspec-*` skills. Respect their planning/implementation boundaries.
- Verify changes with focused tests and appropriate broader checks. Examine the diff and report unverified checks and newly introduced warnings.

## Load task-specific skills

Skills live under `.agents/skills/`. Select the relevant skill by task; do **not** load all of them for every change.

- **Architecture, non-trivial refactoring, ownership, concurrency, and cross-service design:** `hagalaz-engineering-design`.
- **Pull-request reviews, production-reachability investigations, and review remediation:** `hagalaz-pr-review`. Findings must be based on real production paths, not synthetic test-only states.
- **jscpd duplication remediation:** `dry-refactoring`, with attention to semantic rather than syntactic duplication.
- **OpenSpec planning/implementation/verification/archiving:** use the specific existing `openspec-*` skill.
- **Frontend design work:** `frontend-design`.

## Entry points

- Backend: `Hagalaz.sln`, `Hagalaz.AppHost/`, `dotnet build Hagalaz.sln`, `dotnet test Hagalaz.sln`.
- Frontend: `Hagalaz.Web.App/` (check `package.json` for build and test scripts).
- Setup/CI: `README.md`, `global.json`, `.github/workflows/ci.yml`.
- Behavior and changes: `openspec/specs/`, `openspec/changes/`.
