# Tasks

## Authentication and GameWorld

- [x] Add reconnect-only authorization identity proof using existing password validation and no token side effects.
- [x] Add reconnect authentication and exact existing-session/claim/character checks without fresh-login features or side effects.
- [x] Handle `WorldReconnectRequest`, delegate one candidate handoff to Raido, and keep candidate cleanup from signing out the target.

## Raido transport

- [x] Enable stateful reconnect only after successful fresh world login.
- [x] Add one narrow current-API physical handoff operation with atomic single-winner ownership and safe candidate cleanup.
- [x] Prove immediate post-handoff input uses the target and is parsed only after the fresh protocol transition.

## Protocol and tests

- [x] Add response 15 with exact shared 4,608-byte player-entry encoding and two-byte handshake framing without changing existing response bytes.
- [x] Add focused authorization, GameWorld, Raido, and protocol regression coverage for identity, ownership, races, lifetime, cleanup, framing, ISAAC state, and unchanged fresh login.

## Validation

- [x] Run strict OpenSpec validation, focused tests, solution build, and final diff/scope review.
