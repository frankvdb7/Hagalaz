## Why

PR #492 already moved lifecycle ownership to the game worker, map-region
service, scheduler, stores, and lease reconciliation. The remaining code still
has a few ownership leaks: dimension synchronization is stored in the data
holder, mutation can target detached regions, store enumeration hides locking
and allocation semantics, logout state is mixed into persistence state, and
some compensation paths can destroy objects that a store still owns.

This final pass keeps the existing ownership improvements and makes the
remaining shared relationships explicit at their application/service owner.
It also removes caller-visible destruction flags from domain entities: live
Character state is owned and serialized by the GameWorker, while persistence
receives only detached models.

## What Changes

- Make map-region snapshots explicit at `MapRegionService`; remove expensive
  dictionary-copying properties from `IDimension` and make empty-dimension
  removal enforce its complete invariant atomically.
- Initialize the monotonic persistence revision before claiming the exact
  local character, so admission cannot publish a character before its
  revision baseline exists and failed registration never rolls that baseline
  back.
- Make creature event-handler cleanup one terminal attempt that tries every
  handler, preserves the first failure, and discards dead-creature bookkeeping.
- Remove impossible pending-cleanup filtering, claim-loss flags, and a
  forwarding method from `GameSessionLeaseService`.
- Move all dimension/residency synchronization to one `MapRegionService` gate
  and make dimension creation/removal share that ownership boundary.
- Ensure every mutating region operation uses canonical active ownership;
  expose explicit `GetOrCreateMapRegion` and `FindMapRegion` intents rather
  than caller-controlled create/resume flags. Teardown and read-only paths
  use exact lookup without resurrecting an idle region.
- Remove hidden full snapshots from NPC enumeration, and replace character
  streaming enumeration with explicit snapshots where callers need a stable
  set.
- Keep logout workflow state in `CharacterLogoutService`, leaving persistence
  acknowledgement and revision/fingerprint state responsible to persistence.
- Remove destruction arbitration from `MapRegion`; retain only its existing
  loader-owned ready/discarded visibility state.
- Make failed NPC registration preserve store ownership if exact compensation
  cannot remove the NPC, and remove production helpers that exist only for
  tests.
- Remove `IsDestroyed` from Creature, Character, NPC, MapRegion, GameObject,
  and GroundItem. Replace caller-level lifecycle checks with the owning store,
  region service, or region-part collection.
- Make final logout capture a detached `CharacterModel`, revoke exact store
  ownership, remove region membership, and perform Character cleanup in one
  synchronous GameWorker-owned turn. Retain the snapshot in the existing
  logout record for persistence retry.
- Make Character dehydration synchronous and keep persistence publication
  asynchronous over detached snapshots. Route periodic capture through the
  existing GameWorker scheduler, and reject async command results for stale
  Character instances.
- Treat Raido message dispatch and disconnect as independently overlapping
  scopes; use the CharacterStore/logout admission boundary for input ordering.
- Merge the earlier lifecycle-ownership requirements into this record and
  remove the duplicate lifecycle-ownership change directory.

## Non-goals

- No new transaction, compensation framework, retry mechanism, state machine,
  cache, keyed-lock framework, or generic synchronization abstraction. Logout
  bookkeeping is consolidated into one ordinary dictionary behind the logout
  owner’s gate.
- No change to distributed claim IDs, generation fencing, pending claim or
  abort reconciliation, cancellation behavior, or exact-instance ownership.
- No change to scheduler ownership, map-loader primary-failure behavior,
  NpcService sync/async APIs, `MapRegionPart` update-buffer synchronization,
  distributed session fencing/reconciliation, or independent character
  persistence serialization.
- No unrelated production refactoring or public script API changes.

## Acceptance Criteria

- `IDimension` exposes only dimension identity; region and idle-region reads
  are explicit snapshots owned by `MapRegionService`.
- `FindAllRegions`, per-dimension reads, and background enumeration do not
  clone dictionaries through property getters, and `TryRemoveEmptyDimension`
  checks global-dimension exclusion, exact ownership, and emptiness while
  holding the residency owner lock.
- World admission calls `InitializeRevision` before `AddAsync`; registration
  failure destroys the unregistered character without calling
  `FindByMasterId` or `Forget`. Later failure removes the exact registered
  character, destroys it, and releases the session reservation without
  rolling back the monotonic revision state.
- Creature event cleanup attempts every handler, preserves the first failure,
  and leaves no retry inventory or registration capability on the dead object.
- Lease renewal keeps pending claim cleanup, exact claim IDs, retry
  reconciliation, fencing, pending abort processing, and cancellation while
  removing only impossible branches and forwarding ceremony.
- `MapRegionService` exclusively owns dimension membership and active/idle
  residency transitions under one service-owned synchronization boundary.
- Mutating a map region cannot operate on an idle/detached instance.
- Store APIs do not hide full snapshots behind streaming enumeration, and no
  store reader lock is held across arbitrary caller iteration.
- `CharacterPersistenceState` contains persistence state only; logout
  orchestration owns pending logout workflow state and persistence owns
  acknowledgement delivery.
- Map-region lifecycle state remains visible across threads without a second
  loader arbitration mechanism.
- Domain entities expose no `IsDestroyed` lifecycle flag used by callers to
  coordinate terminal cleanup; exact store, residency, or region-part
  ownership determines whether work may act on an entity.
- Final logout snapshot capture and exact Character ownership revocation have
  no asynchronous gap between them, and persistence never reads a live
  Character after that transition.
- Periodic persistence captures detached models on the GameWorker and applies
  asynchronous command results only to the exact still-owned Character.
- Failed NPC registration cannot knowingly leave a destroyed NPC in its store.
- The PR has one authoritative OpenSpec change record for these lifecycle and
  state-ownership simplifications.

## Impact

Affected areas are GameWorld map residency, world admission, creature event
cleanup, lease renewal, related unit tests, and OpenSpec artifacts. Existing
GameWorld scheduler, loader, NPC ownership, abort reservation, Contacts, and
Raido behavior remain covered by the cumulative PR tests.
