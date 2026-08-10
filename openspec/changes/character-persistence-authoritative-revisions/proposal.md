## Why

GameWorld currently assigns character snapshot revisions from process-local wall-clock ticks. Clock rollback, host migration, or process restart can therefore make a valid snapshot appear stale; the Characters consumer then acknowledges it without committing the payload, allowing GameWorld to mark unpersisted state as durable.

## What Changes

- Make the persisted character revision the authoritative ordering source and carry it through character hydration.
- Allocate strictly increasing revisions per character through the existing `CharacterPersistenceState`; remove wall-clock revision generation.
- Persist a deterministic snapshot fingerprint beside the existing revision so exact duplicate delivery can be distinguished from conflicting stale delivery.
- Add explicit `Committed`, `Duplicate`, and `Conflict` outcomes to the existing persistence response/acknowledgement contracts.
- Keep conflicting snapshots uncommitted and prevent them from completing pending logout or persisted-fingerprint transitions.
- Preserve the existing EF concurrency, MassTransit outbox, retry, and fault paths.
- Add the required EF migration, documentation, and unit/integration regression coverage.

## Capabilities

### New Capabilities

- `character-persistence`: Authoritative snapshot ordering, duplicate/conflict handling, and producer acknowledgement semantics.

### Modified Capabilities

- None.

## Impact

- Message contracts in `Hagalaz.Characters.Messages` and hydration state models in GameWorld.
- `CharacterPersistenceState`, the GameWorld persistence and logout services, and the Characters persistence consumer.
- `Hagalaz.Data.Entities.Character`, EF model configuration, migrations, and migration integration assertions.
- Existing Characters/GameWorld MSTest suites and MySQL persistence integration tests.

The change reuses the persisted `SnapshotRevision`, existing hydration pipeline, `CharacterPersistenceState`, EF optimistic concurrency, and MassTransit EF bus outbox. It does not add a distributed sequencing service, parallel revision store, or new retry worker.

## Acceptance Criteria

- A valid snapshot generated after clock skew, rollback, restart, or world migration receives a revision greater than the hydrated persisted revision and can commit.
- The consumer acknowledges an exact duplicate only when both revision and fingerprint match the stored character row.
- An obsolete or fingerprint-conflicting snapshot produces `Conflict`, does not mutate the character graph, and cannot mark GameWorld state persisted.
- Pending logout cannot complete from a conflict outcome and can complete from a committed or exact-duplicate outcome.
- EF concurrency, outbox acknowledgement, retry, fault, and rollback behavior remains correct.
- No wall-clock or second revision source remains active.

## Stop Conditions

- Stop and record follow-up work if implementation requires a second persistence owner, distributed sequencing mechanism, unrelated session/authentication behavior, or a new retry pipeline.
- Stop if existing outbox/concurrency behavior cannot be preserved without broad contract or deployment changes beyond this proposal.
