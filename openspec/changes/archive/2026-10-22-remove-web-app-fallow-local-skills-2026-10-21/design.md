# Design

## Context

See proposal.md for the reason for removing the package-local copies. The current Codex environment exposes the four Fallow plugin skills. The app-local directory contains exactly the four Fallow skill directories and no other skill directories.

## Goals / Non-Goals

**Goals:**

- Use the installed Fallow plugin as the only source of these skills in this workspace.
- Remove only files under the four named package-local skill directories.

**Non-Goals:**

- Do not invoke Fallow's broad agent uninstall command, which also manages guide, MCP, and hook surfaces.

## Decisions

- Delete files from only the four verified Fallow skill trees. This keeps the removal limited to `.agents/skills` and preserves all other project agent configuration.
- Leave `.agents/skills` in place for future project-local skills. Empty directory shells have no tracked content and do not load as skills.

## Risks / Trade-offs

- The plugin must be available in Codex for these skills to load. → Confirm the four plugin skills are present in the active environment before removal.

## Migration Plan

Remove the four skill trees' files and verify no local `SKILL.md` remains. To roll back, restore their contents from the preceding `sync-web-app-fallow-skills-2026-10-21` change.
