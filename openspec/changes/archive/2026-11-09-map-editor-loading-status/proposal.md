# Proposal

## Why

The map editor reports loading with an indeterminate bar and a long line of counters above the workspace. The bar moves the whole workspace when it appears and disappears, the canvas stays blank during the first load, the counters change every 250 ms in a live region, and read failures vanish as soon as loading ends. Saves and world map refreshes show no pending state.

## What Changes

Goal: show the map editor's loading, writing and read-failure states inside the viewport, without moving the workspace.

In scope:

- A centered state in the viewport while the first region loads, and when nothing can be drawn.
- A compact, non-blocking progress indicator once a scene is visible, using the load counts the store already publishes.
- A visible pending state for saves and world map refreshes.
- A persistent summary of failed reads, with their messages and a retry.

Non-goals:

- Faster loading, or changes to request batching and concurrency.
- Keeping the previous scene visible during a reload.
- Changes to the region save or world map APIs.

Existing mechanisms reused: `CacheMapSceneStore` load state and progress, `MapSceneLoad` warnings, and `CachePendingChangesService` for confirming that a retry discards a draft.

Acceptance criteria:

- Loading does not change the size or position of the workspace panels.
- The progress indicator never moves backward during one load, and restarts for a new load.
- A fast incremental load does not flash an indicator.
- Failed reads stay visible after loading ends, and retry reloads the selection.
- Retrying with a draft asks before discarding it.
- Screen readers hear changes of state, not every counter update.

Stop conditions: if progress cannot be derived from the published counts, stop and report rather than adding a second progress owner.

## Capabilities

### New Capabilities

- `map-editor-loading-feedback`: How the map editor presents loading, writing and read-failure states.

### Modified Capabilities

None.

## Impact

Admin map editor page, viewport projection and scene panel empty text. No backend or store contract changes.
