## 1. Characterization and context split

- [x] 1.1 Preserve or add characterization coverage for one logical lifecycle across physical disconnect and replacement, then verify the focused Raido tests pass
- [x] 1.2 Split `RaidoConnectionContext` into `RaidoTcpConnectionContext` and `RaidoHubConnectionContext`, moving fields and methods by responsibility while preserving behavior and verifying the context tests pass
- [x] 1.3 Rename `RaidoConnectionContextOptions` and update the builder to construct TCP then Hub contexts, verifying builder and options tests pass

## 2. Infrastructure migration

- [x] 2.1 Update handler, dispatcher, lifetime manager, store, caller context, extensions, and consumers to use the Hub context while keeping the dispatcher contract and lifecycle ordering unchanged
- [x] 2.2 Keep stable heartbeat, infrastructure features, and items on the TCP context, gate physical heartbeat callbacks by active physical identity and pending input-boundary acknowledgement, use the handler reader for normal physical input completion, and retain only the required close-request notification on the current physical context; verify replacement and stale-transport tests pass
- [x] 2.3 Remove #488-specific Raido caller/reconnect bridges and unused raw-pipe context APIs, verifying repository searches find no obsolete production callers
- [x] 2.4 Add stable TCP `Transport` and internal `Application` duplex-pipe ends, and relay physical input/output through them without changing #477 reconnect state or introducing replay buffering
- [x] 2.5 Update the Hub write/keep-alive paths and connection handler to use the stable transport boundary, commit stable output admission before releasing the reconnect lock, linearize physical-input admission against detach while preserving the single input boundary waiter, drop detached output at the lower boundary, preserve physical failures through reconnect expiry, and ensure terminal cleanup quiesces producer owners before completing their pipe ends and awaits the relay tasks

## 3. Regression coverage and validation

- [x] 3.1 Adapt existing #477 tests to assert stable identity, protocol ownership, timeout behavior, waiter behavior, physical input completion, stale transport behavior, concurrent replacement, and one logical lifecycle
- [x] 3.2 Add stable-pipe continuity, detached-output, terminal-pipe-completion, and no-stale-physical-leakage coverage without duplicating the existing #477 lifecycle suite
- [x] 3.3 Run strict OpenSpec validation, focused Raido tests, the solution build, full solution tests, and cumulative diff review against the #486 baseline
- [x] 3.4 Add deterministic coverage for discarding incomplete stable input at a physical replacement boundary while preserving a complete replacement message, keep boundary acknowledgement separate from the public protocol-reader cursor API, cover a read returned before detach and rapid waiter-preserving replacement, and cover startup completion of handler-owned stable input

Validation note: strict OpenSpec validation, focused Raido tests, the Raido and GameWorld test projects, and the solution build passed. The full solution test command ran; non-Docker projects passed, while Docker-dependent integration fixtures could not start because Docker was unavailable on the host.
