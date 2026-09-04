# Resume the existing world session on reconnect

## Why

Revision-742 sends both fresh world login and world reconnect through
authentication opcode 16. The reconnect flag distinguishes them. Hagalaz must
use that flag to resume the existing logical GameWorld session and character
after Raido replaces the detached physical socket.

## What changes

- Expose the authenticated subject from the existing sign-in request when the
  submitted password is valid and an existing authentication is present,
  without minting a replacement token.
- Add a narrow reconnect authentication operation that returns only the
  verified master ID and installs no candidate GameWorld features.
- Resolve the exact existing world session and logical Raido connection,
  preserve its claim and GameWorld state, and delegate physical handoff to
  Raido's stable TCP/Hub connection boundary.
- Enable stateful reconnect only after a successful fresh world login.
- Send revision-742 response 15 with the existing standard-map player-entry
  bits, then install a fresh seeded client protocol on the existing logical
  connection before resumed game input is parsed.

## Non-goals

- Do not add authentication opcode 18, a reconnect registry, a second session
  claim, a reconnect state machine, replay, snapshots, or resynchronization.
- Do not call fresh world sign-in for reconnect or repeat character hydration,
  registration, Contacts publication, or world sign-in publication.
- Do not change lobby or flag-0 fresh-login behavior.

## Acceptance criteria

- Valid credentials can identify only an already-authenticated subject that
  matches the existing world session and character.
- A successful reconnect keeps the same Raido logical connection ID,
  GameSession, claim, character reference, registration, and handlers.
- Only one candidate can win the existing Raido reconnect window. Invalid,
  stale, duplicate, concurrent, missing-session, and lost-claim candidates
  fail without disturbing the winner or resumed state.
- Candidate teardown cannot sign out or remove the resumed session.
- Response 15 is plain handshake framing with a two-byte payload length and
  the exact 4,608-byte player-entry payload. The target uses fresh ISAAC state
  from the reconnect request before it consumes the first game packet.
- Existing fresh-login and lobby response bytes remain unchanged.

## Affected runtime boundary

The change crosses the GameWorld handshake and authentication boundary and one
narrow Raido physical-handoff boundary. Raido remains the sole owner of
physical transport ownership, reconnect timing, and single-winner state.
