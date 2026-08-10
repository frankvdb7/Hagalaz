# Character persistence

Character state is persisted with a durable `PersistCharacterCommand` published
through the GameWorld EF bus outbox. Each command carries a strictly positive
`SnapshotRevision` allocated per character from the persisted revision hydrated
at sign-in. The `characters.snapshot_revision` column is an EF concurrency
token, and `snapshot_fingerprint` identifies the exact content committed at
that revision.

A higher revision is committed with outcome `Committed`. An equal revision is
acknowledged as `Duplicate` only when its fingerprint matches the stored
fingerprint. Lower revisions, equal revisions with different content, and
legacy rows without a fingerprint produce `Conflict`; they do not mutate the
character or move GameWorld state into persisted state.

`Outcome` is required for successful acknowledgement and dehydration paths.
Missing or unknown outcome values are treated as non-success so mixed-version
messages cannot acknowledge a pending snapshot or complete logout. Deploy the
contract-compatible Characters and GameWorld versions together, or keep the
older consumer from handling new persistence commands until it understands the
outcome field.

The character service applies the snapshot and queues
`PersistCharacterAcknowledged` in the same EF transaction. Acknowledgements are
therefore not emitted for a failed database commit. If acknowledgement delivery
fails after commit, redelivery of the command is safe: the stored revision and
fingerprint classify the retry as an exact duplicate and emit the acknowledgement
again.

## Failure behavior

- Transient consumer/database failures receive five exponential in-process
  retries, from one second up to thirty seconds.
- After retries are exhausted, MassTransit publishes a `Fault<PersistCharacterCommand>`
  and moves the original message to the RabbitMQ `UpdateCharacterRequest_error`
  queue. The fault consumer emits a critical structured log and the
  `hagalaz.character.persistence.failures` metric is an alert signal.
- Unhandled or skipped messages use the endpoint dead-letter queue. Operators
  must inspect the error/dead-letter queue, resolve the underlying fault, and
  redrive the original command; redrive is safe because persistence is
  revision-gated and idempotent.
- GameWorld keeps the snapshot pending until the matching acknowledgement. A
  logout is not finalized while that handoff is pending, and periodic/shutdown
  flushes redrive pending commands. The player may remain in the world or have
  their session closed while the save is pending, but the in-memory character is
  retained for retry rather than discarded.

Monitor the applied, duplicate, conflict, failure, unknown-character,
MassTransit fault, and queue-depth signals together. A rising failure or
conflict counter, or a non-empty error queue, requires operator action.
