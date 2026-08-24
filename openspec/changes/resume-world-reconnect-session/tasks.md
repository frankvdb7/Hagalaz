## 1. Raido handoff

- [x] 1.1 Allow the existing rebind operation to install a replacement protocol atomically with the physical transport.
- [x] 1.2 Enable bounded stateful reconnect support for the GameWorld endpoint and activate it only after successful world sign-in.

## 2. GameWorld reconnect flow

- [x] 2.1 Add an authentication-only world reconnect path that does not create or hydrate a session.
- [x] 2.2 Resolve and validate the existing world session, character, logical context, and ownership before rebind.
- [x] 2.3 Reserve the opcode-18 reconnect during dispatch, then commit with fresh protocol/ISAAC state, preserve any same-buffer suffix, and send the normal world success response.
- [x] 2.4 Reject failed reconnects without running unrelated lobby/session sign-out side effects.
- [x] 2.5 Register map/appearance resynchronization as explicit reconnect work and preserve response-first replacement output ordering; terminate invalidated temporary replacements.

## 3. Resynchronization and tests

- [x] 3.1 Add focused authoritative map/appearance resynchronization without `OnRegistered` or duplicate registration.
- [x] 3.2 Add tests for authentication-only resume and the atomic fresh-protocol transfer; Raido integration tests cover real pipes, response-first output ordering, same-buffer suffixes, expiry, invalidation, and races.
- [x] 3.3 Exercise large authoritative resynchronization with a live output consumer, discard uncertain pre-loss output, and assert response/map/appearance/normal-traffic ordering.

## 4. Validation

- [x] 4.1 Run focused GameWorld and Raido tests, affected builds, strict OpenSpec validation, and cumulative diff review.
