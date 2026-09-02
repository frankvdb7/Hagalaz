## Context

The current `RaidoConnectionContext` combines the stable connection identity, physical transport callbacks, #477 reconnect state, Hub protocol, message writing, timeout policy, caller state, and logical lifecycle bookkeeping. `RaidoConnectionHandler` already creates readers per current physical transport and waits on the existing reconnect window, so the refactor can preserve that control flow while changing the ownership boundaries.

## Goals / Non-Goals

**Goals:**

- Make `RaidoTcpConnectionContext : ConnectionContext` the stable lower owner of physical transport and #477 reconnect state.
- Make `RaidoHubConnectionContext` the public logical owner of protocol, Hub state, writing, and lifecycle-facing state.
- Preserve the existing handler, dispatcher, lifetime manager, store, and reconnect behavior.
- Keep stable features and items separate from per-physical transport features.

**Non-Goals:**

- Implement #478 or any GameWorld reconnect authentication.
- Add cross-context transport transfer, reservations, leases, registries, or handoff APIs.
- Add SignalR message buffering, acknowledgements, or new reconnect semantics.
- Redesign Hub activation, authorization, dispatch, or GameWorld behavior.

## Decisions

### Two contexts, one logical lifecycle

Split the current class into an internal `RaidoTcpConnectionContext` and public `RaidoHubConnectionContext`. The TCP context owns the stable connection ID, stable feature/item collections, current and detached physical connections, physical callbacks, reconnect synchronization, waiter, deadlines, terminal state, and lower abort behavior. The Hub context wraps the TCP context through `ConnectionContext` and owns protocol, caller state, write locking, Hub timeout policy, logical timestamps, activity, and metrics with Hub-level meaning.

The simpler alternative, leaving all fields in one class, preserves behavior but leaves the ownership problem unresolved. A second bootstrap or transfer architecture is not needed for this refactor.

### One options object

Rename `RaidoConnectionContextOptions` to `RaidoHubConnectionContextOptions` and initially keep its four existing settings. The builder passes the same values to the two internal constructors; each context consumes only its own settings. This avoids a new configuration framework or a mechanically pure but unnecessary options split.

### Existing handler and reconnect path

Keep `RaidoConnectionHandler` as the owner of the logical Hub loop. It obtains the current physical transport through internal TCP infrastructure, creates a fresh reader after replacement, and dispatches against the same Hub context. Move the existing persistent-connection activation operation (`TryActivatePersistentConnection(ConnectionContext)`) and its synchronized state to the TCP context without adding a second transition or a cross-context transfer operation.

### Stable versus physical features

The TCP context keeps the stable feature and item collections used for logical connection state. Physical heartbeat and lifetime-notification features are read from the currently attached physical `ConnectionContext` when registering callbacks. The Hub context delegates ordinary connection information through the wrapped stable context and does not expose raw pipes publicly.

### Cancellation ownership

Physical `ConnectionClosed` tokens describe individual sockets. The stable TCP terminal token and `ConnectionClosed` describe the logical lower connection after abort or terminal timeout. The existing Hub-facing `ConnectionAbortedToken` delegates to that stable terminal signal unless characterization tests prove an independently abortable Hub lifetime is already required. No extra cancellation state is added for architectural symmetry.

### #488 separation

Remove caller-to-physical-transport access, response-aware reconnect APIs, protocol-aware reconnect overloads, and other Raido coupling introduced only by the current #488 GameWorld implementation. Update GameWorld and GameUpdate references mechanically for the type rename; do not implement their reconnect behavior here.

## Risks / Trade-offs

- [Risk] Moving fields can accidentally change the logical lifecycle. → Preserve the current handler ordering and assert one lifetime-manager and Hub lifecycle per logical connection.
- [Risk] Stable and physical feature collections can be confused. → Keep stable collections on the TCP context and read physical callback features only from the current physical context.
- [Risk] Mechanical renames can leave a public transport escape hatch. → Search all callers, remove the context-specific raw-pipe API where unused, and keep raw transport access inside Raido infrastructure.

## Migration Plan

Implement the split on the current branch, remove #488-specific Raido coupling, run focused Raido tests, then run the solution build and test suite. There is no data or deployment migration. Rollback is a code rollback.
