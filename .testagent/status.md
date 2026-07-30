# Test status

The local JSON outbox scenarios were replaced with command publication, EF outbox model, logout lifecycle, lock-lifecycle, consumer-retry, deferred logout-cleanup, and bounded-shutdown regression tests. The Characters consumer now uses exponential MassTransit message retry before faulting. Focused suites, full solution build, EF model validation, and diff validation pass.
