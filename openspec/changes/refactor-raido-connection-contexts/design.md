## Context

The current `RaidoConnectionContext` combines the stable connection identity, physical transport state, #477 reconnect state, Hub protocol, message writing, timeout policy, caller state, and logical lifecycle bookkeeping. The refactor separates the stable lower TCP boundary from Hub state while preserving the existing logical lifecycle and reconnect authority.

## Goals / Non-Goals

**Goals:**

- Make `RaidoTcpConnectionContext : ConnectionContext` the stable lower owner of physical transport and #477 reconnect state.
- Give `RaidoTcpConnectionContext` stable `Transport` and internal `Application` duplex-pipe ends for its entire logical lifetime.
- Make `RaidoHubConnectionContext` the public logical owner of protocol, Hub state, writing, and lifecycle-facing state.
- Preserve the existing handler, dispatcher, lifetime manager, store, and reconnect behavior.
- Keep stable features and items separate from per-physical transport features.

**Non-Goals:**

- Implement #478 or any GameWorld reconnect authentication.
- Add cross-context transport transfer, reservations, leases, registries, or handoff APIs.
- Add SignalR message buffering, acknowledgements, replay, or disconnected-output buffering; detached output is consumed and dropped.
- Redesign Hub activation, authorization, dispatch, or GameWorld behavior.

### Connections and Hubs layer boundary

Kestrel and `Microsoft.AspNetCore.Connections` provide the physical `ConnectionContext` to Raido's Connections layer. `RaidoTcpConnectionContext` is that layer's stable runtime: it owns stable pipes, features and items, lower lifetime, heartbeat, physical transport attachment, and optional stateful reconnect. Stateful reconnect is one capability of the lower Connections layer, not the identity of the whole layer.

The Raido Hubs layer operates on the stable logical connection through `RaidoHubConnectionContext`, `RaidoHubConnectionHandler`, `RaidoHubConnectionStore`, and `IRaidoHubLifetimeManager`; it owns protocol, Hub dispatch, caller state, and Hub timeout policy. It must not depend on which physical Kestrel transport currently backs the stable connection. GameWorld and GameUpdate consume these Hub/logical-connection semantics and do not own Raido's pipes, physical callbacks, transport-boundary synchronization, or lower reconnect implementation.

## Decisions

### Two contexts, one logical lifecycle

Split the current class into an internal `RaidoTcpConnectionContext` and public `RaidoHubConnectionContext`. The TCP context owns the stable connection ID, stable feature/item collections, current and detached physical connections, connection-state synchronization, waiter, deadlines, terminal state, and lower abort behavior. It separately records whether a physical transport has ever been activated and whether the logical TCP context is terminal; a reconnect waiter exists only during a detached stateful reconnect window. The Hub context wraps the TCP context through `ConnectionContext` and owns protocol, caller state, write locking, Hub timeout policy, logical timestamps, activity, and metrics with Hub-level meaning.

The simpler alternative, leaving all fields in one class, preserves behavior but leaves the ownership problem unresolved. A second bootstrap or transfer architecture is not needed for this refactor.

### One options object

Use the neutral `RaidoConnectionContextOptions` name for the existing four settings. The builder passes the same values to the two internal constructors; each context consumes only its own settings. This avoids a new configuration framework or a mechanically pure but unnecessary options split while keeping the lower TCP dependency neutral.

### Existing handler and reconnect path

Keep `RaidoHubConnectionHandler` as the owner of the logical Hub loop. It reads the stable TCP `Transport.Input` through one logical reader and dispatches against the same Hub context. Physical input is relayed into the stable pipe by the TCP context, so replacement does not create a second reader or require the handler to identify the physical socket. Move the existing persistent-connection activation operation (`TryAttachPhysicalConnection(ConnectionContext)`) and its synchronized state to the TCP context without adding a second transition or a cross-context transfer operation.

When a physical input read detaches, the handler inspects the stable reader's current buffer and dispatches any complete messages already available from the detached socket. It then discards the incomplete tail and acknowledges the raw-TCP input boundary before replacement input is admitted. The physical input relay waits for that acknowledgement, so bytes from the replacement cannot be appended to incomplete bytes from the detached socket. Messages already returned by the reader remain on the normal dispatch path. The public protocol reader retains ordinary pipe cancellation semantics; only the handler uses the internal buffered-message operation for this boundary drain. This is the deliberate raw-TCP divergence from ordinary SignalR transport continuity: a physical socket replacement is a new byte-stream boundary, not a message replay boundary.

### Stable transport/application pipes

Create one duplex-pipe pair when the TCP context is constructed. Expose one end as the stable `Transport` and keep the opposite `Application` end internal to Raido. One long-lived lower input relay follows the currently attached physical transport and copies input into `Application.Output` across activations. A single lower outbound relay consumes `Application.Input`, writes to the currently attached physical output, and consumes/discards bytes while detached so the stable output cannot become a replay queue. Each stable pipe end has one execution owner: the Hub write path owns `Transport.Output`, the physical-input relay owns `Application.Output`, the handler owns `Transport.Input`, and the outbound relay owns `Application.Input`.

