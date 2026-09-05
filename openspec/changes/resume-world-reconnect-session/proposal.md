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
  connection, character, and authentication subject, then use the existing
  session claim critical section to revalidate the target before mutating it
  and returning the existing logical connection to Raido's connection
  dispatcher.
- Use the existing Raido attach lifecycle and protocol replacement API. No
  candidate logical context, cross-context handoff, transport transfer, or
  reconnect-specific Raido state is introduced.
- Install the winner protocol and metadata and flush response 15 while the
  replacement transport is still raw, then return the existing logical
  connection to Raido's dispatcher for one existing physical attach
  transition. A stale or concurrent candidate is rejected inside the existing
  session claim before target mutation.
- Provide handshake policy through an injectable, request-specific
  `IHandshakeValidator<TRequest>`.

## Non-goals

- Do not add authentication opcode 18, a reconnect registry, a second session
  claim, replay, snapshots, or resynchronization.
- Do not call fresh world sign-in for reconnect or repeat character hydration,
  registration, Contacts publication, or world sign-in publication.
- Do not change lobby or flag-0 fresh-login behavior.
- Do not modify the existing #477/#488 Raido reconnect state machine. Raido
  connection infrastructure owns physical dispatch, logical connection
  creation, and the existing internal attach seam. No public physical attach
  API, reservation, lease, handoff, or second reconnect transition is added.

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
  and transition marker inside that claim before changing target state. A
  transient marker on the existing logical target prevents a second candidate
  from mutating the winner between application selection and Raido's internal
  attach. Invalid, stale, duplicate, concurrent, missing, and lost-claim
  requests fail without disturbing resumed state.
- Response 15 uses plain handshake framing with a two-byte payload length and
  the exact 4,608-byte player-entry payload. The winner installs
  protocol/metadata and flushes response 15 while the physical connection is
  still raw, then returns the target for one existing Raido attach. The first
  immediate game packet remains buffered until that attach starts Raido's
  existing reader.
- If the final attach fails after target mutation, the replacement is aborted
  and the existing logical target is terminated rather than left partially
  transitioned and reconnectable.
- Existing fresh world and lobby response bytes and routing remain unchanged.
- Handshake policy is injected through request-specific validators, with no
  static global handshake policy class.

## Affected runtime boundary

The change crosses the GameWorld raw handshake, authorization, session, and
logical Raido connection boundaries. Raido remains the sole owner of physical
transport ownership, reconnect timing, and detach/attach concurrency. GameWorld
owns reconnect authentication and exact session/character authorization.
