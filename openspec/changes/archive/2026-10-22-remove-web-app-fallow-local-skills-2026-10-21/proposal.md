# Proposal

## Why

The Fallow plugin is installed and provides the Fallow skills to Codex. Keeping a second project-local copy under `Hagalaz.Web.App/.agents/skills` duplicates those skills and can cause duplicate or stale guidance to load.

## What Changes

- Remove the four Fallow skill definitions and supporting files from `Hagalaz.Web.App/.agents/skills`.
- Leave other project agent configuration and the installed Fallow plugin unchanged.

### Non-goals

- Do not uninstall or reconfigure the Fallow plugin.
- Do not change app source, package manifests, lockfiles, Fallow configuration, or CI.
- Do not remove unrelated files from `.agents`.

### Acceptance Criteria

- No app-local Fallow `SKILL.md` files or supporting skill files remain under `.agents/skills`; the four skills remain available from the installed Fallow plugin.
- Other project-local agent configuration is unchanged.
- The OpenSpec change validates and the scoped diff passes `git diff --check`.

### Stop Conditions

- Stop if a target directory contains files outside the four Fallow skill trees identified for removal.
- Stop if the plugin-provided Fallow skills are no longer available in the current Codex environment.

## Capabilities

### New Capabilities

- None. This removes duplicate developer guidance and does not change application behavior.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Web.App/.agents/skills/fallow`
- `Hagalaz.Web.App/.agents/skills/fallow-setup`
- `Hagalaz.Web.App/.agents/skills/fallow-review`
- `Hagalaz.Web.App/.agents/skills/fallow-codex-onboarding`
- One repository-local OpenSpec change directory
