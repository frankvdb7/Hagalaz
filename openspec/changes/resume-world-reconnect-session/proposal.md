# Resume the existing world session on reconnect

## Why

Revision 742 uses opcode 16 for both fresh world login and world reconnect.
The reconnect flag must be classified before Raido creates a new logical
connection so a valid reconnect can resume the already-running GameWorld
session and character.

## What changes

- Keep the dedicated reconnect-only authorization request/response. It proves
  the existing identity without minting a token or creating GameWorld state.
- Process the fixed opcode-14 session handshake and send its existing
  acknowledgement, then read and classify the following authentication
  request before creating a logical Raido context. Fresh world and lobby
  requests continue through the normal factory path; only fresh world login
  enables the existing Raido stateful reconnect window.
- For a reconnect request, validate the existing world session, claim, logical
  connection, character, and authentication subject, then ask Raido connection
  infrastructure to activate the raw `ConnectionContext` on that existing
  logical connection.
- Use the existing Raido attach lifecycle and protocol replacement API. No
  candidate logical context, cross-context handoff, transport transfer, or
  reconnect-specific Raido state is introduced.
- Select the physical winner through one application-neutral Raido reservation
  before mutating the target. Keep the existing reconnect waiter pending while
  GameWorld installs the winner protocol and metadata and flushes response 15;
  resume the existing Raido reader only after that boundary.
- Provide handshake policy through an injectable, request-specific
  `IHandshakeValidator<TRequest>`.

## Non-goals

- Do not add authentication opcode 18, a reconnect registry, a second session
  claim, replay, snapshots, or resynchronization.
- Do not call fresh world sign-in for reconnect or repeat character hydration,
  registration, Contacts publication, or world sign-in publication.
- Do not change lobby or flag-0 fresh-login behavior.
- Do not modify the existing #477/#488 Raido reconnect state machine. Raido
  connection infrastructure may expose one application-neutral physical
  activation operation that delegates to its existing internal attach seam.
  No high-level reconnect wrapper is added.

## Acceptance criteria

- Opcode 14 is acknowledged before the following authentication request is
  classified. Opcode 16 with reconnect flag 1 is handled before logical Raido
  context creation and never creates a temporary candidate context.
- Valid credentials can identify only an already-authenticated subject that
  matches the existing world session and character.
- A successful reconnect preserves the existing logical connection ID,
  GameSession, claim, character reference, registration, and handlers. The raw
  replacement physical connection ID is never rewritten.
- The existing Raido reconnect window and GameWorld session claim provide the
  single-winner race boundary. Invalid, stale, duplicate, concurrent, missing,
  and lost-claim requests fail without disturbing resumed state. A candidate
  that loses physical reservation cannot mutate the target protocol, ISAAC,
  protocol lifetime, character metadata, or transport.
- Response 15 uses plain handshake framing with a two-byte payload length and
  the exact 4,608-byte player-entry payload. The candidate must reserve the
  physical winner before GameWorld installs protocol/metadata or sends success;
  the existing detached Raido logical connection resumes only after response
  15 is flushed. The first immediate game packet remains buffered until that
  resume starts Raido's existing reader.
- Existing fresh world and lobby response bytes and routing remain unchanged.
- Handshake policy is injected through request-specific validators, with no
  static global handshake policy class.

## Affected runtime boundary

The change crosses the GameWorld raw handshake, authorization, session, and
logical Raido connection boundaries. Raido remains the sole owner of physical
transport ownership, reconnect timing, and detach/attach concurrency. GameWorld
owns reconnect authentication and exact session/character authorization.
