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

`WorldReconnectConnectionHandler` performs version/system-update validation,
dedicated existing-authentication validation, exact world-session lookup, the
existing session-claim check, and exact logical-session/character/auth-subject
matching. It resolves the target by the stable session connection ID.

The handler calls the existing target attach lifecycle with the raw replacement
connection. A single thin `RaidoHubConnectionContext.TryReconnect` wrapper
delegates to the existing `TryAttachPhysicalConnection`; it adds no state,
waiter, transfer, or cleanup behavior. The target logical object, features,
items, handlers, and stable ID remain the owner. The replacement physical ID is
left untouched.

The existing `IGameSessionClaimStore.ExecuteIfOwnerAsync` serializes competing
GameWorld reconnect attempts for the known session claim. Raido's existing
reconnect window and attach lock remain the transport winner mechanism.

## Protocol and ordering

The reconnect handler creates a fresh revision-specific client protocol in its
own scope and seeds it from the reconnect request. It attaches the raw socket,
then installs that protocol on the existing target through the existing
`SetProtocolAsync` API. It sends response 15 directly through the raw
connection using `HandshakeProtocol`; response 15 is opcode 15 with declared
`VariableShort` framing and a 4,608-byte player-entry payload. The client sends
game input only after this response, when the target's fresh protocol is
already installed.

No response-aware Raido writer, physical transport writer, target-owned
reconnect waiter, or candidate cancellation state is added. Failed requests
abort only the raw replacement connection and dispose the untransferred
protocol scope; they do not remove or sign out the existing session.

## Authentication

Normal sign-in keeps the token-issuing `SignInUserRequestMessage` contract.
Reconnect uses the dedicated validation message and response. The GameWorld
authentication service maps the validated subject to the master ID; it does
not mint a token or install candidate features.
