## ADDED Requirements

### Requirement: Snapshot revisions are authoritative per character

The system SHALL derive each new character snapshot revision from the character's persisted revision carried through hydration and SHALL allocate revisions monotonically per character without using host wall-clock time.

#### Scenario: Snapshot saves across host clock skew
- **WHEN** a character persisted at revision 500 is hydrated on a host whose clock is behind the previous host
- **THEN** the next snapshot is assigned a revision greater than 500 and can be committed

#### Scenario: Process restart preserves ordering
- **WHEN** a new GameWorld process hydrates a character whose persisted revision is 12
- **THEN** its first new snapshot is assigned revision 13 or greater

#### Scenario: Independent characters allocate independently
- **WHEN** snapshots are allocated for two different characters
- **THEN** each character's sequence is monotonic and no global wall-clock sequence is required

### Requirement: Hydration exposes persisted revision

The Characters hydration response SHALL include the persisted snapshot revision, and GameWorld SHALL carry that value through its hydration state and initialize the character persistence state before the character can be flushed.

#### Scenario: Hydration transfers revision
- **WHEN** the Characters service hydrates a character with revision 27
- **THEN** the GameWorld hydration result exposes revision 27 and initializes the producer sequence from it

#### Scenario: Failed registration does not retain revision state
- **WHEN** character registration fails after hydration
- **THEN** GameWorld forgets the initialized persistence state during sign-in cleanup

### Requirement: Exact duplicates and conflicts have distinct outcomes

The persistence consumer SHALL use both snapshot revision and deterministic content fingerprint to classify a message as committed, exact duplicate, or conflict.

#### Scenario: Higher revision commits
- **WHEN** a valid snapshot has a revision greater than the stored revision
- **THEN** the consumer applies the snapshot and fingerprint atomically and publishes `Committed` through the existing outbox transaction

#### Scenario: Exact redelivery is idempotent
- **WHEN** a snapshot has the stored revision and the same non-empty fingerprint
- **THEN** the consumer leaves the character graph unchanged and publishes `Duplicate`

#### Scenario: Obsolete snapshot conflicts
- **WHEN** a snapshot has a lower revision than the stored revision
- **THEN** the consumer leaves the character graph unchanged and publishes `Conflict`

#### Scenario: Equal revision with different content conflicts
- **WHEN** a snapshot has the stored revision but a different or unknown fingerprint
- **THEN** the consumer leaves the character graph unchanged and publishes `Conflict`

### Requirement: Producer persistence state remains honest

GameWorld SHALL move a pending fingerprint to persisted state only after `Committed` or `Duplicate` for the matching revision. A `Conflict` outcome SHALL retain pending state.

#### Scenario: Conflict cannot mark state persisted
- **WHEN** GameWorld receives `Conflict` for its pending revision
- **THEN** the pending fingerprint remains unpersisted and the next flush can issue a newer revision

#### Scenario: Stale acknowledgement cannot clear newer pending state
- **WHEN** a conflict or success outcome arrives for a revision older than the current pending revision
- **THEN** the current pending revision remains unchanged

### Requirement: Logout requires successful persistence confirmation

Pending logout SHALL complete only after the matching snapshot is committed or recognized as an exact duplicate.

#### Scenario: Conflict blocks logout
- **WHEN** a pending logout receives `Conflict` for its snapshot
- **THEN** the character remains pending and no world sign-out completion is published

#### Scenario: Duplicate permits logout
- **WHEN** a pending logout receives `Duplicate` for its matching snapshot
- **THEN** the persistence state is acknowledged and the existing logout completion path may remove and destroy the character

### Requirement: Existing durability and concurrency behavior remains intact

The change SHALL preserve EF optimistic concurrency reset/retry behavior, MassTransit EF outbox atomicity, fault/error-queue behavior, and rollback of acknowledgement when snapshot persistence fails.

#### Scenario: Concurrent commands converge
- **WHEN** two snapshots race for the same character
- **THEN** EF concurrency retry uses fresh state, the highest applicable revision remains stored, and an obsolete command is reported as conflict rather than success

#### Scenario: Failed commit emits no success
- **WHEN** applying a snapshot or its outbox acknowledgement fails before commit
- **THEN** the database snapshot and success acknowledgement are rolled back and the existing retry/fault path remains active
