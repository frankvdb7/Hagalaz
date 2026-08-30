# Design

## Existing ownership

`PasswordGrantCommandConsumer` remains the credential-validation owner.
The tokenless authorization consumer calls it with empty scopes and returns
the existing subject and validation outcomes. `AuthenticationService` uses the
existing resilience and rate-limit pipeline and resolves that subject to the
master ID without installing temporary authentication state.

`HandshakeHub` owns reconnect request validation, authentication, existing
connection lookup, session/character ownership verification, response
construction, and response sending. It does not reproduce Raido reconnect
eligibility or lifetime rules.

## Minimal Raido bridge

The ordinary temporary `RaidoConnectionContext` handles the reconnect
request. The existing dispatcher contract remains unchanged. The GameConnection
boundary accepts the temporary caller context and replacement protocol, while
Raido internally resolves the caller's current physical connection.

After the hub sends `WorldReconnectResponse` through
`Clients.Caller.SendAsync`, the boundary stores one private one-shot action on
the temporary context. The handler consumes that action only after
`RaidoProtocolReader.Advance(true)`, and the action invokes the existing
`TryReconnect` publication operation with the captured physical connection and
replacement protocol. No response message, response callback, or raw transport
is passed through the GameWorld boundary.

The existing `TryReconnect(ConnectionContext)` operation remains the
publication and reconnect-lifecycle authority. Its small protocol-aware
extension installs the replacement protocol before publishing the replacement
physical connection and before completing the existing waiter. Its existing
window, timeout, terminal, callback-registration, and concurrent-publication
checks remain authoritative.

The generic write path remains unchanged. GameWorld decides which message to
send; Raido only serializes and flushes it using the current protocol and
physical connection. Existing write/transport failure handling determines
whether the temporary context can still schedule the reconnect.

## Physical lifetime

The temporary handler reports a local successful-transfer outcome from its
deferred action. On success it clears the temporary context through normal
cleanup before running the normal disconnect callback, so that callback cannot
abort the transferred physical connection. Temporary metrics, store removal,
lifetime callbacks, and hub disconnect callbacks still complete.

`RaidoConnectionHandler.ConnectAsync` retains the accepted physical
`ConnectionContext` locally and, only after temporary cleanup completes,
awaits its existing `ConnectionClosed` signal. The stable logical context
owns the active transport through #477 during this wait. On failed publication
the normal temporary abort and cleanup path remains unchanged.

## Scope boundary

No new state machine, candidate subsystem, ownership framework, writer
abstraction, dispatch result, or lifecycle mode is introduced. The only
special handler behavior is the one-shot action required by the existing
reader advance boundary and the local successful-transfer outcome required to
avoid aborting the transferred transport.
