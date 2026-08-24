## Context

`RaidoConnectionContext` currently owns a raw Kestrel `ConnectionContext`, its input/output pipes, cancellation token, connection id, caller context, and write lock. `RaidoConnectionHandler` creates one reader for that raw transport and always calls terminal dispatcher/lifetime cleanup when the reader exits. `RaidoConnectionStore` is already the singleton owner used by the lifetime manager for client destinations.

## Goals / Non-Goals

**Goals:**

- Keep one existing `RaidoConnectionContext` as the logical owner and make only its physical transport replaceable.
- Keep the current public connection id as the stable logical destination and expose the current physical id separately.
- Use an explicit lifecycle with `Connected`, `Reconnecting`, and `Closed` states.
- Keep server reconnect support separate from per-logical-connection activation.
- Use the existing store and lifetime manager as the logical owner; do not add a second session store.
- Coordinate transport replacement with the existing write lock and a generation check.
- Use an injectable `TimeProvider` timer for deterministic grace expiry tests.
- Expose a connection feature that lets application/protocol code permanently disable reconnect for the logical connection.

**Non-Goals:**

- Implementing GameWorld opcode 18 handling beyond the separate #476 intent change.
- Authenticating, resuming, or persisting a GameWorld character/session.
- Queuing or replaying sends, duplicate suppression, sequence/ACK protocols, encryption reinitialization, or cross-process recovery.
- Changing the Raido wire protocol or introducing a generic state-machine framework.

## Decisions

1. **Retain the existing logical context.** The original context remains in `RaidoConnectionStore` and owns the stable id, caller context, logical features/items, protocol association, terminal cancellation, and hub lifetime. A replacement contributes only a `RaidoPhysicalTransport` wrapper around its physical transport. It is handed off only after the replacement handler has completed its protocol reader, so two Raido readers never own the same input pipe.

2. **Separate support from activation.** `StatefulReconnectEnabled` declares that the endpoint supports stateful reconnect and makes the feature available, but every logical connection starts with reconnect retention disabled. Application code calls `IRaidoStatefulReconnectFeature.EnableReconnect()` only after the logical session is eligible. A bounded `StatefulReconnectGracePeriod` and `TimeProvider` are carried into each context. GameWorld enables it only after world sign-in; lobby and pre-auth connections therefore remain terminal on transport loss.

3. **Use a small lifecycle, not a framework.** Transport loss transitions `Connected -> Reconnecting`; successful rebind transitions `Reconnecting -> Connected`; expiry, explicit abort, protocol failure, and shutdown transition to `Closed`. All transitions are serialized by the context's lifecycle lock and terminal cleanup is idempotent.

4. **Rebind through the existing connection store.** `RaidoConnectionStore.TryRebindAsync(logicalConnectionId, replacement)` is the application-callable operation. It looks up the retained logical owner and delegates to one context operation. The first successful attempt wins; attempts after `Connected` or `Closed` fail without replacing the active transport.

5. **Make generation ownership explicit.** Each physical transport has a monotonically increasing generation. The message loop captures the generation when it creates a reader and checks it before dispatch. Writes are serialized with the existing write lock and reject detached/stale generations, so no operation silently targets an old transport.

6. **Keep sends failure-only while detached.** A lifetime-manager send that reaches a logical context in `Reconnecting` throws a dedicated reconnecting/unavailable exception. No replay buffer or hidden best-effort success is added.

7. **Let the handler wait across replacement transports.** `RaidoConnectionHandler` recreates the protocol reader for each active generation. A reconnect handshake registers `IRaidoTransportHandoffFeature.OnTransportReady`; the handler completes the current `RaidoProtocolReader` before invoking that callback. A physical reader exit waits for either a successful rebind or terminal closure; only terminal closure invokes dispatcher and lifetime `OnDisconnectedAsync`.

8. **Use a timer callback for expiry.** The context owns one grace timer. Rebind cancels it under the lifecycle lock; a racing callback can only close the context if it still owns the `Reconnecting` state. Tests use a fake `TimeProvider`/timer rather than delay races.

9. **Expose reconnect control through features.** SignalR exposes reconnect capability through connection features and notifies application code after a new transport is ready. Raido exposes `EnableReconnect()`, `DisableReconnect()`, and `OnReconnected(Func<PipeWriter, Task>)` for logical lifecycle consumers, plus the lower transport handoff feature used by a replacement handshake. Disabling while connected makes the next physical loss terminal; disabling during the grace window closes the retained logical connection immediately. The feature does not add negotiation, authentication, buffering, or replay.

## Risks / Trade-offs

- **A replacement must be handed off before starting an independent Raido handler** → The replacement handler completes its protocol reader, then the handoff callback transfers only the lower physical transport wrapper to the retained logical owner; this keeps one dispatcher/read loop owner and prevents two readers on one pipe.
- **The stable id initially matches the first Kestrel id** → Existing callers and client destinations remain compatible while the separate physical id and generation make replacement explicit.
- **A send during the window fails** → This is intentional because replay is outside #477 and silent loss would falsely report delivery.
- **An in-flight handler operation may finish before the rebind wins** → Generation checks prevent work from starting after the transition; no new work is admitted for a stale generation.

## Migration Plan

No migration is required. Stateful reconnect is disabled by default, so existing applications keep immediate disconnect semantics. An application can opt in through Raido options and use the store rebind operation when it owns a validated replacement transport. Rollback is a source revert; no persistent state changes.

## Open Questions

None for this scope. GameWorld's validation and logical-session resume are the explicit follow-up boundary.
