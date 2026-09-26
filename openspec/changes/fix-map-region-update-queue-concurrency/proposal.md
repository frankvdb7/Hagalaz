## Why

World entry can fail while a region is loading because the asynchronous map
loader queues updates at the same time that the synchronous game tick sends and
clears the region-part update list. The unsynchronized list can expose a
cleared null element to `QueueUpdate`, causing the client to remain in world
loading.

## What changes

- Keep updates queued after the current tick cutoff in a pending buffer while
  publishing one frozen prepared buffer for the current client tick.
- Send only the prepared buffer so every character observes the same update
  set, and clear only that buffer when the tick completes.
- Let the worker keep successfully prepared regions in a local collection, so
  a region whose prepare phase fails is not sent client updates while all
  regions still reach the reset boundary.
- Add regression coverage for concurrent queue and clear operations.

## Impact

This affects the GameWorld map-render update boundary during asynchronous
region loading. It preserves update ordering and duplicate filtering while
making queue ownership safe across the loader and game tick.

## Acceptance criteria

- Concurrent queueing and clearing of map-part updates does not throw or expose
  null updates.
- Updates queued after preparation are deferred to the next tick, while the
  prepared set remains stable for all characters in the current tick.
- A region whose client-prepare phase fails is not sent client updates, reset
  still runs for every region, and unexpected client/NPC failures remain
  observable to the worker.
- Focused tests, the affected build, strict OpenSpec validation, and the real
  client world-entry check pass.

## Non-goals

- Do not redesign map-region loading or add another queue or worker.
- Do not change the world-entry protocol, cache decoding, persistence, or
  reconnect behavior.
