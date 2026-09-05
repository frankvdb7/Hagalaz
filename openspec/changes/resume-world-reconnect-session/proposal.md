# Resume the existing world session on reconnect

## Why

Revision 742 uses opcode 16 for both fresh world login and world reconnect.
The reconnect flag must be classified before Raido creates a new logical
connection so a valid reconnect can resume the already-running GameWorld
session and character.

## What changes

- Keep dedicated reconnect-only authorization validation separate from normal
  token-issuing sign-in.
- Process opcode 14 and its acknowledgement, then classify the following
  authentication request before creating a logical Raido context. Fresh world
  and lobby requests continue through the normal factory path; only fresh
  world login enables Raido stateful reconnect.
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
  handshake protocol for the connection lifetime.
- Provide handshake policy through an injectable, request-specific
  `IHandshakeValidator<TRequest>`.

## Non-goals

- Do not add authentication opcode 18, a reconnect registry, a second session
  claim, replay, snapshots, or resynchronization.
- Do not call fresh world sign-in for reconnect or repeat character hydration,
  registration, Contacts publication, or world sign-in publication.
- Do not change lobby or flag-0 fresh-login behavior.
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
- Existing fresh world and lobby response bytes and routing remain unchanged.
- Handshake policy is injected through request-specific validators, with no
  static global handshake policy class.
- Cancellation before protocol mutation must not terminalize an unchanged
  target, while cancellation after mutation and final attach failure must
  terminalize the partially transitioned target and abort the replacement.
- Fresh and lobby handshakes must not instantiate the reconnect-only handler;
  reconnect classification resolves it once from the accepted connection
  scope.

## Affected runtime boundary

The change crosses the GameWorld raw handshake, authorization, session, and
logical Raido connection boundaries. Raido remains the sole owner of physical
transport ownership, reconnect timing, and detach/attach concurrency. GameWorld
owns reconnect authentication and exact session/character authorization.
