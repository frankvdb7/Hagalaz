# Test research

Target: `CharacterPersistenceService` in `Hagalaz.Services.GameWorld`.

Existing conventions: MSTest 4, sealed test classes, async test methods, MassTransit test harnesses, and NSubstitute for isolated collaborators.

Acceptance checklist:

- A failed character update is retried and eventually succeeds.
- A character snapshot request carries the character id and snapshot revision.
