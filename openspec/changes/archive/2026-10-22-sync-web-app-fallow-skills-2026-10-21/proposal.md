# Proposal

## Why

`Hagalaz.Web.App/.agents/skills` contains only two Fallow skills, and both differ from current upstream content. The app is missing Fallow's setup and Codex onboarding skills, so its project-local agent guidance is incomplete.

## What Changes

- Use Fallow CLI 3.34.1 to install or update its `fallow` and `fallow-setup` skills into the app's `.agents/skills` directory.
- Update `fallow-review` and add `fallow-codex-onboarding` from the official Fallow skills repository.
- Keep the change limited to the four project-local skill directories and this OpenSpec record.

### Non-goals

- Do not change application source, package manifests, lockfiles, Fallow analysis configuration, or CI.
- Do not install user-global skills or configure Fallow guides, MCP servers, or hooks.
- Do not add a script or automation for future skill updates.

### Acceptance Criteria

- `.agents/skills` contains the four current official skills: `fallow`, `fallow-setup`, `fallow-review`, and `fallow-codex-onboarding`.
- Fallow CLI status reports the `fallow` and `fallow-setup` skill surfaces as installed or current.
- The review and onboarding skill files match the official repository revision used for this sync.
- The OpenSpec change validates and the scoped diff passes `git diff --check`.

### Stop Conditions

- Stop if the Fallow CLI install plan includes changes outside `.agents/skills` after excluding guide, MCP, and hook steps.
- Stop if an existing app-local skill contains deliberate project-specific behavior that the official skill would replace.

## Capabilities

### New Capabilities

- None. This updates developer guidance only and does not change application behavior.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Web.App/.agents/skills/fallow`
- `Hagalaz.Web.App/.agents/skills/fallow-setup`
- `Hagalaz.Web.App/.agents/skills/fallow-review`
- `Hagalaz.Web.App/.agents/skills/fallow-codex-onboarding`
- One repository-local OpenSpec change directory
