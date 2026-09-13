## Purpose

Keep lifecycle and shared-state coordination at the application owner that
already sequences or claims it, while preserving externally observable
cleanup, generation, retry, cancellation, and distributed ownership behavior.

## ADDED Requirements

### Requirement: MapRegionService owns residency coordination

`IDimension` MUST expose identity without owning a synchronization primitive.
`MapRegionService` MUST use one service-owned gate for the dimension registry,
active residency, idle residency, creation, suspension, resumption, removal,
and enumeration. Region construction and loading MUST NOT occur while that
gate is held.

Active-region housekeeping MUST run at the GameWorld worker execution boundary,
after the worker has completed the active-region tick. An independent cleanup
loop MUST NOT change residency while the worker is selecting or ticking active
regions.

#### Scenario: Region enumeration returns an explicit snapshot

- **WHEN** a worker asks for active or idle regions
- **THEN** `MapRegionService` copies the values while holding its residency
  gate
- **AND** caller iteration does not hold the service gate

#### Scenario: Empty dimension removal is atomic

- **WHEN** an exact non-global dimension is requested for removal
- **THEN** the service removes it only if it is still canonical and both
  active and idle residency stores are empty while gated
- **AND** a stale instance or global dimension is rejected

#### Scenario: Housekeeping follows the active gameplay phase

- **WHEN** the game worker is selecting or ticking active regions
- **THEN** region housekeeping does not suspend or destroy a region
- **AND** housekeeping runs only after that worker phase completes

#### Scenario: Detached region destruction does not block the tick

- **WHEN** housekeeping removes an idle region from residency
- **THEN** the exact region is queued for destruction after ownership is
  released
- **AND** the game tick does not await `DestroyAsync`
- **AND** a hosted background worker performs the expensive destruction

### Requirement: Mutating region operations require canonical active ownership

Every operation that changes a region MUST obtain canonical active ownership
through `MapRegionService.GetOrCreateMapRegion`. Read-only and teardown paths
MUST use `FindMapRegion`, which MUST NOT create or resume an idle region.

#### Scenario: Mutation resumes an idle region

- **WHEN** a collision, object, item, or dynamic-region operation
  targets an idle region
- **THEN** the service resumes or creates the canonical active region before
  the mutation is applied
- **AND** the mutation is not applied to a detached instance

#### Scenario: Teardown does not resurrect an idle region

- **WHEN** creature teardown looks up its previous region
- **THEN** it uses the exact existing-region lookup without resuming or
  creating a region

### Requirement: World admission preserves monotonic revision state

`WorldSessionAdmissionService` MUST call `InitializeRevision` before
`CharacterService.AddAsync`. Revision initialization MUST remain monotonic and
MUST NOT be forgotten as rollback for a failed local registration.

#### Scenario: Registration fails after revision initialization

- **WHEN** local character registration returns false or throws
- **THEN** the character is destroyed as unregistered
- **AND** no `FindByMasterId` probe or persistence `Forget` operation occurs

#### Scenario: A post-registration failure removes only the claimed instance

- **WHEN** a later admission step fails after `AddAsync` succeeds
- **THEN** the exact character instance is removed and destroyed before the
  session reservation is released
- **AND** the monotonic revision state is not rolled back

### Requirement: Store collection boundaries are explicit

Store APIs MUST NOT hide full snapshots behind streaming enumeration or hold a
reader lock across arbitrary caller iteration. Character callers that need a
stable set MUST use an explicit snapshot operation; direct lookups MAY hold a
reader lock only for the lookup itself. Unused NPC full-enumeration APIs MUST
be removed.

#### Scenario: Character broadcast uses one stable set

- **WHEN** a service broadcasts to all characters
- **THEN** it obtains an explicit character snapshot before iterating
- **AND** store synchronization is released before caller code runs

### Requirement: Logout workflow state belongs to logout orchestration

Pending and removed logout state MUST be owned by `CharacterLogoutService`
through one ordinary dictionary behind one owner gate. Each record MUST retain
the exact character instance, optional detached snapshot, and optional
persistence receipt. No Creature, Character, or NPC lifecycle flag MAY be used
by callers to coordinate this transition.
Persistence state MUST contain only persistence serialization, revision, pending
receipt matching, and acknowledgement state.

