# Character persistence

Character state is persisted with a durable `PersistCharacterCommand` published
through the GameWorld EF bus outbox. Each command carries a strictly positive
`SnapshotRevision`. The `characters.snapshot_revision` column is an EF
concurrency token, so a command can only replace the snapshot it read when its
revision is still current. A duplicate or stale revision is a no-op and still
receives an acknowledgement.

The character service applies the snapshot and queues
`PersistCharacterAcknowledged` in the same EF transaction. Acknowledgements are
therefore not emitted for a failed database commit. If acknowledgement delivery
fails after commit, redelivery of the command is safe: the stored revision makes
the retry idempotent and it emits the acknowledgement again.

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

Monitor the applied, duplicate/stale, failure, unknown-character, MassTransit
fault, and queue-depth signals together. A rising failure counter or non-empty
error queue requires operator action.
