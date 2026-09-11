## Why

PR #492 already moved lifecycle ownership to the game worker, map-region
service, scheduler, stores, and lease reconciliation. Two successive
simplification records now describe overlapping work, while a few remaining
APIs still hide snapshot allocation, perform check-then-mutate coordination,
or retain rollback state after ownership has been claimed.

This final pass keeps the existing ownership improvements and removes only
the remaining state and control-flow machinery that has no independent owner.

## What Changes

- Make map-region snapshots explicit at `MapRegionService`; remove expensive
  dictionary-copying properties from `IDimension` and make empty-dimension
  removal enforce its complete invariant atomically.
- Claim the exact local character before initializing persistence revision, so
  failed admission never needs to probe or roll back persistence state for an
  unregistered character.
- Make creature event-handler cleanup one terminal attempt that tries every
  handler, preserves the first failure, and discards dead-creature bookkeeping.
- Remove impossible pending-cleanup filtering, claim-loss flags, and a
  forwarding method from `GameSessionLeaseService`.
- Merge the earlier lifecycle-ownership requirements into this record and
  remove the duplicate lifecycle-ownership change directory.

## Non-goals

- No new transaction, compensation framework, retry mechanism, state machine,
  concurrent collection, cache, or synchronization abstraction.
- No change to distributed claim IDs, generation fencing, pending claim or
  abort reconciliation, cancellation behavior, or exact-instance ownership.
- No change to scheduler ownership, map-loader primary-failure behavior,
  NpcService sync/async APIs, `MapRegionPart` update-buffer synchronization,
  or independent character persistence/logout markers.
- No unrelated production refactoring or public script API changes.

## Acceptance Criteria

- `IDimension` exposes only dimension identity; region and idle-region reads
  are explicit snapshots owned by `MapRegionService`.
- `FindAllRegions`, per-dimension reads, and background enumeration do not
  clone dictionaries through property getters, and `TryRemoveEmptyDimension`
  checks global-dimension exclusion, exact ownership, and emptiness while
  holding the residency owner lock.
- World admission calls `AddAsync` before `InitializeRevision`; registration
  failure does not call `FindByMasterId` or mutate/forget persistence state.
  Later failure removes the exact registered character, forgets its owned
  persistence state, destroys it, and releases the session reservation.
- Creature event cleanup attempts every handler, preserves the first failure,
  and leaves no retry inventory or registration capability on the dead object.
- Lease renewal keeps pending claim cleanup, exact claim IDs, retry
  reconciliation, fencing, pending abort processing, and cancellation while
  removing only impossible branches and forwarding ceremony.
- The PR has one authoritative OpenSpec change record for these lifecycle and
  state-ownership simplifications.

## Impact

Affected areas are GameWorld map residency, world admission, creature event
cleanup, lease renewal, related unit tests, and OpenSpec artifacts. Existing
GameWorld scheduler, loader, NPC ownership, abort reservation, Contacts, and
Raido behavior remain covered by the cumulative PR tests.
