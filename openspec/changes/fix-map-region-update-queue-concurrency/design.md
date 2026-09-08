## Context

`MapRegionLoadScheduler` loads regions on a background service while
`GameWorkerService` runs synchronous client update ticks. `MapRegionPart`'s
pending updates are written by the loader and game services, sent during the
client update tick, and cleared during the reset tick. The existing
`List<IRegionPartUpdate>` is accessed by all three paths without
synchronization.

## Decision

Use one private lock owned by `MapRegionPart` for queue inspection, append,
snapshot, and clear operations. `SendUpdates(ICharacter)` copies the queue
under that lock and sends the snapshot outside the lock, so network/script
callbacks do not hold the queue lock.

Keep the existing `List<IRegionPartUpdate>` and equality-based duplicate
filtering. The lock is the smallest change that preserves the current data
structure and update semantics while covering the asynchronous loader boundary.

## Risks

The snapshot adds a small allocation only when pending updates are sent. This
is bounded by the existing queue size and avoids holding the lock during
network sends.

