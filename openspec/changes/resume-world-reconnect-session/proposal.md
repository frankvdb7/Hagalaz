# Resume revision-742 world reconnect

## Why

Revision 742 sends world reconnect intent as opcode 16 with reconnect flag 1. The
server must authenticate that request and resume the existing GameWorld logical
session without allocating or hydrating a second world session. The existing
Raido #477 transition already owns reconnect lifetime and concurrency, but its
physical-only handoff needs one narrow candidate-owned claim to span the
asynchronous response-15 flush.

## What changes

- Add a tokenless authorization request that reuses `PasswordGrantCommand` and
  exposes existing credential-validation results without OpenIddict token work.
- Add an exact-type `WorldReconnectRequest` handler that resolves the existing
  session by authenticated master ID and verifies session/character ownership.
- Add the minimal revision-742 response 15 with the required 4,608-byte
  player-entry payload.
- Enable stateful reconnect for GameWorld and extend the existing Raido
  transition with one private physical-candidate claim and the smallest
  physical write/flush completion path needed for safe handoff.
- Preserve temporary-connection cleanup boundaries and the stable logical
  connection while installing the replacement client protocol before resumed
  processing.

## Capabilities

### New capabilities

- `world-reconnect-session`: authenticate and resume an existing GameWorld
  session for the revision-742 reconnect request.
- `raido-reconnect-handoff`: complete a candidate-owned physical handoff through
  the existing stateful reconnect transition.

### Modified capabilities

None.

## Impact

Authorization messages/consumer registration, GameWorld authentication and
handshake handling, GameWorld response encoding/registration, and Raido's
existing reconnect context/handler. Focused authorization, GameWorld, response,
and Raido tests are added or extended. No client changes are required.

## Acceptance criteria

- Only opcode 16 reconnect flag 1 invokes the reconnect handler; opcode 18 and
  fresh flag 0 behavior remain out of scope/unchanged.
- Credentials are validated through the existing `PasswordGrantCommand` path,
  returning the authenticated master ID without token creation or inspection.
- GameWorld resolves and verifies the existing session and character, with no
  fresh allocation, hydration, registration, or mediator sign-in.
- Exactly one candidate can claim the existing #477 reconnect window. Only that
  candidate may complete and commit response 15.
- Response 15 is physically written/flushed before protocol installation,
  physical publication, waiter completion, and resumed logical processing.
- A failed, canceled, closed, ambiguous, stale, losing, timeout, or aborted
  candidate cannot publish a transport or mutate logical protocol/session/
  character state.
- The replacement protocol is installed on the existing logical context and the
  adopted physical transport is not read from or disposed by the temporary
  context.
- Response 15 contains only the required revision-742 4,608-byte payload and
  standard-map encoding remains unchanged.

## Non-goals and stop conditions

Do not add opcode 18, fresh world-session allocation, character hydration or
registration, `WorldSignInCommand`, OpenIddict token creation/inspection,
replay protection, snapshots, resynchronization, client changes, a second
reconnect registry/state machine/waiter, public reservations, leases, generic
pending infrastructure, or broad Raido lifecycle/authentication refactoring.

Do not duplicate #477 reconnect eligibility, lifetime, timeout, terminal, or
concurrency logic in GameWorld. If a concrete existing API gap is found, stop
and keep the seam narrower rather than adding a second owner.
