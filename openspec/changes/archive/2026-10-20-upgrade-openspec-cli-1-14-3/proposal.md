# Proposal

## Why

The repository's CI still validates OpenSpec artifacts with CLI 1.10.0, while the current stable CLI is 1.14.3. The generated Codex workflow skills also still identify themselves as 1.10.0 and omit the enabled `verify` workflow. A first strict 1.14.3 validation also exposed existing compatibility findings in active OpenSpec artifacts that would make the upgraded CI fail.

## What Changes

- Refresh the selected Codex OpenSpec workflow skills with CLI 1.14.3, including the enabled verification workflow.
- Pin both OpenSpec CI validation steps to 1.14.3.
- Update the repository OpenSpec README to include the verification workflow and keep CLI/profile refresh guidance accurate.
- Repair only artifacts reported by the 1.14.3 strict validator: shorten its 40 overlong requirement descriptions while preserving behavior and scenarios, and mark the two empty active change placeholders as no-spec changes.

## Scope and Non-Goals

- This is a repository OpenSpec tooling and specification-text compatibility update; no production code, product behavior, or runtime boundary changes.
- Keep the machine-wide custom workflow profile unchanged. Generate only the workflows it currently selects.
- Do not adopt experimental stores, worksets, custom schemas, or the additional workflows not selected by the current profile.
- Do not change OpenSpec artifacts that are not reported by the 1.14.3 validator.
- Do not add a package dependency.

## Acceptance Criteria

1. All generated Codex OpenSpec skills identify CLI 1.14.3, and the selected `verify` workflow is present.
2. Strict active-artifact and archived-task validation passes with the exact current stable CLI version, 1.14.3.
3. The README accurately describes the selected verification workflow and the project-versus-machine configuration boundary.
4. CI runs those same active and archived validations with CLI 1.14.3.
5. `git diff --check` passes and no production source files change.

## Stop Conditions

- Stop if a compatibility repair would require changing production code or product behavior.
- The two empty active change placeholders were confirmed by the user as no-behavior changes; mark them `skip_specs: true`.
- Stop if new validator failures appear outside the listed compatibility findings; record them as a follow-up instead of expanding this change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This tooling-only change sets `skip_specs: true` in `.openspec.yaml`.

## Impact

Affected files are the generated `.agents/skills/openspec-*` instructions, `.github/workflows/ci.yml`, `openspec/README.md`, the current `item-container-storage` and `trading-completion` specs, and the active delta specs/metadata reported by CLI 1.14.3. No application APIs, runtime dependencies, or deployment behavior change.
