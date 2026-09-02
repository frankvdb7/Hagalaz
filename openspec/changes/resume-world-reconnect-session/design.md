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

The ordinary temporary `RaidoConnectionContext` handles the reconnect request.
After the hub sends `WorldReconnectResponse` through
`Clients.Caller.SendAsync`, the existing `TryReconnect` publication operation
is invoked through the GameConnection boundary. Raido resolves the caller's
current physical connection internally and passes it to the existing
publication logic with the replacement protocol. No response message,
response callback, or raw transport is passed through the GameWorld boundary.

The existing `TryReconnect(ConnectionContext)` operation remains the
publication and reconnect-lifecycle authority. Its small protocol-aware
extension installs the replacement protocol before publishing the replacement
physical connection and before completing the existing waiter. Its existing
window, timeout, terminal, callback-registration, and concurrent-publication
checks remain authoritative.

The generic write path remains unchanged. GameWorld decides which message to
send; Raido only serializes and flushes it using the current protocol and
physical connection. Existing write/transport failure handling remains
authoritative.

## Physical lifetime

The temporary handler follows the normal Raido lifecycle after the reconnect
request. The existing #477 stateful reconnect operation remains responsible
for replacement transport publication, logical lifetime, timeout, terminal
handling, and waiter completion. No additional transfer or accepted-transport
lifetime mechanism is added.

## Scope boundary

No new state machine, candidate subsystem, ownership framework, writer
abstraction, dispatch result, or lifecycle mode is introduced. The dispatcher
and handler remain unchanged; the integration only feeds the temporary
physical connection and replacement protocol into the existing reconnect
publication operation.
