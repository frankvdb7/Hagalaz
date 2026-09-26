# Fix stale map-region teardown resurrection

Delayed removal and existing-object update paths currently use the
create-capable map-region lookup. A stale callback can therefore resume an
idle region or create and schedule loading for a region that was already
removed, and stale game-object collision work can reach a replacement region.

## In scope

- Use the existing non-resurrecting map-region lookup for ground-item removal,
  game-object removal, object collision mutation, and region updates.
- Preserve exact game-object ownership before applying collision or
  existing-object update behavior.
- Add deterministic lifecycle and delayed-update regression coverage.

## Out of scope

- Changes to `GetOrCreateMapRegion` semantics for legitimate creation paths.
- Logout, CodeQL, map-region architecture refactoring, or broad cleanup.

## Acceptance criteria

- Stale teardown never creates or resumes a map region.
- Stale work from a removed dynamic dimension is treated as a no-op and does
  not recreate the dimension or schedule loading.
- Teardown against a suspended canonical region does not activate it.
- Stale game-object callbacks cannot mutate replacement collision or queued
  object state.
- Legitimate creation still requests loading for a new canonical region.
