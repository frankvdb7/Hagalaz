# Test plan

1. Replace the local JSON outbox test scenarios with scoped bus-outbox command publication and fingerprint behavior tests.
2. Verify the owned EF outbox model exposes the three MassTransit persistence tables.
3. Preserve and compile the legacy request/response consumer tests while adding the command consumer path.
4. Configure consumer retry handling and exercise a transient command-consumer/database failure.
5. Exercise logout persistence failure with a live Raido context and assert session cleanup without character removal.
6. Exercise `ConnectionHub` success and failure paths and assert destruction occurs only after successful sign-out.
7. Exercise lock-entry churn and same-character contention; assert registry retirement and serialization.
8. Run focused tests, character consumer tests, full build, and diff validation.
9. Track failed logout cleanup and complete it after a later durable persistence flush, with regression coverage for removal and world signout.
10. Bound worker shutdown and final flush with a host-linked deadline, report cancellation, and test a blocked durable handoff.
