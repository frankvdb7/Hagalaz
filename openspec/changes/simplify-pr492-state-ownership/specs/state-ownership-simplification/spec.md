## Purpose

Keeps lifecycle and shared-state coordination at the application owner that already sequences or claims it, while preserving externally observable cleanup, generation, and retry behavior.

## ADDED Requirements

### Requirement: Game worker owns creature tick sequencing

The game worker MUST invoke creature tick phases in its established global order, and a creature MUST NOT retain a second per-creature phase state machine for normal tick admission.

#### Scenario: A failed creature tick does not wedge the next tick

- **WHEN** a creature callback throws during one major update
- **AND** the game worker invokes the next valid major update
- **THEN** the creature may execute the next update normally
- **AND** the failure is not represented as a permanently stranded intermediate phase

#### Scenario: Destroyed creatures do no normal tick work

- **WHEN** a destroyed creature receives a later tick callback
- **THEN** it performs no normal content, client update, or reset work

### Requirement: Map-region residency has one synchronization model

Map-region residency MUST preserve exact-instance transitions and canonical uniqueness while reads exposed for enumeration are safe snapshots rather than live mutable collection views.

#### Scenario: Concurrent creation has one canonical region

- **WHEN** concurrent callers request creation of the same region
- **THEN** exactly one region instance is canonical
- **AND** all callers observe that canonical instance

#### Scenario: A residency snapshot is stable during later mutation

- **WHEN** a caller obtains an active or idle region enumeration snapshot
- **AND** residency later changes
- **THEN** enumeration of the earlier snapshot remains safe and represents the earlier view

#### Scenario: Stale idle destruction cannot remove a replacement

- **WHEN** an idle region is replaced or resumed before a stale destruction attempt
- **THEN** the stale attempt does not remove the current exact instance

### Requirement: Contact sessions preserve generation ownership

The contact-session store MUST atomically accept only newer generations, remove only the exact generation and connection, and enumerate a safe snapshot.

#### Scenario: An older generation cannot overwrite a newer session

- **WHEN** an older or equal session generation is submitted after a newer session
- **THEN** the newer session remains stored

#### Scenario: A stale generation cannot remove a newer session

- **WHEN** removal is requested with an older generation or different connection
- **THEN** the current newer session remains stored

### Requirement: Dead map-region teardown releases external resources only

After its exact residency owner removes a region, region teardown MUST mark the region terminal and release required external NPC, item, and object resources without invoking active-region collection mutation semantics or maintaining a second teardown-only collection API.

#### Scenario: External teardown hooks run after residency removal

- **WHEN** an exact idle region is removed from residency and destroyed
- **THEN** registered NPCs are unregistered and item/object destruction hooks run
- **AND** the region is terminal afterward

#### Scenario: Teardown failure does not reopen the region

- **WHEN** an external teardown hook fails
- **THEN** the region remains destroyed
- **AND** a duplicate destruction attempt is rejected

### Requirement: Pending abort ownership is enforced by the session store

Pending abort reservation MUST prevent connection-ID reuse until completion, and the abort coordinator MUST rely on that store-owned invariant rather than performing a duplicate active-session verification.

#### Scenario: A pending abort reserves its connection identifier

- **WHEN** a pending abort exists for a session connection
- **THEN** another session cannot claim that connection identifier until the pending abort completes

#### Scenario: Abort processing remains retryable

- **WHEN** abort processing fails and releases its processing marker
- **THEN** the pending reservation remains available to the existing lease reconciliation owner

### Requirement: Justified synchronization remains local

The map-region scheduler state and `MapRegionPart` update buffers MUST retain their existing synchronization because those components own the shared mutable resources they protect.

#### Scenario: Region updates remain safe across asynchronous producers and worker phases

- **WHEN** a region update is queued while the worker prepares, sends, or resets updates
- **THEN** pending and prepared update buffers retain their existing safe handoff behavior

