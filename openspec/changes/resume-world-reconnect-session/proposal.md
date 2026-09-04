# Resume the existing world session on reconnect

## Why

Revision 742 uses opcode 16 for both fresh world login and world reconnect.
The reconnect flag must be classified before Raido creates a new logical
connection so a valid reconnect can resume the already-running GameWorld
session and character.

## What changes

- Keep the dedicated reconnect-only authorization request/response. It proves
  the existing identity without minting a token or creating GameWorld state.
- Read and classify the first raw GameWorld handshake before creating a
  logical Raido context. Fresh world and lobby requests continue through the
  normal factory path; only fresh world login enables the existing Raido
  stateful reconnect window.
- For a reconnect request, validate the existing world session, claim, logical
  connection, character, and authentication subject, then attach the raw
  `ConnectionContext` directly to that existing logical connection.
- Use the existing Raido attach lifecycle and protocol replacement API. No
  candidate logical context, cross-context handoff, transport transfer, or
  reconnect-specific Raido state is introduced.
- Provide handshake policy through an injectable, request-specific
  `IHandshakeValidator<TRequest>`.

## Non-goals

- Do not add authentication opcode 18, a reconnect registry, a second session
  claim, replay, snapshots, or resynchronization.
- Do not call fresh world sign-in for reconnect or repeat character hydration,
  registration, Contacts publication, or world sign-in publication.
- Do not change lobby or flag-0 fresh-login behavior.
- Do not modify the existing #477/#488 Raido reconnect state machine. The only
  possible Raido change is making its existing physical attach seam public so
  GameWorld can call it directly. No high-level reconnect wrapper is added.

## Acceptance criteria

- Opcode 16 with reconnect flag 1 is handled before logical Raido context
  creation and never creates a temporary candidate context.
- Valid credentials can identify only an already-authenticated subject that
  matches the existing world session and character.
- A successful reconnect preserves the existing logical connection ID,
  GameSession, claim, character reference, registration, and handlers. The raw
  replacement physical connection ID is never rewritten.
- The existing Raido reconnect window and GameWorld session claim provide the
  single-winner race boundary. Invalid, stale, duplicate, concurrent, missing,
  and lost-claim requests fail without disturbing resumed state.
- Response 15 uses plain handshake framing with a two-byte payload length and
  the exact 4,608-byte player-entry payload. GameWorld installs the fresh
  protocol and flushes response 15 before handing the replacement transport to
  the existing detached Raido logical connection. The first immediate game
  packet remains buffered until that attach starts Raido's existing reader.
- Existing fresh world and lobby response bytes and routing remain unchanged.
- Handshake policy is injected through request-specific validators, with no
  static global handshake policy class.

## Affected runtime boundary

The change crosses the GameWorld raw handshake, authorization, session, and
logical Raido connection boundaries. Raido remains the sole owner of physical
transport ownership, reconnect timing, and detach/attach concurrency. GameWorld
owns reconnect authentication and exact session/character authorization.
