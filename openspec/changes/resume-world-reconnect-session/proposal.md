# Resume the existing world session on reconnect

## Why

Revision-742 sends both fresh world login and world reconnect through
authentication opcode 16. The reconnect flag distinguishes them. Hagalaz must
use that flag to resume the existing logical GameWorld session and character
after Raido replaces the detached physical socket.

## What changes

- Add a dedicated reconnect-only authorization request/response using existing
  password validation and no token minting. The normal sign-in contract remains
  token-issuing and has no reconnect mode flag.
- Resolve the exact existing world session and logical Raido connection,
  preserve its claim and GameWorld state, and delegate physical handoff to
  Raido's stable TCP/Hub connection boundary.
- Enable stateful reconnect only after a successful fresh world login, through
  the stable logical connection's reconnect feature.
- Send revision-742 response 15 only after the existing target has accepted the
  replacement and installed its fresh seeded client protocol; resumed game
  input is released only after the response is flushed and the handoff waiter
  completes.

## Non-goals

- Do not add authentication opcode 18, a reconnect registry, a second session
  claim, a second handoff state machine, replay, snapshots, or
  resynchronization.
- Do not call fresh world sign-in for reconnect or repeat character hydration,
  registration, Contacts publication, or world sign-in publication.
- Do not change lobby or flag-0 fresh-login behavior.

## Acceptance criteria

- Valid credentials can identify only an already-authenticated subject that
  matches the existing world session and character.
- A successful reconnect keeps the same Raido logical connection ID,
  GameSession, claim, character reference, registration, and handlers. The
  replacement physical connection ID remains unchanged.
- Only one candidate can win the existing Raido reconnect window. Invalid,
  stale, duplicate, concurrent, missing-session, and lost-claim candidates
  fail without disturbing the winner or resumed state.
- Candidate teardown cannot sign out or remove the resumed session.
- Response 15 is plain handshake framing with a two-byte payload length and
  the exact 4,608-byte player-entry payload. Handshake framing is selected from
  the declared message size, not the concrete response type. The target uses
  fresh ISAAC state from the reconnect request before it consumes the first
  game packet.
- Existing fresh-login and lobby response bytes remain unchanged.

## Affected runtime boundary

The change crosses the GameWorld handshake and authentication boundary and one
narrow Raido physical-handoff boundary. Raido remains the sole owner of
physical transport ownership, reconnect timing, the single existing
detach/attach lifecycle, and single-winner state. The replacement physical
connection ID is never rewritten; the target's stable logical connection ID
remains the GameWorld identity. A candidate cancellation token may cancel
before physical transfer, but cannot cancel the target-owned transition after
ownership transfer.
