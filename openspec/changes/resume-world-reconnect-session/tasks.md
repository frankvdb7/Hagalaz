## 1. Raido handoff

- [x] 1.1 Allow the existing rebind operation to install a replacement protocol atomically with the physical transport.
- [x] 1.2 Enable bounded stateful reconnect support for the GameWorld endpoint and activate it only after successful world sign-in.

## 2. GameWorld reconnect flow

- [x] 2.1 Add an authentication-only world reconnect path that does not create or hydrate a session.
- [x] 2.2 Resolve and validate the existing world session, character, logical context, and ownership before rebind.
- [x] 2.3 Register a reader-safe opcode-18 transport handoff, then rebind with fresh protocol/ISAAC state and send the normal world success response.
- [x] 2.4 Reject failed reconnects without running unrelated lobby/session sign-out side effects.

## 3. Resynchronization and tests

- [x] 3.1 Add focused authoritative map/appearance resynchronization without `OnRegistered` or duplicate registration.
- [x] 3.2 Add tests for authentication-only resume and the atomic fresh-protocol handoff; existing Raido lifecycle tests cover expiry and races.

## 4. Validation

- [x] 4.1 Run focused GameWorld and Raido tests, affected builds, strict OpenSpec validation, and cumulative diff review.
