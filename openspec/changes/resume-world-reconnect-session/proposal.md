# Resume revision-742 world reconnect

## Why

A revision-742 reconnect request must authenticate the supplied credentials,
resume the already-existing GameWorld logical connection, and attach the new
physical connection without allocating a second session or character.

The Raido stateful reconnect lifecycle from #477 already owns the reconnect
window, physical publication, logical lifetime, timeout, and waiter. This change
only connects the GameWorld handshake to that existing operation.

## What changes

- Add tokenless credential validation backed by `PasswordGrantCommand`, while
  preserving its existing validation behavior and avoiding token creation or
  lookup.
- Handle `WorldReconnectRequest` by resolving the existing connection by
  authenticated master ID and verifying session/character ownership.
- Add `WorldReconnectResponse` with the required revision-742 player-entry
  payload and reuse the existing player-entry serialization.
- Enable stateful reconnect for GameWorld.
- After the generic response send completes, invoke the existing `TryReconnect`
  operation with the temporary physical connection and replacement client
  protocol.

## Capabilities

### New capabilities

- `world-reconnect-session`: authenticate and resume an existing GameWorld
  session for the reconnect request.
- No new Raido capability; the existing stateful reconnect capability is reused.

### Modified capabilities

None.

## Impact

Authorization messages/consumer registration, GameWorld authentication and
handshake handling, GameWorld response encoding/registration, and the existing
Raido connection context are affected. Focused authorization, GameWorld,
response, and reconnect tests are added or adapted. No client changes are
required.

## Acceptance criteria

- Reconnect credentials use the existing `PasswordGrantCommand` validation
  path and expose the authenticated master ID without token work.
- GameWorld resolves and verifies the existing session and character, with no
  fresh allocation, hydration, registration, or mediator sign-in.
- The response is sent through the existing generic Raido write path before the
  existing `TryReconnect` publication operation reuses the physical connection.
- Successful publication installs the replacement protocol before the
  replacement transport is observable to resumed processing and before the
  existing reconnect waiter completes.
- Concurrent candidates can result in at most one attached physical transport;
  a failed candidate follows normal temporary cleanup.
- The response has the required revision-742 framing and 4,608-byte
  player-entry payload; standard-map output is unchanged.

## Non-goals and stop conditions

Do not add a second reconnect state machine, registry, waiter, timeout, lease,
reservation model, transport ownership abstraction, generic reliable-send
framework, or broad Raido lifecycle/authentication refactor. Do not create a
fresh world session, hydrate or register a character, perform fresh sign-in,
create or inspect tokens, add replay/snapshot/resynchronization behavior, or
change the client.

Do not duplicate reconnect eligibility, lifetime, timeout, terminal, or
waiter
logic in GameWorld. If a concrete API gap is found, keep the change within the
existing Raido publication and handler paths.
