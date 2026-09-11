# Delta: lifecycle ownership

## ADDED Requirements

### Requirement: Domain lifecycle objects remain simple

Creature and MapRegion implementations MUST expose semantic lifecycle state
without owning competing-caller locks, semaphores, leases, fencing tokens, or
destruction state machines.

#### Scenario: Creature destruction becomes terminal before callbacks

- **WHEN** the owning lifecycle service calls `Creature.Destroy()`
- **THEN** `IsDestroyed` is true before region, area, and user cleanup callbacks
  run
- **AND** structural cleanup is attempted before `OnDestroy`
- **AND** the first failure is propagated after later cleanup attempts

#### Scenario: Region destruction follows residency ownership

- **WHEN** `MapRegionService` removes an exact idle region from residency
- **THEN** the owner may call `DestroyAsync()` without a second domain-level
  destruction claim
- **AND** later canonical lookups cannot return that region
- **AND** the region remains terminal if cleanup fails

### Requirement: The map scheduler owns load infrastructure

`MapRegionLoadScheduler` MUST own the load request channel, in-flight
deduplication, completion signaling, and shutdown. `MapRegionService` MUST
request loads through `IMapRegionLoadScheduler` without a second request-sink or
queue abstraction.

#### Scenario: Canonical publication requests one load

- **WHEN** `MapRegionService` publishes a new canonical region
- **THEN** it requests that exact region from the scheduler
- **AND** duplicate requests share one scheduler-owned in-flight operation

#### Scenario: Loader owns canonical validation

- **WHEN** a queued region is stale, discarded, or no longer canonical
- **THEN** `MapRegionLoader` rejects it before applying region data
- **AND** the scheduler does not duplicate residency validation

### Requirement: Failed map loads preserve the primary failure

When map loading fails, the loader MUST mark the region discarded, attempt
best-effort cleanup, exact-remove the failed instance, log secondary cleanup
failures, and rethrow the original load exception or cancellation.

#### Scenario: NPC cleanup failure does not replace the load failure

- **WHEN** loading fails after one or more NPCs were registered
- **AND** one NPC cleanup operation also fails
- **THEN** every registered NPC cleanup is attempted
- **AND** the cleanup failure is logged as secondary
- **AND** the original load exception is rethrown unchanged

### Requirement: NPC removal claims lifecycle ownership

`NpcService` MUST destroy an NPC only after its exact instance has been removed
from `NpcStore` by that caller. A failed or false removal MUST NOT cause a
second caller to destroy the same NPC.

#### Scenario: Concurrent unregister callers have one destruction owner

- **GIVEN** one exact NPC instance is present in `NpcStore`
- **WHEN** two callers unregister that NPC concurrently
- **THEN** only the caller whose store removal succeeds destroys the NPC
- **AND** the other caller leaves the NPC destruction state unchanged

### Requirement: Pending abort processing is store-owned and retryable

`GameSessionStore` MUST keep pending abort reservations and a single
writer-locked processing marker. Begin, release, and complete MUST require the
exact pending session, and a failed attempt MUST release the marker for the
existing lease-cycle retry owner.

#### Scenario: Failed abort processing is available for lease retry

- **GIVEN** a session has a pending abort reservation
- **WHEN** its abort processor releases the processing marker after failure
- **THEN** the reservation remains pending
- **AND** the next lease cycle can claim it again

#### Scenario: A different session cannot alter the marker

- **GIVEN** a pending abort reservation belongs to one session instance
- **WHEN** another session instance uses the same connection identifier
- **THEN** begin, release, and complete operations for the other instance fail
- **AND** the original reservation remains owned by the original session
