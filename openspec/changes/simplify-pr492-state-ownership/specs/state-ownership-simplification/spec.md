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

### Requirement: Mutating region operations require canonical active ownership

Every operation that changes a region MUST obtain canonical active ownership
through `MapRegionService`. `resume: false` MUST remain available only for
read-only access and MUST NOT be used by a mutation path.

#### Scenario: Mutation resumes an idle region

- **WHEN** a collision, object, item, dynamic-region, or teardown operation
  targets an idle region
- **THEN** the service resumes or creates the canonical active region before
  the mutation is applied
- **AND** the mutation is not applied to a detached instance

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

Pending, removed, and completing logout state MUST be owned by
`CharacterLogoutService` through one keyed workflow record. Persistence state
MUST contain only persistence serialization, revision, and acknowledgement
state.

#### Scenario: Logout completion reconciles one keyed record

- **WHEN** logout is tracked, detached, and acknowledged
- **THEN** the logout coordinator uses the exact character record for pending,
  removal, and completion gates
- **AND** persistence acknowledgement remains independent of logout workflow
  bookkeeping

Persistence acknowledgements and final cleanup MUST be identified by the exact
snapshot receipt. Logout cleanup MUST NOT clear revision or persisted-snapshot
bookkeeping after a replacement admission has established a new revision owner
for the same master id.

#### Scenario: Stale logout cleanup cannot clear replacement state

- **WHEN** an old logout receipt is finalized after a replacement revision
  owner has been initialized
- **THEN** the old receipt cleanup is ignored
- **AND** the replacement owner's persistence bookkeeping remains available

### Requirement: MapRegion lifecycle state is visibility-only

`MapRegion` MUST retain cross-thread visibility for ready/discarded state, but
MUST NOT perform a second CAS-based lifecycle arbitration when the loader and
scheduler are the sole transition owner.

#### Scenario: Loader-owned state remains visible to readers

- **WHEN** the loader marks a region ready or discarded
- **THEN** concurrent readers observe the published state
- **AND** `MapRegion` does not compete with the loader by arbitrating a second
  lifecycle transition

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
