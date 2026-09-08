## Why

World entry can fail while a region is loading because the asynchronous map
loader queues updates at the same time that the synchronous game tick clears
the region-part update list. The unsynchronized list can expose a cleared null
element to `QueueUpdate`, causing the client to remain in world loading.

## What changes

- Synchronize access to each map-part's pending update queue.
- Send a stable snapshot of pending updates so clearing the queue cannot alter
  an in-progress send.
- Add regression coverage for concurrent queue and clear operations.

## Impact

This affects the GameWorld map-render update boundary during asynchronous
region loading. It preserves update ordering and duplicate filtering while
making queue ownership safe across the loader and game tick.

## Acceptance criteria

- Concurrent queueing and clearing of map-part updates does not throw or expose
  null updates.
- A pending update is sent from a stable snapshot even if the live queue is
  cleared afterward.
- Focused tests, the affected build, strict OpenSpec validation, and the real
  client world-entry check pass.

## Non-goals

- Do not redesign map-region loading or add another queue or worker.
- Do not change the world-entry protocol, cache decoding, persistence, or
  reconnect behavior.