The relays are owned by the TCP context, their faults are observed and transition the logical connection to terminal state, and they are cancelled during terminal cleanup. Async cleanup captures and awaits both relay tasks after releasing their cancellation and pipe boundaries, so cleanup returns only after the TCP-owned relay executions and their consumer-end completion have finished. Terminal signalling only cancels pending operations; the Hub write owner completes `Transport.Output` after acquiring the write lock, and the physical-input relay completes `Application.Output` when it exits. The handler completes the stable transport input after its protocol reader is finished, and the outbound relay completes the stable application input when it exits. On recoverable detach, the lower output boundary cancels the pending stable output read, consumes and drops the detached bytes, and only then permits replacement activation to publish. Hub writes and keep-alive writes are admitted to the stable transport output only while an active physical connection is published, using the existing state lock; the admission check, serialization, and stable flush invocation are one operation, and caller cancellation is checked before bytes are advanced. A write invoked while detached is dropped before it can be observed by a replacement. `TryAttachPhysicalConnection` waits synchronously for the existing output boundary; this is an intentional raw-TCP/no-replay divergence because detached output is dropped rather than replayed. Physical input uses the physical close token only to stop reads; bytes already advanced to the stable input invoke an uncancellable stable flush so they cannot remain unflushed until replacement input is copied. The input relay waits for the previous input boundary acknowledgement before reading a replacement, and replacement physical heartbeat callbacks are ignored until that acknowledgement. Physical relay completion reports through the existing current/detached reconnect state using physical reference identity; it does not complete the stable pipes during a recoverable disconnect.

### Stable versus physical features

The physical-input relay snapshots each returned read result before taking the state lock. It validates the physical identity and copies the snapshot into stable input, advances the stable writer, and invokes its uncancellable flush while holding that lock; a stale result is consumed without stable admission. The input-boundary waiter is retained while outstanding, so rapid physical replacement cannot orphan the relay's awaited boundary. The outer logical handler lifecycle completes handler-owned stable input as an idempotent fallback when startup exits before dispatch creates a protocol reader.

The TCP context owns the stable connection infrastructure features (`IConnectionIdFeature`, `IConnectionItemsFeature`, `IConnectionTransportFeature`, `IConnectionLifetimeFeature`, `IConnectionLifetimeNotificationFeature`, `IConnectionHeartbeatFeature`, and `IConnectionInherentKeepAliveFeature`) and preserves other custom/application features from the initial physical context. The inherent-keepalive value is refreshed from the currently activated physical context, while the physical feature object is not copied. The per-transport `IMemoryPoolFeature`, `IConnectionEndPointFeature`, `IConnectionSocketFeature`, `IConnectionMetricsTagsFeature`, and `IConnectionCompleteFeature` are excluded so replacement-specific infrastructure cannot become stable state. The item collection is concurrency-safe because it can be accessed by the logical Hub and physical callbacks concurrently; existing items on the initial physical connection are copied once into it during initial publication, while replacement items are never merged. The stable lifetime-notification feature is the TCP context itself: its close-request token is signaled for an authoritative physical close request or a direct stable `RequestClose()`. Stable `RequestClose()` applies the existing stable abort path directly and does not delegate to a previously captured physical lifetime-notification feature. Stable cancellation observers execute asynchronously and their callback task is observed and logged separately, so a blocking or throwing observer cannot prevent terminal completion. Its token setter is unsupported because the stable context owns the token. During each activation it registers one forwarding callback with the physical heartbeat feature; that callback ticks stable handlers only while its physical context is still active. The TCP context also observes each physical `ConnectionClosed` token directly, alongside the existing close-request notification, so detachment or terminalization does not depend on input-relay progress. Both registrations are locally owned until the activation wins and are transferred or disposed together under the existing lifecycle lock. The relay's identity check remains defensive and idempotent for duplicate or stale close notifications. The Hub context delegates ordinary connection information through the wrapped stable context and does not expose raw pipes publicly.

Keepalive and client-timeout checks use the lower context's `IsActive` capability. The Hub keepalive check also consults the stable lower context's current inherent-keepalive capability, so an active physical transport with its own keepalive suppresses Raido pings and a replacement can change that decision. A detached or terminal connection does not record keepalive writes or advance the last-send timestamp.

When a physical transport fails during a reconnect window, the TCP context retains its exception privately. A successful replacement clears it; reconnect expiry promotes it to the stable terminal exception unless a more specific terminal exception was supplied.

### Cancellation ownership

Physical `ConnectionClosed` tokens describe individual sockets. The stable TCP terminal token and `ConnectionClosed` describe the logical lower connection after abort or terminal timeout. The existing Hub-facing `ConnectionAborted` delegates to that stable terminal signal unless characterization tests prove an independently abortable Hub lifetime is already required. No extra cancellation state is added for architectural symmetry.

### #488 separation

Remove caller-to-physical-transport access, response-aware reconnect APIs, protocol-aware reconnect overloads, and other Raido coupling introduced only by the current #488 GameWorld implementation. Update GameWorld and GameUpdate references mechanically for the type rename; do not implement their reconnect behavior here.

## Risks / Trade-offs

- [Risk] Moving fields can accidentally change the logical lifecycle. → Preserve the current handler ordering and assert one lifetime-manager and Hub lifecycle per logical connection.
- [Risk] Stable and physical feature collections can be confused. → Keep stable collections on the TCP context and read physical callback features only from the current physical context.
- [Risk] Mechanical renames can leave a public transport escape hatch. → Search all callers, remove the context-specific raw-pipe API where unused, and keep raw transport access inside Raido infrastructure.

## Migration Plan

Implement the split on the current branch, remove #488-specific Raido coupling, run focused Raido tests, then run the solution build and test suite. There is no data or deployment migration. Rollback is a code rollback.
