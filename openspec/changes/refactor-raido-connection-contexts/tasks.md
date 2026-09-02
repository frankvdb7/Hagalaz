## 1. Characterization and context split

- [x] 1.1 Preserve or add characterization coverage for one logical lifecycle across physical disconnect and replacement, then verify the focused Raido tests pass
- [x] 1.2 Split `RaidoConnectionContext` into `RaidoTcpConnectionContext` and `RaidoHubConnectionContext`, moving fields and methods by responsibility while preserving behavior and verifying the context tests pass
- [x] 1.3 Rename `RaidoConnectionContextOptions` and update the builder to construct TCP then Hub contexts, verifying builder and options tests pass

## 2. Infrastructure migration

- [x] 2.1 Update handler, dispatcher, lifetime manager, store, caller context, extensions, and consumers to use the Hub context while keeping the dispatcher contract and lifecycle ordering unchanged
- [x] 2.2 Keep stable heartbeat/features/items on the TCP context, use the handler reader for normal physical input completion, and retain only the required close-request notification on the current physical context; verify replacement and stale-transport tests pass
- [x] 2.3 Remove #488-specific Raido caller/reconnect bridges and unused raw-pipe context APIs, verifying repository searches find no obsolete production callers

## 3. Regression coverage and validation

- [x] 3.1 Adapt existing #477 tests to assert stable identity, protocol ownership, timeout behavior, waiter behavior, physical input completion, stale transport behavior, concurrent replacement, and one logical lifecycle
- [ ] 3.2 Run strict OpenSpec validation, focused Raido tests, the solution build, full solution tests, and cumulative diff review against the #486 baseline

Validation note: strict OpenSpec validation, focused tests, the solution build, and the cumulative diff review passed. The serial full-solution test run reached the changed projects but Docker-dependent integration tests could not start because Docker was unavailable on the host.
