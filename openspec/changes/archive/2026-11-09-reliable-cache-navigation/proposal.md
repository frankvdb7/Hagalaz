# Reliable Cache Studio navigation

## Goal
Preserve browsing context and unsaved local work during existing Cache Studio navigation (supervisor checkpoint D1 + D2).

## Scope and acceptance criteria
- AC1: The URL owns applied kind, query, offset and limit. Draft search remains local. Back, Forward, reload and detail return restore browsing without duplicate dispatch.
- AC2: All supported definition editors and staged sprite uploads request Keep editing / Discard changes on navigation, including reused detail IDs. Clean forms never prompt; cancellation preserves input and URL.
- AC3: Failed/cancelled writes preserve edits. Only confirmed writes establish a clean baseline; pending writes block navigation. Native unload warnings protect local edits where supported.
- AC4: Relevant regression tests, coverage, admin build, Biome, Fallow, OpenSpec and desktop/mobile browser checks pass, with fixture limitations reported.

## Non-goals
Request-state redesign (D3), discovery/copy changes (D4), visual redesign, backend changes, autosave, persistent drafts, World Studio, commits or pushes.

## Reuse and stop conditions
Reuse existing page stores, HTTP resources, Signal Forms, Angular Router and Material dialogs. Stop for supervisor review after D1+D2; record additional findings separately.
