# Design

## Initial classification

`ClientConnectionHandler` reads opcode 14 with the existing
`HandshakeProtocol`, consumes that fixed one-byte message, and sends the same
acknowledgement that `HandshakeHub` previously produced. It then reads the
following authentication request. For reconnect, it consumes those
authentication bytes before passing the same raw `ConnectionContext` to the
reconnect handler. For fresh world and lobby requests, it retains the bytes so
the normal Raido logical handler reads the request exactly once.

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
the existing internal target attach seam. Competing candidates are serialized
by `IGameSessionClaimStore.ExecuteIfOwnerAsync`; after entering that critical
section, the handler re-resolves and validates the current session, claim,
target, character, authentication subject, and detached/reconnectable state.
Only the candidate that passes those checks mutates the target. It installs the
protocol and metadata, flushes response 15, and then performs the one existing
Raido attach operation. No new transport state owner, reservation, callback,
or second transition is introduced.

It receives only the logical Raido target and physical `ConnectionContext`, so
it remains application-neutral. The target logical object, features, items,
handlers, and stable ID remain the owner. The replacement physical ID is left
untouched. `RaidoHubConnectionContext` does not expose physical attachment
publicly, and Raido #477/#488 reconnect behavior remains the single
synchronized attach transition defined by that architecture.

The existing `IGameSessionClaimStore.ExecuteIfOwnerAsync` serializes competing
GameWorld reconnect attempts for the known session claim. Raido's existing
reconnect window and attach lock remain the transport winner mechanism.

## Protocol and ordering

The reconnect handler creates a fresh revision-specific client protocol in its
own scope and seeds it from the reconnect request. Inside the existing session
claim critical section, the selected winner installs that protocol on the
detached target through `SetProtocolAsync`, updates reconnect-specific client
metadata, and sends and flushes response 15 directly through the raw
connection using `HandshakeProtocol`. Response 15 is opcode 15 with declared
`VariableShort` framing and a 4,608-byte player-entry payload. The handler then
performs the existing single Raido attach operation, which publishes the
replacement and resumes the existing logical reader.

The raw input pipe has no Raido reader before attach. A client packet sent
immediately after response 15 therefore stays buffered, then is decoded once
by the existing Raido input pump using the fresh protocol after attach.

No response-aware Raido writer, physical transport writer, candidate registry,
generation, reservation, lease, callback, or second reconnect state machine is
added. If response flushing or the final attach fails after protocol ownership
transfers, the target is aborted and the raw replacement is aborted. This
simple terminal policy avoids leaving a partially transitioned target
reconnectable; protocol rollback is not attempted.

## Authentication

Normal sign-in keeps the token-issuing `SignInUserRequestMessage` contract.
Reconnect uses the dedicated validation message and response. Its raw login,
password, remote address, and physical connection ID are passed in an
explicit `WorldReconnectAuthenticationRequest`, so this pre-Raido path does
not read `IRaidoCallerContextAccessor.Context`. The GameWorld authentication
service maps the validated subject to the master ID, uses the explicit address
or connection ID for rate limiting, and does not mint a token or install
candidate features.

Fresh lobby and world hubs use request-specific validators through DI. The
open-generic default registration can be replaced later by a closed
request-specific registration without editing either caller.