#### Scenario: Logout completion reconciles one keyed record

- **WHEN** logout is requested for an exact character instance
- **THEN** the logout owner claims that instance without silently replacing a
  different instance with the same master id
- **AND** a duplicate claim reuses the same persistence receipt instead of
  submitting a second forced snapshot

#### Scenario: Duplicate logout arrives before a receipt exists

- **WHEN** a second logout observes the same exact character owned by a logout
  that has not created its persistence receipt yet
- **THEN** the second logout reports that logout is already in progress
- **AND** it does not report successful completion or release the ownership

Persistence acknowledgements MUST be delivered to persistence infrastructure
and identified by the exact correlation and snapshot revision. Logout MUST
capture the final snapshot and revoke exact world ownership in one synchronous
GameWorker-owned turn before submitting that detached snapshot, then wait for
that receipt before releasing the session. Normal logout MUST NOT erase
persisted fingerprints or revision allocation state.

#### Scenario: Stale acknowledgement cannot complete another snapshot

- **WHEN** an acknowledgement has the wrong correlation or snapshot revision
- **THEN** the pending receipt remains unresolved and no persisted fingerprint
  is committed

#### Scenario: Character persistence has one pending operation per master

- **WHEN** a normal save is requested while an earlier snapshot for the same
  master is still unacknowledged
- **THEN** the normal save returns without dehydrating or publishing another
  snapshot
- **AND** the original receipt remains the only pending owner

#### Scenario: Forced persistence waits and snapshots current state

- **WHEN** a forced or final save is requested while an earlier snapshot for the
  same master is still unacknowledged
- **THEN** it waits for that exact receipt to resolve
- **AND** it dehydrates the character again after the wait
- **AND** it publishes the final snapshot with a new correlation and revision

#### Scenario: Failed local submission releases the exact pending owner

- **WHEN** publication or the EF outbox submission fails after the snapshot
  receipt has been recorded
- **THEN** the failed receipt no longer blocks persistence for that master
- **AND** the consumed snapshot revision is not rolled back
- **AND** a later attempt can publish a new snapshot with a greater revision

#### Scenario: The first terminal acknowledgement wins

- **WHEN** an exact pending receipt first receives a terminal `Conflict`
- **THEN** the receipt remains `Conflict` when a contradictory acknowledgement
  is delivered later
- **AND** the conflict clears pending ownership without persisting its
  fingerprint
- **AND** a later persistence attempt uses a new receipt and revision

### Requirement: Live Character ownership belongs to the GameWorker

Live Character state MUST be mutated only by work admitted to the serialized
GameWorker boundary. `CharacterStore` MUST admit work only for the exact
currently registered Character, and the logout ownership claim MUST prevent
new work from being admitted for that master ID. Creature, Character, and NPC
MUST NOT expose a lifecycle flag for callers to use as a validity protocol.

#### Scenario: Gameplay already admitted runs before terminal logout

- **WHEN** gameplay is admitted before logout claims the exact Character
- **THEN** the gameplay task runs before the queued terminal transition
- **AND** its effects are present in the final detached snapshot

#### Scenario: Stale gameplay is rejected after ownership revocation

- **WHEN** terminal logout has removed the exact Character from the
  authoritative store
- **THEN** later work for that Character is rejected by the owner
- **AND** a replacement Character with the same master ID cannot receive it

### Requirement: Final logout is one GameWorker-owned handoff

Final logout MUST synchronously capture a detached `CharacterModel`, revoke
exact active Character ownership, remove required world/region membership, and
perform terminal Character cleanup in one GameWorker-owned operation. No await
or externally scheduled asynchronous gap MAY occur between final snapshot
capture and ownership revocation. After revocation, logout persistence MUST
use only the retained snapshot, master/session data, and persistence receipt;
it MUST NOT read the Character or its service scope again.

#### Scenario: Persistence cannot race a later live mutation

