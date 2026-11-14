# Design

## Context

`MapSceneLoad` owns the requests of one selection and publishes loading state, counts and warnings through `CacheMapSceneStore`. Totals grow during a load because objects and textures are only known once regions and appearances arrive.

## Decisions

- `MapLoadStatusComponent` owns presentation only. It derives its view from store values passed as inputs and holds no request state. It is projected into the viewport, so it also appears in fullscreen.
- Progress is settled work over known work. Because totals grow, the component keeps the highest percentage reached during one load. A load ending, or the region count falling, starts a new percentage.
- The compact indicator fades in after 250 ms using CSS, so short loads never show it and no timer needs cleanup.
- A visually hidden live region states the phase only. Counts are visible text outside the live region, and the bar exposes its value through its own ARIA attributes.
- The page owns retry. It asks `CachePendingChangesService` first, which already prompts for a draft and refuses during a write, then starts a new load through the store. The store remains the only retry owner.

## Risks

- The percentage can pause while newly discovered work catches up. A pause is preferred to a bar that moves backward.
- A reload still clears the scene because each load owns and releases its texture bitmaps. Keeping the old scene is a non-goal.
