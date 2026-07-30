# Test plan

1. Add a MassTransit harness consumer that faults the first two update requests and responds on the third.
2. Add an always-failing harness consumer and a temporary durable persistence outbox.
3. Exercise `CharacterPersistenceService.PersistAsync` with dehydrated character models.
4. Assert retry success, request metadata, and durable queue creation after broker failure.
5. Exercise logout persistence failure with a live Raido context and assert session cleanup without character removal.
6. Exercise `ConnectionHub` success and failure paths and assert destruction occurs only after successful sign-out.
7. Exercise lock-entry churn and same-character contention; assert registry retirement and serialization.
