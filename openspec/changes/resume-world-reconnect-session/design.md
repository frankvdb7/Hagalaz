# Design

## Ownership

`SignInUserRequestConsumer` keeps password validation as the authoritative
credential check. A reconnect-only request asks it to prove existing valid
authentication and return the password-validated subject. It never creates or
reads a token in that mode. `AuthenticationService` converts that subject to a
master ID and does not install authentication, session, character, or Contacts
features on the temporary candidate.

`HandshakeHub` performs protocol/version checks, authenticates the candidate,
looks up the existing world session, checks the session claim through
`ExecuteIfOwnerAsync`, and obtains the logical Raido connection by the stable
session connection ID. It does not reproduce Raido's reconnect window or
concurrency rules.

## Raido handoff

The public Raido boundary exposes only logical reconnect operations. The
implementation keeps the physical `ConnectionContext`, stable pipes, input
relay, output relay, and terminal transitions inside `Raido.Server`.

The candidate's physical socket is transferred atomically to the detached
target. The candidate is then terminalized without aborting the transferred
socket. The target keeps its stable ID, features, items, protocol owner, and
logical lifecycle callbacks. The target's existing reconnect window remains
the only eligibility and single-winner authority.

The handoff must stop candidate input ownership before target input ownership
starts. It must also preserve the write boundary around response 15 and the
fresh protocol installation so the target cannot parse a post-response packet
with the candidate handshake protocol or the old ISAAC state.

## Protocol

`WorldReconnectResponse` is a dedicated handshake message with opcode 15 and
the player-entry bit prefix emitted by `DrawStandardMapMessageEncoder` when
`RenderViewport` is true. The shared helper changes no normal map bytes.

`HandshakeProtocol` gains only the special two-byte length framing required by
this response. Normal handshake, lobby, and fresh world responses keep their
current bytes. The reconnect target receives a new revision-specific protocol
scope, seeded from the reconnect request. The old target protocol scope is
disposed once after the new protocol owns the target.

## Failure rules

Any failed credential proof, session lookup, exact ownership check, claim
execution, physical handoff, response write, protocol allocation, or protocol
transition rejects the candidate. A failed candidate remains safe to clean up.
No failure path removes the existing session or character. Candidate cleanup
has no logical GameWorld features, so its unconditional disconnect callback is
side-effect free.
