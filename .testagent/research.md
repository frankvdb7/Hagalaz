# Test research

Target: EF-backed MassTransit character persistence in GameWorld, the one-way character persistence command consumer, logout cleanup, and per-character lock lifecycle.

Existing conventions: MSTest 4, sealed test classes, async test methods, MassTransit test harnesses, and NSubstitute for isolated collaborators.

Acceptance checklist:

- Persistence publishes a durable one-way command through the scoped bus outbox rather than waiting for a request/response waiter.
- The persistence command includes the character id and snapshot revision and unchanged snapshots are skipped.
- Forced persistence publishes a new snapshot revision.
- The owned EF outbox context contains InboxState, OutboxMessage, and OutboxState and has a migration.
- The character consumer applies the one-way command idempotently by snapshot revision while retaining the legacy request contract.
- Transient command-consumer/database failures are retried before the message is faulted.
- When logout persistence fails, the session is removed but the live character remains available for retry.
- A disconnected character is destroyed only after successful logout.
- Per-character lock entries are removed after the last holder releases them.
- Same-character persistence remains serialized while lock entries are being retired.
- A logout persistence failure is tracked and a later successful periodic flush removes the character, destroys it, clears persistence state, and publishes world signout.
- Shutdown must pass a bounded, host-linked cancellation token to both worker stop and the final durable handoff flush, and must log cancellation as an incomplete handoff.
