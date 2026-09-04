# Design

## Initial classification

`ClientConnectionHandler` reads the first raw handshake message with the
existing `HandshakeProtocol`. For reconnect, it consumes the handshake bytes
before passing the same raw `ConnectionContext` to the reconnect handler. For
fresh world and lobby requests, it retains the bytes so the normal Raido
logical handler reads the request again.

The normal factory is called only after classification. It receives
`statefulReconnect: true` for `WorldSignInRequest` and false for lobby and all
other requests. The normal `HandshakeHub` therefore keeps its existing fresh
world and lobby behavior and does not enable reconnect at runtime.

## Reconnect ownership

`WorldReconnectConnectionHandler` receives an
`IHandshakeValidator<WorldReconnectRequest>` for revision and system-update
policy. It performs dedicated existing-authentication validation, exact
world-session lookup, the existing session-claim check, and exact
logical-session/character/auth-subject matching. It resolves the target by the
stable session connection ID. Reconnect failure mapping and target ownership
checking remain private to this handler because neither is handshake policy.

The handler asks `RaidoHubConnectionHandler` to activate the raw replacement
connection on the resolved target. The handler owns this operation and calls
the existing internal target attach seam. It receives only the logical Raido
target and physical `ConnectionContext`, so it remains application-neutral.
The target logical object, features, items, handlers, and stable ID remain the
owner. The replacement physical ID is left untouched. `RaidoHubConnectionContext`
does not expose physical attachment publicly, and Raido #477/#488 reconnect
behavior remains unchanged.

The existing `IGameSessionClaimStore.ExecuteIfOwnerAsync` serializes competing
GameWorld reconnect attempts for the known session claim. Raido's existing
reconnect window and attach lock remain the transport winner mechanism.

## Protocol and ordering

The reconnect handler creates a fresh revision-specific client protocol in its
own scope and seeds it from the reconnect request. It installs that protocol
on the detached target through `SetProtocolAsync`, sends and flushes response
15 directly through the raw connection using `HandshakeProtocol`, then asks
Raido connection infrastructure to activate the physical connection. Response
15 is opcode 15 with declared `VariableShort` framing and a 4,608-byte
player-entry payload.

The raw input pipe has no Raido reader before attach. A client packet sent
immediately after response 15 therefore stays buffered, then is decoded once
by the existing Raido input pump using the fresh protocol after attach.

No response-aware Raido writer, physical transport writer, target-owned
reconnect waiter, or candidate cancellation state is added. If response
flushing or attach fails after protocol ownership transfers, the target retains
that scope under the existing `SetProtocolAsync` lifetime rules, the raw
replacement is aborted, and the detached target remains subject to its normal
Raido reconnect timeout. No two-phase Raido transaction is added.

## Authentication

Normal sign-in keeps the token-issuing `SignInUserRequestMessage` contract.
Reconnect uses the dedicated validation message and response. The GameWorld
authentication service maps the validated subject to the master ID; it does
not mint a token or install candidate features.

Fresh lobby and world hubs use request-specific validators through DI. The
open-generic default registration can be replaced later by a closed
request-specific registration without editing either caller.
