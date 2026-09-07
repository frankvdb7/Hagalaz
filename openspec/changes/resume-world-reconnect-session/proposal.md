# Resume the existing world session on reconnect

## Why

Revision 742 uses opcode 16 for both fresh world login and world reconnect.
The reconnect flag must be classified before Raido creates a new logical
connection so a valid reconnect can resume the already-running GameWorld
session and character.

## What changes

- Keep dedicated reconnect-only authorization validation separate from normal
  token-issuing sign-in.
- Process opcode 14 and its acknowledgement, then cheaply classify the
  following authentication request before creating a logical Raido context.
  Opcode 19 waits for its complete declared authentication frame but is
  classified without full decoding. Opcode 16 is inspected only through
  packet framing/header data and its reconnect flag; only flag 1 is fully
  decoded. Fresh world and lobby requests continue through the normal factory
  path; only fresh world login enables Raido stateful reconnect, and both
  retain their raw authentication bytes for the logical reader. The outer
  handshake timeout remains active while the lobby frame is incomplete.
- For reconnect, validate the existing world session, claim, logical
  connection, character, and authentication subject inside the existing claim
  critical section.
- Let `RaidoConnectionDispatcher` own the accepted physical connection, create
  one application scope, and invoke a scoped `RaidoConnectionDelegate` with a
  per-connection `RaidoConnectionDispatchContext`.
- Use the context's high-level existing-dispatch continuation: Raido performs
  its internal awaiting-reconnect preflight, GameWorld prepares the protocol,
  metadata, and response 15, and Raido performs the existing internal attach
  before the call returns.
- Preserve the outer handshake timeout through the existing session claim. A
  preparation cancellation before protocol commit leaves the logical target
  reconnectable; a failure after protocol commit is terminalized by GameWorld.
- Resolve the reconnect handler lazily from the accepted physical connection's
  scoped provider after reconnect classification, while retaining the scoped
  handshake protocol for the connection lifetime. Create the reconnect
  protocol scope only after the validated session target is ready for
  preparation.
- Provide one shared injectable handshake policy for lobby, fresh-world, and
  reconnect requests.
- Hide the Raido physical dispatcher behind a public listener-composition
  extension, leaving the dispatcher implementation internal.

## Non-goals

- Do not add authentication opcode 18, a reconnect registry, a second session
  claim, replay, snapshots, or resynchronization.
- Do not call fresh world sign-in for reconnect or repeat character hydration,
  registration, Contacts publication, or world sign-in publication.
- Do not change lobby or flag-0 fresh-login decoding, routing, or response
  behavior beyond keeping the pre-logical timeout active until lobby framing
  is complete.
- Do not modify the existing #477/#488 Raido reconnect state machine. No
  connection-selection result DTO, GameWorld reconnect marker, `Items`-based
  coordination, public physical attach API, public reconnect-state query,
  reservation, lease, handoff, or second reconnect transition is added.

## Acceptance criteria

- Opcode 14 is acknowledged before the following authentication request is
  classified. Opcode 16 with reconnect flag 1 is handled before logical Raido
  context creation and never creates a temporary candidate context.
- Valid credentials can identify only an already-authenticated subject that
  matches the existing world session and character.
- Existing-token infrastructure failures propagate as request faults instead
  of being converted into an ordinary unsuccessful authentication response;
  cancellation propagates and internal authorization requests receive the
  consumer cancellation token.
- A successful reconnect preserves the existing logical connection ID,
  GameSession, claim, character reference, registration, and handlers. The raw
  replacement physical connection ID is never rewritten.
- The existing GameWorld session claim serializes reconnect candidates. Each
  candidate revalidates the current session, claim, target, character, subject,
  and Raido awaiting-reconnect preflight inside that claim before changing
  target state. Invalid, stale, duplicate, concurrent, missing, and lost-claim
  requests fail without disturbing resumed state.
- Response 15 uses plain handshake framing with a two-byte payload length and
  the exact 4,608-byte player-entry payload. The winner installs
  protocol/metadata and flushes response 15 while the physical connection is
  still raw, then performs one existing Raido attach. The first immediate game
  packet remains buffered until that attach starts Raido's existing reader.
- If the final attach fails after target mutation, the replacement is aborted
  and the existing logical target is terminated rather than left partially
  transitioned and reconnectable.
- If disposing the previous protocol lifetime throws after
  `SetProtocolAsync` has committed the new protocol, the target remains
  terminalized with the new protocol installed and the incoming protocol scope
  owned by target cleanup.
- Response delivery treats both a canceled flush and a completed writer as
  failure. An outer handshake-timeout cancellation is expected control flow
  and is not logged as a reconnect or dispatcher application failure.
- Existing fresh world and lobby response bytes and routing remain unchanged.
- Handshake revision and system-update policy is shared and injectable, with no
  static global policy or request-specific validator hierarchy.
- Cancellation before protocol mutation must not terminalize an unchanged
  target, while cancellation after mutation and final attach failure must
  terminalize the partially transitioned target and abort the replacement.
- Fresh and lobby handshakes must not instantiate the reconnect-only handler;
  reconnect classification resolves it once from the accepted connection
  scope.
- The pre-logical timeout remains active while an opcode-19 authentication
  frame is incomplete, without consuming or fully decoding that frame.

## Affected runtime boundary

The change crosses the GameWorld raw handshake, authorization, session, and
logical Raido connection boundaries. Raido remains the sole owner of physical
transport ownership, reconnect timing, and detach/attach concurrency. GameWorld
owns reconnect authentication and exact session/character authorization.
