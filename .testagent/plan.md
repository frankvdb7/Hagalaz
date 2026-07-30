# Test plan

1. Replace the local JSON outbox test scenarios with scoped bus-outbox command publication and fingerprint behavior tests.
2. Verify the owned EF outbox model exposes the three MassTransit persistence tables.
3. Preserve and compile the legacy request/response consumer tests while adding the command consumer path.
4. Exercise logout persistence failure with a live Raido context and assert session cleanup without character removal.
5. Exercise `ConnectionHub` success and failure paths and assert destruction occurs only after successful sign-out.
6. Exercise lock-entry churn and same-character contention; assert registry retirement and serialization.
7. Run focused tests, character consumer tests, full build, and diff validation.
