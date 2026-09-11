## Purpose

Keep lifecycle and shared-state coordination at the application owner that
already sequences or claims it, while preserving externally observable
cleanup, generation, retry, cancellation, and distributed ownership behavior.

## ADDED Requirements

### Requirement: Map-region reads and removal are owner operations

`IDimension` MUST expose identity only. `MapRegionService` MUST provide safe,
explicit snapshots for active and idle region enumeration, and
`TryRemoveEmptyDimension` MUST enforce the global-dimension, exact-owner, and
empty-residency invariants atomically under the dimension residency lock.

#### Scenario: Game tick receives one explicit active-region snapshot

- **WHEN** `GameWorkerService` asks for all active regions
- **THEN** `MapRegionService` copies active values while holding each
  dimension residency lock
- **AND** no `IDimension` property performs hidden dictionary cloning

#### Scenario: Empty dimension removal is atomic

- **WHEN** an exact non-global dimension is requested for removal
- **THEN** the service removes it only if active and idle residency are both
  empty while locked
- **AND** a stale instance or global dimension is rejected

### Requirement: World admission orders ownership before persistence

`WorldSessionAdmissionService` MUST claim the exact local character through
`AddAsync` before calling `InitializeRevision`.

#### Scenario: Registration fails before persistence initialization

- **WHEN** local character registration returns false
- **THEN** the character is destroyed as unregistered
- **AND** revision initialization, persistence forgetting, and `FindByMasterId`
  probing are not performed

#### Scenario: A post-registration failure rolls back the owned instance

- **WHEN** a later admission step fails after `AddAsync` succeeds
- **THEN** the exact character instance is removed
- **AND** its admission-owned persistence state is forgotten and the character
  is destroyed before the session reservation is released

### Requirement: Creature event cleanup is terminal

Creature destruction MUST detach its event-handler inventory before attempting
cleanup, attempt every captured handler, preserve the first failure, and make
future handler registration impossible.

#### Scenario: One handler failure does not skip later handlers

- **WHEN** stopping one registered handler throws during destruction
- **THEN** all remaining registered handlers are still attempted
- **AND** the first failure is propagated after the cleanup pass
- **AND** no handler inventory remains for retry

### Requirement: Lease renewal uses store-owned membership

`GameSessionLeaseService` MUST preserve pending claim cleanup, exact claim IDs,
retry reconciliation, distributed fencing, pending abort processing, and
cancellation while omitting membership checks that are impossible under
`GameSessionStore` ownership.

#### Scenario: Pending cleanup is reconciled without active-loop filtering

- **WHEN** a session is moved to pending claim cleanup
- **THEN** it is absent from `FindAll` and remains in the separate cleanup
  reconciliation list
- **AND** lease renewal does not build a redundant pending-session set

#### Scenario: Lost renewal uses the existing reconciliation owner

- **WHEN** renewal returns false or fails with a non-cancellation exception
- **THEN** the lease service logs and invokes the existing abort/reconcile
  coordinator directly
- **AND** cancellation behavior remains unchanged

### Requirement: Cumulative lifecycle ownership remains simple

Creature and MapRegion MUST retain semantic terminal state without competing
caller locks, destruction state machines, or teardown-only internal mutation
APIs. Map loading remains scheduler-owned, NPC removal remains the exact
destruction claim, pending abort processing remains store-owned and retryable,
and justified update-buffer synchronization remains unchanged.

#### Scenario: Existing lifecycle ownership remains intact

- **WHEN** the cumulative PR lifecycle paths run
- **THEN** exact-instance removal, scheduler completion, primary loader failure,
  NPC ownership, abort retry, Contacts generation, and update-buffer handoff
  behavior remain unchanged
