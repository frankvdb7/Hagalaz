## Why

PR #492 still contains several lower-level state machines and concurrent collections that duplicate ownership already provided by the game worker, map-region service, or stores. This increases lifecycle complexity and leaves phase state vulnerable to being stranded when a callback fails.

## What Changes

- Remove `CreatureUpdateState` and let `GameWorkerService` own tick phase ordering.
- Replace `Dimension`'s concurrent residency dictionaries with ordinary dictionaries protected by one residency synchronization model and snapshot reads.
- Replace the `ContactSessionStore` concurrent dictionary plus lock with one store-owned lock and ordinary dictionary.
- Reduce dead `MapRegion` teardown to external resource cleanup; remove teardown-only collection mutation helpers.
- Remove the redundant active-session lookup from pending-abort coordination.
- Simplify `NpcService` rethrows without changing cleanup behavior.
- Preserve independent character persistence/logout markers and justified scheduler/update-buffer synchronization.
- Add behavior-focused regression tests and update the OpenSpec record.

## Capabilities

### New Capabilities

- `state-ownership-simplification`: Defines single-owner lifecycle, residency, contact-session, and teardown behavior.

### Modified Capabilities

- None.

## Impact

Affected areas are GameWorld creature ticks, map-region residency and teardown, the Contacts session store, pending session-abort coordination, NpcService cleanup, related unit tests, and OpenSpec documentation. No public script API, distributed fencing contract, scheduler ownership model, or MapRegionPart update-buffer lock changes.

