## Context

`RaidoConnectionContext` owns the stable logical id, application pipes, cancellation token, caller context, and write lock. `RaidoPhysicalConnectionSession` owns one raw Kestrel `ConnectionContext` and pumps it into the application pipes. `RaidoConnectionHandler` creates one reader for the stable application input and keeps it alive across physical replacement. `RaidoConnectionStore` is already the singleton owner used by the lifetime manager for client destinations.

## Goals / Non-Goals

**Goals:**

- Keep one existing `RaidoConnectionContext` as the logical owner and make only its physical transport replaceable.
- Keep the current public connection id as the stable logical destination and expose the current physical id separately.
- Use an explicit lifecycle with `Connected`, `Reconnecting`, and `Closed` states.
- Keep server reconnect support separate from per-logical-connection activation.
- Use the existing store and lifetime manager as the logical owner; do not add a second session store.
- Coordinate transport replacement with the existing write lock and generation-scoped pump ownership.
- Use an injectable `TimeProvider` timer for deterministic grace expiry tests.
- Expose a connection feature that lets application/protocol code permanently disable reconnect for the logical connection.

**Non-Goals:**

- Implementing GameWorld opcode 18 handling beyond the separate #476 intent change.
- Authenticating, resuming, or persisting a GameWorld character/session.
- Queuing or replaying sends, duplicate suppression, sequence/ACK protocols, encryption reinitialization, or cross-process recovery.
- Changing the Raido wire protocol or introducing a generic state-machine framework.

## Decisions

1. **Separate stable application pipes from physical sessions.** `RaidoConnectionContext` owns one `RaidoApplicationConnection` for the logical lifetime. `RaidoPhysicalConnectionSession` owns exactly one raw Kestrel `ConnectionContext` and pumps bytes between that session and the stable application pipes. The original logical handler reads the stable pipe and remains alive while physical sessions are replaced; no raw transport is detached from one handler and donated to another.

2. **Separate support from activation.** `StatefulReconnectEnabled` declares that the endpoint supports stateful reconnect and makes the feature available, but every logical connection starts with reconnect retention disabled. Application code calls `IRaidoStatefulReconnectFeature.EnableReconnect()` only after the logical session is eligible. A bounded `StatefulReconnectGracePeriod` and `TimeProvider` are carried into each context. GameWorld enables it only after world sign-in; lobby and pre-auth connections therefore remain terminal on transport loss.

3. **Use a small lifecycle, not a framework.** Transport loss transitions `Connected -> Reconnecting`; successful rebind transitions `Reconnecting -> Connected`; expiry, explicit abort, protocol failure, and shutdown transition to `Closed`. All transitions are serialized by the context's lifecycle lock and terminal cleanup is idempotent.

4. **Rebind through a prepare/commit reservation.** `RaidoConnectionStore.TryPrepareRebindAsync(logicalConnectionId, replacement)` is the application-callable prepare operation. It validates the reconnecting logical owner and reserves the replacement physical session, returning a reservation rather than reporting success before the transfer can commit. The physical handler commits the reservation only after it has quiesced the replacement input and captured its unread bytes. The first valid reservation wins; expiry, abort, or a failed commit invalidates it.

5. **Make transfer ownership and ordering explicit.** Each physical pump pair has its own cancellation source, input/output tasks, and stopped completion. A replacement first reserves the reconnecting logical context and its physical session. The session then stops the source generation, waits for the target's previous generation, captures the replacement protocol reader's complete unread suffix, commits the target session, and uses one output barrier for deferred committed work. The barrier is installed while the target write semaphore is held, so normal writes and pings either finish admission before the barrier or wait behind it. Before emitting reconnect output, the target's uncertain pre-loss output is discarded; it is not replayable without ACK or sequence semantics. The response is then flushed first, a live physical consumer drains the target output while post-commit work runs, and only after resynchronization finishes are the suffix installed, target pumps started, and normal output admitted. This establishes `source quiesced -> suffix captured -> target committed/barrier installed -> uncertain output discarded -> response flushed -> post-commit work consumed/flushed -> suffix installed -> target pumps resumed -> normal output admitted`; later bytes from the replacement socket cannot overtake the suffix. Invalidating a prepared reservation aborts its temporary replacement connection, so it cannot fall back to ordinary handshake processing. A failed commit invalidates the reservation and aborts or restores the source according to the commit boundary.

