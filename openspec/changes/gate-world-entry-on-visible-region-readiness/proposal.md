## Why

World entry currently sends the startup map update before the asynchronous visible-region load finishes. The client can therefore see NPCs and objects appear after entering the world and can issue movement while clipping is still incomplete. The entry path needs one readiness barrier so the initial world view is complete before it is exposed.

## What Changes

- Reuse the existing single-reader map-region scheduler to await every region in the character's initial visible viewport.
- Keep the existing complete region-load pipeline as the readiness boundary: clipping, static and custom objects, ground items, and NPCs must all finish before entry continues.
- Start character registration and its startup map/entity packets only after all visible regions are ready.
- Treat any region-load failure as a terminal world-entry failure: run existing cleanup, disconnect the session cleanly, and do not retry within the entry operation.
- Keep the public synchronous map-update API used by scripts unchanged.

## Capabilities

### New Capabilities

- `world-entry-region-readiness`: World entry waits for the complete initial visible-region population and fails cleanly when that population cannot be completed.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Services/MapRegionLoadScheduler.cs`
- `Hagalaz.Services.GameWorld/Mediator/Consumers/WorldSignInCommandConsumer.cs`
- `Hagalaz.Services.GameWorld.Tests` scheduler and world-sign-in regression tests
- No new dependencies, client protocol changes, public script-facing async APIs, or changes to later viewport updates.
