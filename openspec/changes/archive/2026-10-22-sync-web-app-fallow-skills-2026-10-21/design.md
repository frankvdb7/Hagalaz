# Design

## Context

See proposal.md for the scope. The package uses Fallow CLI 3.34.1. With an explicit app root, `fallow agent install --dry-run` identifies the existing `fallow` skill as foreign and offers to replace it with a version-marked pointer to the CLI's package copy; it offers the absent `fallow-setup` skill as the same kind of pointer. The install command can skip its guide, MCP, and hook surfaces. The official `fallow-rs/fallow-skills` repository contains all four skills; the snapshot selected for `fallow-review` and `fallow-codex-onboarding` is commit `b3bb2ef396b67abe17def46aa1d96e8b1992d905`.

## Goals / Non-Goals

**Goals:**

- Keep the app-local skill directory as the single project-local source available to Codex.
- Use the Fallow CLI for the two skills it exposes from its installed package.
- Use the official skills repository for the review and Codex onboarding skills not included in the CLI's install bundle.

**Non-Goals:**

- No runtime, dependency, CI, or Fallow analysis configuration changes.
- No installation of the Fallow guide, MCP server, or hooks.

## Decisions

- Run `fallow agent install` with an explicit `Hagalaz.Web.App` root, Codex harness, and `--without guide --without mcp --without hooks`. The first dry run without `--root` resolved to the repository root, so the explicit root is required to keep writes package-local.
- Pass `--force` because CLI status and its dry run show that the tracked `fallow` directory is foreign to the installer. Its contents are clean in Git and were not modified locally, so replacing them with the CLI-managed copy is safe. The same CLI operation installs the missing `fallow-setup` skill.
- Copy `fallow-review` and `fallow-codex-onboarding` from the official repository snapshot. The CLI's embedded bundle contains only `fallow` and `fallow-setup`; installing all four by hand would bypass the supported ownership marker for the two bundled skills.
- Do not use a broad installer invocation: its default scope also manages guides, MCP configuration, and hooks, none of which is part of this change.

## Risks / Trade-offs

- The `fallow` and `fallow-setup` skills are installed as pointers into the package-local `node_modules/fallow` package. → Use the CLI-supported layout and confirm Fallow status recognizes both skills after installation.
- The two skills sourced from GitHub can drift after this sync. → Record the exact upstream commit in this change and compare all copied files with that checkout before marking the task complete.

## Migration Plan

Install the CLI-bundled skills, copy the two remaining official skill directories, then verify the four skill directories and CLI status. To roll back, restore the two tracked directories from Git and remove the two newly added skill directories.
