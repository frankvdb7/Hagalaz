## Context

GameWorld publishes durable character snapshots through the MassTransit EF bus outbox. The current producer uses a process-local wall-clock revision generator, while the Characters consumer gates writes with the persisted `Character.SnapshotRevision` concurrency token. The producer already tracks a content fingerprint and pending acknowledgement in `CharacterPersistenceState`, but the consumer has no durable snapshot identity with which to distinguish exact redelivery from a different stale payload.

The change crosses the shared message contracts, Characters persistence service, GameWorld hydration and persistence lifecycle, and the existing EF schema. Issue #345 already owns active-session uniqueness; this change assumes that ownership and does not add another session/fencing mechanism.

## Goals / Non-Goals

**Goals:**

- Make the database character row the only authoritative revision source.
- Seed per-character in-memory revision allocation from the hydrated persisted revision.
- Persist a deterministic content fingerprint beside the revision for exact duplicate detection.
- Make committed, exact-duplicate, and conflict outcomes explicit and preserve producer/logout state honesty.
- Reuse the existing EF concurrency token, MassTransit outbox, retry, fault, hydration, and pending-state mechanisms.

**Non-Goals:**

- No distributed sequencing service, second revision store, generic conflict framework, or new retry worker.
- No changes to active-session uniqueness, authentication policy, or unrelated session cleanup.
- No attempt to reconstruct fingerprints for historical rows; an empty legacy fingerprint is treated as unknown and cannot produce a successful duplicate outcome.

## Decisions

### Per-character revision ownership

`CharacterPersistenceState` owns the per-character last-issued revision together with pending and persisted fingerprints. Hydration carries `SnapshotRevision` from the Characters service through `GetCharacterResponse`, the hydration saga, `CharacterHydrated`, and `CharacterModel`. Authentication initializes the state after the runtime character is registered; failed sign-in cleanup forgets that state. `NextRevision(masterId)` increments the initialized value and defaults from zero for isolated/test-created characters.

The wall-clock `SnapshotRevisionGenerator` is removed. Keeping a separate generator would leave two owners for ordering and would make restart/migration correctness dependent on coordination between them. Querying the database on every save is also rejected because hydration already crosses the authoritative boundary and the state lock serializes producer allocation for a character.

### Durable exact-duplicate identity

Add a `snapshot_fingerprint` column to `characters`, beside `snapshot_revision`, using the existing character row as the single authoritative persistence record. A shared message-project helper computes SHA-256 over a canonical projection of the persistence DTOs, excluding correlation ID and revision. GameWorld uses the helper for its pending fingerprint and the Characters consumer uses the same helper for comparison and storage.

Comparing the complete relational graph on every stale message is rejected because it would duplicate mapping/normalization rules and require additional reads. The fingerprint is identity metadata, not an ordering or sequencing mechanism.

### Outcome contract and ownership

Add `Committed`, `Duplicate`, and `Conflict` to the existing persistence response/acknowledgement contracts. The outcome field has no successful default: missing or unknown values are non-success and must fail closed. The Characters consumer applies a higher revision and stores its fingerprint before publishing `Committed` through the existing EF outbox. Equal revision plus matching non-empty fingerprint publishes `Duplicate` without applying the graph. Lower revisions, equal revisions with a different fingerprint, and equal revisions with no known stored fingerprint publish `Conflict` without mutation.

`CharacterPersistenceState` only moves a pending fingerprint to persisted for `Committed` or `Duplicate` when both the acknowledgement correlation ID and revision match the pending snapshot. A conflict or acknowledgement for a different snapshot keeps the pending snapshot intact. `CharacterLogoutService` remains the only owner of acknowledgement-to-logout reconciliation: it acknowledges only successful outcomes, then applies the existing completion guard. A conflict therefore cannot complete logout.

The legacy request response receives the same outcome so it cannot report a conflict as successful. Its state-machine success path must emit `CharacterDehydrated` only for `Committed` or `Duplicate`.

### Schema and deployment

Add an EF migration for `snapshot_fingerprint` with a bounded 64-character default for existing rows, update the model snapshot/designer, and update migration-count assertions. Deploy the schema migration before enabling the new producer/consumer behavior. Rows with an empty legacy fingerprint can accept a newer revision, which establishes the fingerprint; an equal legacy revision is a conflict and will be retried as a newer snapshot.

## Risks / Trade-offs

- [Legacy rows have no fingerprint] → Treat equal-revision messages as conflict rather than falsely acknowledging them; the next producer revision establishes identity.
- [An old outbox acknowledgement arrives after restart with the same revision as a replacement payload] → Match state transitions by master ID, correlation ID, and revision; acknowledgements for a different payload cannot clear or persist the replacement pending snapshot.
- [Concurrent commands race on EF concurrency] → Retain `DbUpdateConcurrencyException` reset/retry behavior; a retried obsolete command becomes an explicit conflict and the highest committed snapshot remains authoritative.
- [Message contracts gain a result field] → Keep the field trailing and use the existing response/acknowledgement types; do not introduce a parallel conflict pipeline.

## Migration Plan

1. Create and validate the EF migration adding `snapshot_fingerprint` with an empty default.
2. Deploy the migration before starting the updated Characters and GameWorld services.
3. Allow the first newer snapshot for each legacy character to populate its fingerprint.
4. Rollback, if required, by stopping the updated services and reverting the application/schema migration together; do not run the old producer against a partially reverted contract/schema.

## Open Questions

None. Fingerprint persistence and hydration-seeded per-character allocation are approved decisions for this change.