6. **Keep sends failure-only while detached.** A lifetime-manager send that reaches a logical context in `Reconnecting` throws a dedicated reconnecting/unavailable exception. No replay buffer or hidden best-effort success is added.

7. **Keep protocol-reader completion with its owner.** `RaidoConnectionHandler` owns the one stable application `RaidoProtocolReader`. `RaidoProtocolReader.DisposeAsync` only releases the wrapper; the handler explicitly completes the underlying reader in its final `finally`. During a successful replacement, the temporary handshake handler passes an independent copy of its unread suffix to the transfer, advances to the end exactly once, and completes only that temporary application after the physical session has accepted the target. The original logical handler continues on its stable reader and application pipes.

8. **Use a timer callback for expiry.** The context owns one grace timer. Rebind cancels it under the lifecycle lock; a racing callback can only close the context if it still owns the `Reconnecting` state. Tests use a fake `TimeProvider`/timer rather than delay races.

9. **Expose reconnect control through features.** SignalR exposes reconnect capability through connection features and notifies application code after a new transport is ready. Raido exposes `EnableReconnect()`, `DisableReconnect()`, and persistent `OnReconnected(Func<PipeWriter, Task>)` registration for logical lifecycle consumers. Disabling is a permanent veto for that logical connection; disabling during the grace window closes it immediately. The feature does not add negotiation, authentication, buffering, or replay.

10. **Keep physical plumbing inside Raido.** `RaidoApplicationConnection` and `RaidoPhysicalConnectionSession` are internal implementation owners. The public builder does not expose a physical-session injection method or feature; Raido creates the application/session pair at the transport boundary. The common split is still used when reconnect is disabled because it preserves one transport ownership and terminal cleanup path, while the logical retention branch remains opt-in.

11. **Compose post-reconnect callbacks.** `OnReconnected` registrations are retained for the logical lifetime, snapshotted at each successful commit, and awaited in registration order. A callback failure aborts the logical connection but does not prevent later registered callbacks from being attempted.

12. **Keep the reconnect barrier phase-specific.** `RaidoRebindReservation` exposes only the two operations needed by the handshake boundary: committed response work and post-commit resynchronization work. During either phase, registered work may write through the target while normal logical writes wait. The existing callback-compatible execution boundary is retained narrowly because GameWorld's resynchronization reaches the connection through its established synchronous session/lifetime-manager proxy; changing every character update API to carry a reconnect writer would be a broader ownership change. Normal writes and pings still acquire the target write semaphore before checking the barrier. The barrier is released only after the response/resynchronization output and replacement suffix have been ordered; no second transport queue, replay path, or generic transaction coordinator is introduced.

## Risks / Trade-offs

- **A replacement has its own temporary handler** → The temporary handler owns only the replacement application's pipes until the handshake dispatch reserves a transfer. The transfer waits for its physical pumps and application reader ownership to stop before committing, so the stable logical handler and physical pumps never concurrently read the same pipe.
- **The stable id initially matches the first Kestrel id** → Existing callers and client destinations remain compatible while the separate physical id makes replacement explicit.
- **A send during the window fails** → This is intentional because replay is outside #477 and silent loss would falsely report delivery.
- **An in-flight handler operation may finish before the rebind wins** → The stable application reader remains the one dispatch owner; unread bytes are captured at the transfer boundary and no old physical pump remains active after commit.

## Migration Plan

No migration is required. Stateful reconnect is disabled by default, so existing applications keep immediate disconnect semantics. An application can opt in through Raido options and use the store rebind operation when it owns a validated replacement transport. Rollback is a source revert; no persistent state changes.

## Open Questions

None for this scope. GameWorld's validation and logical-session resume are the explicit follow-up boundary.
