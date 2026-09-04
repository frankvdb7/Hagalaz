# Tasks

## Authentication and GameWorld

- [x] Add a dedicated reconnect-only authorization request/response using existing password validation and no token minting; preserve normal sign-in semantics.
- [x] Add reconnect authentication and exact existing-session/claim/character checks without fresh-login features or side effects.
- [x] Handle `WorldReconnectRequest`, delegate one candidate handoff to Raido, and keep candidate cleanup from signing out the target.

## Raido transport

- [x] Enable stateful reconnect only after successful fresh world login.
- [x] Extend the existing detach/attach lifecycle with one narrow physical handoff operation; preserve physical and logical IDs, single-winner ownership, and safe candidate cleanup.
- [x] Expose reconnect enabling through `IRaidoStatefulReconnectFeature`, before fresh-login success is committed, and remove reconnect control from `RaidoCallerContext`.
- [x] Enforce the candidate-token cancellation boundary and target-owned response/protocol ordering.
- [x] Prove immediate post-handoff input uses the target and is parsed only after the fresh protocol transition.

## Protocol and tests

- [x] Add response 15 with exact shared 4,608-byte player-entry encoding and declared two-byte handshake framing without changing existing response bytes.
- [x] Add focused authorization, GameWorld, Raido, handler, and protocol regression coverage for identity, ownership, races, lifetime, cleanup, framing, ISAAC state, first-packet routing, and unchanged fresh login.

## Validation

- [x] Run strict OpenSpec validation, the requested test matrix, solution build, and final diff/scope review.
