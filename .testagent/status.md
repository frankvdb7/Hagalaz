# Test status

The local JSON outbox scenarios were replaced with command publication, EF outbox model, logout lifecycle, lock-lifecycle, consumer-retry, deferred logout-cleanup, bounded-shutdown, revocation-ordering, and durable-acknowledgement regression tests. The Characters consumer now uses exponential MassTransit message retry before faulting. Durable acknowledgement keeps producer fingerprints pending until a matching applied-or-already-current revision acknowledgement arrives. The shutdown deadline regression now gates on persistence entry before asserting cancellation; the focused worker suite passes.

## PR #486 reconnect remediation status

Added focused coverage for protocol/output failure provenance, caller cancellation, keep-alive generation and physical-write failures, constructor callback publication, timeout/close contention, and real store membership through reconnect. The focused reconnect/context suite passed with 49 tests, the full Raido.Server.Tests suite passed with 152 tests, and the GameWorld suite passed with 753 tests. Affected Raido and GameWorld builds, the solution build, and strict OpenSpec validation passed; solution output retains only pre-existing warnings.
