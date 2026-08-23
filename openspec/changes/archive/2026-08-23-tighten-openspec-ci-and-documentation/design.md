## Context

The repository already has a project-local OpenSpec root and generated Codex skills, but `.github/workflows/ci.yml` does not validate OpenSpec artifacts. The CLI is installed as `@fission-ai/openspec` and is currently pinned locally at `1.10.0`.

## Goals / Non-Goals

**Goals:**

- Make active-spec and archived-task validation a reproducible CI check.
- Keep the repository README aligned with the generated `update-change` and `sync-specs` workflows and the distinction between project and global configuration.

**Non-Goals:**

- Changing the selected workflow profile.
- Adding the optional `verify` workflow.
- Changing application code, OpenSpec requirements, or existing change artifacts.

## Decisions

- **Use a dedicated CI job.** OpenSpec validation is independent of the .NET and frontend jobs, so it gets its own checkout and Node setup rather than being hidden inside an unrelated job.
- **Pin the CLI package exactly.** Use `@fission-ai/openspec@1.10.0` in CI so validation behavior cannot drift with the npm `latest` tag.
- **Run two checks.** Use strict non-interactive validation for all active changes/specs and the separate archived-task check. The latter is intentionally not combined with strict artifact validation because it has a distinct CLI purpose.
- **Document global versus project configuration.** `openspec/config.yaml` remains the project source for schema, context, and rules; the workflow profile is managed by the machine-wide OpenSpec configuration and applied with `openspec update`.

## Risks / Trade-offs

- [Risk] A future OpenSpec upgrade may make the pinned CI version stale. → Mitigation: update the pin and regenerate `.agents/skills/` together when upgrading OpenSpec.
- [Risk] CI will fail when a change or spec is malformed. → Mitigation: this is the intended guardrail; the command is non-interactive and reports the exact artifact to fix.
