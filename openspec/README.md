# Repository change specifications

This directory is the repository-local, lightweight change-specification workflow for AI-assisted and human development.

## Source of truth

- `specs/` describes the currently agreed behavior of the system.
- `changes/` contains one directory per active change. Each change records its goal, scope, design, tasks, and behavior deltas.
- `changes/archive/` contains completed changes and their historical rationale.
- `AGENTS.md` contains permanent engineering and agent-working rules, not feature-specific behavior.

## Workflow

1. Explore the existing code and current specification with `$openspec-explore`.
2. Create one change directory with `$openspec-propose`, containing `proposal.md`, `design.md`, `tasks.md`, and any relevant delta specification.
3. Review the goal, non-goals, acceptance criteria, and proposed reuse before implementation.
4. Implement only the listed tasks with `$openspec-apply-change`.
5. If the plan needs to change, revise its existing artifacts with `$openspec-update-change` instead of expanding the change silently.
6. If new work is discovered outside scope, record it as a follow-up instead of expanding the change silently.
7. Verify the acceptance criteria and sync the current specification with `$openspec-sync-specs` when needed.
8. Archive the completed change with `$openspec-archive-change`, which moves it under `changes/archive/` after verification.

OpenSpec is initialized for the Codex agent in `.agents/skills/`. The project-level `openspec/config.yaml` contains the schema, context, and rules for this repository; the selected workflow profile is machine-wide and can be inspected with `openspec config list`. Run `openspec update` after changing the profile or upgrading the CLI so the generated skills stay synchronized. The process should remain lightweight and should not be used for trivial one-line changes.