- **WHEN** final snapshot capture completes and persistence then blocks
- **THEN** exact Character ownership has already been revoked
- **AND** later gameplay cannot mutate the captured snapshot or stale Character

#### Scenario: Failed persistence retries from the retained snapshot

- **WHEN** durable persistence fails after terminal Character cleanup
- **THEN** a retry reuses the pending logout snapshot or receipt state
- **AND** it does not resurrect, re-add, or reread the Character

### Requirement: Periodic persistence snapshots on the GameWorker

Periodic persistence MUST request detached snapshots through the existing
GameWorker scheduler before starting asynchronous persistence publication. Its
background persistence loop MUST NOT dehydrate or otherwise read live
Character state directly.

#### Scenario: Background flush persists detached models

- **WHEN** periodic flushing begins
- **THEN** the GameWorker performs the dehydration read for each still-owned
  Character
- **AND** the background persistence phase receives `CharacterModel` values
  only

### Requirement: Async results require exact active ownership

Asynchronous command work MUST capture immutable inputs before its await and
MUST apply Character-side results only when the original exact Character is
still the authoritative CharacterStore owner. A result for a stale instance
MUST be dropped, including when a replacement has the same master ID.

#### Scenario: Replacement does not receive a stale continuation

- **WHEN** an async command completes after the original Character was
  removed and a replacement was registered
- **THEN** the result is discarded
- **AND** the replacement is not mutated by the stale continuation

### Requirement: MapRegion lifecycle state is visibility-only

`MapRegion` MUST retain cross-thread visibility for ready/discarded state, but
MUST NOT expose a separate destruction flag or perform lifecycle arbitration
when the loader, scheduler, and `MapRegionService` are the transition owners.

#### Scenario: Loader-owned state remains visible to readers

- **WHEN** the loader marks a region ready or discarded
- **THEN** concurrent readers observe the published state
- **AND** `MapRegion` does not compete with the loader by arbitrating a second
  lifecycle transition

### Requirement: Domain entity removal belongs to the owning collection

Creature, Character, NPC, MapRegion, GameObject, and GroundItem MUST NOT expose
an `IsDestroyed` flag as a caller-visible validity protocol. `CharacterStore`
MUST own Character admission, `MapRegionService` MUST own region residency,
and `MapRegionPart` MUST own GameObject and GroundItem membership. Callers MUST
establish exact ownership at the owning boundary before invoking terminal
cleanup.

#### Scenario: Removed object state is represented by collection ownership

- **WHEN** a GameObject or GroundItem is removed from its owning region part
- **THEN** the owner no longer returns that exact instance from its collection
- **AND** callers do not consult an entity-level `IsDestroyed` flag

#### Scenario: Detached region state is represented by residency ownership

- **WHEN** `MapRegionService` removes an exact region from active or idle
  residency
- **THEN** stale region references cannot remove or replace a newer canonical
  region
- **AND** the detached region does not require an entity-level destruction flag

### Requirement: NPC registration compensation preserves ownership

When synchronous or asynchronous NPC registration fails in `OnRegistered`, the
service MUST attempt exact store removal and MUST destroy the NPC only after
successful removal. If exact removal fails, the original registration
exception MUST be rethrown and the store-owned NPC MUST NOT be destroyed.

#### Scenario: Failed compensation preserves store ownership

- **WHEN** synchronous or asynchronous `OnRegistered` throws and exact store
  removal also fails
- **THEN** the original registration exception is rethrown
- **AND** the NPC is not destroyed while the store may still own it

### Requirement: Cumulative lifecycle ownership remains simple

Creature destruction MUST remain terminal, map loading MUST remain
scheduler-owned, pending abort processing MUST remain store-owned and
retryable, NPC removal MUST remain the exact destruction claim, Contacts MUST
retain generation checks, and justified update-buffer synchronization MUST
remain unchanged.

#### Scenario: Existing lifecycle ownership remains intact

- **WHEN** the cumulative PR lifecycle paths run
- **THEN** exact-instance removal, scheduler completion, primary loader failure,
  NPC ownership, abort retry, Contacts generation, and update-buffer handoff
  behavior remain unchanged
