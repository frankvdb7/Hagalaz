## Context

The existing Raido connection context owns stable connection-scoped state but currently reads and writes one permanent transport. Its handler creates one reader for the lifetime of that transport, and its physical callback registrations are tied to the original connection features. The implementation must preserve those existing ownership boundaries while making only the physical transport replaceable. See `proposal.md` and the capability delta for the externally observable contract.

## Goals / Non-Goals

**Goals:**

- Keep the original connection context as the single owner of connection ID, features, items, caller state, protocol, and lifetime.
- Maintain a small synchronized transport/reconnect boundary with one active reconnect waiter per reconnect window.
- Make physical callback and operation races safe using captured `ConnectionContext` reference identity.
- Reuse the existing handler, lifetime manager, store, pipes, heartbeat features, and cancellation primitives.

**Non-Goals:**

- Authentication, session validation, protocol restoration, resynchronization, replay, acknowledgements, or distributed reconnect ownership.
- A logical-session hierarchy, reconnect registry, state machine, generation counter, `IStatefulReconnectFeature`, or generic registration abstraction.
- Changing GameUpdate, the connection store, lifetime management, or existing GameWorld session and protocol behavior.

## Decisions

### Stable state owner and published physical transport

Keep the existing `_connection` as the authoritative connection-scoped state owner and add one nullable current physical `ConnectionContext`. A non-null current transport means it is already published and usable; candidates are registered before publication and are never visible through current-transport access during registration. `Features`, `Items`, `Protocol`, caller state, and connection ID remain delegated to `_connection`; endpoints are read from the captured current physical transport.

### One lock and one waiter per reconnect window

Use the existing BCL lock primitive to protect reconnect eligibility, terminal state, current transport, and the waiter reference. When an active transport detaches, create one waiter for the whole reconnect window. Failed candidates reuse it. A successful publication completes it, and only a later disconnect of that successful transport creates the next waiter. Timeout and publication both arbitrate under the lock; timeout makes the lifetime terminal.

`TryReconnect` captures the current waiter under the lock, registers candidate callbacks outside the lock, then validates the captured waiter identity and candidate state before atomically publishing the candidate. This avoids provisional current transport state and avoids invoking cancellation callbacks while holding the lock. Token registrations are published under the lock and obsolete registrations are disposed outside it. Physical heartbeat features own their callback registrations.

The active reconnect window also retains the identity of its detached physical connection in one `_detachedConnection` field. It is set with the reconnect waiter and detach deadline, and cleared with successful publication or terminal cleanup. A close-request callback from that detached connection remains terminal while the window exists; after a replacement publishes and clears the field, the old callback is stale. `TryReconnect` performs its final validation and detached close-request token check under `_reconnectLock`. That successful validation/claim is the replacement publication linearization point: state is published and the waiter is completed before releasing the lock. The waiter uses `RunContinuationsAsynchronously` so completion cannot run continuations inline under the lock.

### Captured physical identity for operations and callbacks

Every read, write, heartbeat, close, and close-request path captures the physical `ConnectionContext` it uses. After an operation completes or fails, the captured reference is compared with the current transport under the lock. A stale failure is ignored. A current failure follows the same detach/reconnect path as physical close. Detach publishes the null current transport before waking pending read or flush operations, and never cancels the stable terminal-abort token.

The handler only routes deliberately recognized physical read-operation failures to `HandleTransportFailure`: physical `OperationCanceledException` and `IOException` results captured from the underlying physical `PipeReader.ReadAsync`. Because `RaidoProtocolReader` also invokes the protocol parser, it preserves the identity of only those underlying physical-read failures so a parser that throws the same exception type still follows the terminal path. Protocol parsing, protocol validation, malformed or incomplete data, size violations, application exceptions, and other non-transport exceptions propagate to the existing terminal error path. `ObjectDisposedException` is not reconnectable by exception type; it can only be handled as transport loss if a future transport boundary proves that the exception came from the captured physical operation.

The same provenance rule applies to output operations. `WriteCore` and its completion path perform protocol serialization and output metadata access separately from the captured physical `FlushAsync`. Serialization, encoding, and metadata failures use the terminal path, including when their exception type is `IOException` or `OperationCanceledException`. Only exceptions caught directly from the captured physical flush may detach, and caller cancellation propagates without changing Raido state. Keep-alive generation is terminal on failure, while only a failure from the captured physical ping write can detach; stale captured failures remain harmless.

### Fresh readers in the handler

The handler obtains a currently published physical transport before creating a reader. If it observes a detached connection with an active reconnect window, it waits outside the lock instead of exiting or requesting `Input`. It dispatches only the captured transport, then waits outside the lock on the current reconnect-window waiter with the remaining detach-anchored deadline. After successful waiter completion it loops and creates a new reader for the replacement. Lifetime connect/disconnect notifications remain one-per-stable-connection.

### Opt-in configuration

Add the smallest builder and options surface needed to enable reconnect. `RaidoOptionsSetup` supplies a finite bounded default timeout. The capability remains opt-in but has no production caller enabled yet; GameWorld opt-in is deferred to the logical/session work and GameUpdate remains unchanged. No SignalR reconnect feature interface is exposed.

### Terminal and deadline invariants

`Abort()` and the current physical connection's `ConnectionClosedRequested` callback are terminal. A physical detach creates the single reconnect waiter and deadline immediately; failed candidates reuse that window. Successful publication clears transient physical failure state, and timeout permanently closes the logical lifetime. All terminal transitions share the same synchronized state transition and perform registration disposal and physical cancellation after releasing the lock.

Physical detach also stops and clears the per-read client-timeout state before a replacement can run its heartbeat. `_clientTimeoutActive` remains enabled so the replacement receives its client-timeout callback. The timeout lock is acquired before the reconnect lock wherever both are needed; no reconnect-lock-to-timeout-lock acquisition is introduced.

Client-timeout detection only updates elapsed state and decides whether a timeout occurred while holding the timeout lock. It releases that lock before invoking the identity-safe terminal transition, whose registration disposal and physical pipe cancellation remain outside both locks. Initial physical callback registration follows the same local-then-publish ownership rule as replacement registration: synchronous callbacks can detach or terminalize the connection, and unpublished locals are disposed afterward without undoing that transition.

## Risks / Trade-offs

- [Risk] A candidate can close while its callbacks are being registered. → Its callbacks capture the candidate, publication checks the candidate's close tokens, and unsuccessful registration ownership remains with the caller.
- [Risk] A stale write or heartbeat failure can race a successful replacement. → Reference identity is checked under the reconnect lock before any failure can detach or abort the stable connection.
- [Risk] A timeout can race candidate callback registration. → The detach-anchored deadline, waiter identity, and completion state are checked under the same lock; the first terminal/publication decision wins.
- [Risk] A replacement heartbeat can observe stale elapsed read-timeout state. → Detach clears the per-read timeout state under the established timeout-before-reconnect lock order, while retaining the registration-enabled flag for the replacement.
- [Risk] Physical cancellation may leave pending pipe operations in unusual transport implementations. → Detach nulls the current transport first, then uses the transport's existing pending-read and pending-flush cancellation methods without cancelling the stable terminal token.

## Migration Plan

The change is source-compatible for existing builders because reconnect is opt-in. No production caller enables the capability yet, so GameWorld and GameUpdate retain terminal-disconnect behavior. Rollback is a code rollback, with no persisted data or schema migration.
