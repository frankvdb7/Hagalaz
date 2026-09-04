# Design

## Ownership

`SignInUserRequestConsumer` keeps the normal token-issuing sign-in contract.
Reconnect uses a dedicated validation request/response and the same password
grant credential check. The reconnect consumer returns only success, the
validated subject, and validation failure flags; it does not mint a token.
`AuthenticationService` converts that subject to a master ID and does not
install authentication, session, character, or Contacts features on the
temporary candidate.

`HandshakeHub` performs protocol/version checks, authenticates the candidate,
looks up the existing world session, checks the session claim through
`ExecuteIfOwnerAsync`, and obtains the logical Raido connection by the stable
session connection ID. It does not reproduce Raido's reconnect window or
concurrency rules, and it does not run fresh world sign-in for reconnect.

## Raido handoff

The target's existing reconnect window and detach/attach lifecycle remain the
only eligibility, ownership, and single-winner authority. The candidate is
terminalized through that existing lifecycle and its current physical socket is
attached to the exact detached target. No candidate/winner state machine or
second registry is introduced. The target keeps its stable logical ID,
features, items, protocol owner, and logical lifecycle callbacks. The
replacement physical `ConnectionContext.ConnectionId` is left untouched and
therefore remains distinct from the logical ID.

The handoff must stop candidate input ownership before target input ownership
starts. It must also preserve the write boundary around response 15 and the
fresh protocol installation so the target cannot parse a post-response packet
with the candidate handshake protocol or the old ISAAC state. The candidate's
abort token is checked before transfer; once the socket is target-owned, the
remaining transition uses no candidate cancellation token.

## Protocol

`HandshakeProtocol` writes the declared `Fixed`, `VariableByte`, or
`VariableShort` framing for every handshake message. Normal handshake, lobby,
and fresh world responses keep their current bytes. `WorldReconnectResponse`
declares `VariableShort`, yielding opcode 15 and the exact 4,608-byte payload.
The reconnect target receives a new revision-specific protocol scope, seeded
from the reconnect request. The old target protocol scope is disposed once
after the new protocol owns the target. The target accepts the replacement,
installs the fresh protocol, writes and flushes response 15, and only then
completes the existing reconnect waiter that releases resumed input.

## Failure rules

Any failed credential proof, session lookup, exact ownership check, claim
execution, physical handoff, response write, protocol allocation, or protocol
transition rejects the candidate. A failed candidate remains safe to clean up.
No failure path removes the existing session or character. Candidate cleanup
has no logical GameWorld features, so its unconditional disconnect callback is
side-effect free. If the target has accepted the physical socket but cannot
finish the protocol/response transition, the target is terminalized rather
than reopening a second handoff path.
