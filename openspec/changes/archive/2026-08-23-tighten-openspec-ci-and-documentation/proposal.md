## Why

OpenSpec artifacts are currently validated manually, so CI can accept malformed or incomplete specifications. The repository README also does not describe the current update workflow or distinguish project rules from the machine-wide workflow profile.

## What Changes

- Add a dedicated CI validation job that installs the pinned OpenSpec CLI and validates active specs and archived task completion.
- Update the OpenSpec README with the current `update-change` workflow and the distinction between project configuration and the global workflow profile.
- Keep application code, runtime behavior, and the selected workflow set unchanged.

## Capabilities

### New Capabilities

None. This is a tooling and documentation change with no spec-level behavior change.

### Modified Capabilities

None.

## Impact

- `.github/workflows/ci.yml` gains a reproducible OpenSpec validation job.
- `openspec/README.md` becomes the source of guidance for the current repository-local OpenSpec workflow.
- CI depends on the exact `@fission-ai/openspec@1.10.0` package version for this check.
