## 1. Raido handoff

- [x] 1.1 Allow the existing rebind operation to install a replacement protocol atomically with the physical transport.
- [x] 1.2 Enable bounded stateful reconnect for the GameWorld endpoint.

## 2. GameWorld reconnect flow

- [x] 2.1 Add an authentication-only world reconnect path that does not create or hydrate a session.
- [x] 2.2 Resolve and validate the existing world session, character, logical context, and ownership before rebind.
- [x] 2.3 Rebind opcode 18 with fresh protocol/ISAAC state and send the normal world success response.
- [x] 2.4 Reject failed reconnects without running unrelated lobby/session sign-out side effects.

## 3. Resynchronization and tests

- [x] 3.1 Add focused authoritative map/appearance resynchronization without `OnRegistered` or duplicate registration.
- [x] 3.2 Add tests for authentication-only resume and the atomic fresh-protocol handoff; existing Raido lifecycle tests cover expiry and races.

## 4. Validation

- [x] 4.1 Run focused GameWorld and Raido tests, affected builds, strict OpenSpec validation, and cumulative diff review.
