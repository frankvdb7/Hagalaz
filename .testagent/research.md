# Test research

Target: logout cleanup in `AuthenticationService` and `ConnectionHub`, plus the existing `CharacterPersistenceService` regression coverage.

Existing conventions: MSTest 4, sealed test classes, async test methods, MassTransit test harnesses, and NSubstitute for isolated collaborators.

Acceptance checklist:

- A failed character update is retried and eventually succeeds.
- A character snapshot request carries the character id and snapshot revision.
- If all broker update attempts fail, the snapshot is durably queued for replay.
- Outbox entries can be read and replayed without relying on in-memory fingerprint state.
- When logout persistence fails, the session is removed but the live character remains available for retry.
- A disconnected character is destroyed only after successful logout.
- Per-character lock entries are removed after the last holder releases them.
- Same-character persistence remains serialized while lock entries are being retired.
