## Context

`MapRegionLoadScheduler` loads regions on a background service while
`GameWorkerService` runs synchronous client update ticks. `MapRegionPart`'s
pending updates are written by the loader and game services, sent during the
client update tick, and cleared during the reset tick. The existing
`List<IRegionPartUpdate>` is accessed by all three paths without
synchronization.

## Decision

Use one private lock owned by `MapRegionPart` for pending/prepared buffer
swaps, append, and completion. `PrepareUpdatesForTick` atomically freezes the
pending list for the current tick and starts a new pending list. Every
`SendUpdates(ICharacter)` reads the prepared list and invokes network/script
callbacks outside the lock. `CompleteUpdateTick` clears only the prepared list,
so updates queued after preparation survive for the next tick.

Keep equality-based duplicate filtering within the pending buffer. The two
lists and one lock are the smallest change that preserves the asynchronous
loader boundary while defining a precise client-tick cutoff.

## Risks

The snapshot adds a small allocation only when pending updates are sent. This
is bounded by the existing queue size and avoids holding the lock during
network sends.
