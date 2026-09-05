# Design

## Dispatcher boundary

`RaidoConnectionDispatcher` owns each accepted physical `ConnectionContext`,
creates one application scope, resolves the scoped `RaidoConnectionDelegate`,
and invokes it with a per-connection `RaidoConnectionDispatchContext`. The
dispatch context has an internal constructor and privately owns the accepted
physical connection, the logical-context factory, and the logical Hub handler.
It exposes only `DispatchNewAsync` and `DispatchExistingAsync`; it does not
expose the physical connection or a reconnect-state query.

`ClientConnectionHandler` reads opcode 14 with the existing
`HandshakeProtocol`, consumes that fixed one-byte message, and sends the same
acknowledgement that `HandshakeHub` previously produced. It then reads the
following authentication request. Reconnect consumes those authentication bytes
before Raido logical creation. Fresh world and lobby retain their bytes so the
normal logical handler reads each request exactly once.

`DispatchNewAsync` creates the logical context through the existing factory and
awaits `RaidoHubConnectionHandler.ConnectAsync`, keeping the application scope
alive for the logical connection. Fresh world receives `statefulReconnect: true`;
lobby receives false.

## Reconnect ownership

`WorldReconnectConnectionHandler` receives an
`IHandshakeValidator<WorldReconnectRequest>` for revision and system-update
policy. It performs dedicated existing-authentication validation, exact
world-session lookup, the existing session-claim check, and exact
logical-session/character/auth-subject matching. It resolves the target by the
stable session connection ID.

Inside `IGameSessionClaimStore.ExecuteIfOwnerAsync`, the handler re-resolves
and validates the current session, claim, target, character, and subject, then
calls `DispatchExistingAsync`. Raido first verifies under its existing state
lock that the target is awaiting reconnect: reconnect is enabled, the target is
not terminal or disposed, no physical connection is current, a live reconnect
waiter and detached physical connection exist, and the reconnect window has not
expired. Only after this preflight does the context invoke GameWorld's
preparation callback. The callback installs the fresh protocol, updates client
metadata, and flushes response 15. The context then performs the existing
internal `TryAttachPhysicalConnection` before returning, so the GameSession
claim remains held through attachment. A preparation exception aborts only the
replacement physical connection; GameWorld tracks whether `SetProtocolAsync`
committed and aborts the logical target only when preparation fails after that
mutation point. A final attach failure remains terminal for the target.

There is no connection-selection result DTO, GameWorld reconnect marker,
`Items`-based coordination, reservation, lease, or second reconnect state. The
final internal attach is the authoritative winner operation.

## Protocol and ordering

The reconnect handler creates a fresh revision-specific client protocol in its
own async scope and seeds it from the reconnect request. Ownership of that scope
transfers to the existing target when `SetProtocolAsync` is called; the handler
does not dispose it afterward. Response 15 is opcode 15 with declared
`VariableShort` framing and a 4,608-byte player-entry payload.

The raw input pipe has no Raido reader before attach. A client packet sent
immediately after response 15 therefore stays buffered, then is decoded once by
the existing Raido input pump using the fresh protocol after attach.

If preparation fails before the protocol transition commits, the target remains
unchanged and reconnectable while the dispatch context aborts the replacement
physical connection. If preparation fails after the transition commits,
GameWorld aborts the partially transitioned target; no protocol rollback is
attempted. A clean preflight rejection returns false before the preparation
callback. A final attach failure throws an infrastructure
connection-aborted exception and terminalizes both sides.

## Application scope and authentication

The dispatcher does not constructor-inject a scoped application handler. It
creates and disposes one async scope per accepted physical connection. The
scope remains alive through a new logical Hub lifecycle and ends after an
existing reconnect has internally attached. The separately created reconnect
protocol scope transfers to the existing target on successful protocol
replacement. `ClientConnectionHandler` keeps the handshake protocol in that
accepted-connection scope but resolves `WorldReconnectConnectionHandler` from
the same scoped provider only after reconnect classification, so fresh and
lobby connections do not retain the reconnect-only dependency graph.

The outer handshake timeout remains owned by `ClientConnectionHandler` and is
passed through `ExecuteIfOwnerAsync`; the claim callback continues to pass its
token through preparation, response flushing, and final attach.

Normal sign-in keeps the token-issuing `SignInUserRequestMessage` contract.
Reconnect uses the dedicated validation message and response. Its raw login,
password, remote address, and physical connection ID are passed in an explicit
`WorldReconnectAuthenticationRequest`, so this pre-Raido path does not read
`IRaidoCallerContextAccessor.Context`.
