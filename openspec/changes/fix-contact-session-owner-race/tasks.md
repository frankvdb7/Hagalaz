## 1. Presence ownership

- [x] 1.1 Allocate and carry a monotonic GameWorld session generation.
- [x] 1.2 Admit lobby/world sessions through the existing distributed account
      claim and store the generation and connection data in
      `ContactSessionContext`.
- [x] 1.3 Replace only with newer generations and remove only for the exact
      generation/connection owner.
- [x] 1.4 Transfer the exact lobby claim atomically during local and
      cross-GameWorld lobby-to-world commit and preserve claim
      renewal/cleanup semantics.

## 2. Message propagation

- [x] 2.1 Add session generations and connection IDs to existing lobby/world
      presence messages and mediator sign-out commands.
- [x] 2.2 Pass the immutable `IGameSession` generation and connection ID through
      GameWorld and Contacts consumers.

## 3. Regression coverage

- [x] 3.1 Add both lobby/world message-ordering tests, including same-connection
      promotion and delayed World-to-Lobby replacement.
- [x] 3.2 Add stale-sign-in, stale-sign-out, exact-owner, duplicate-notification,
      generation-allocation, message-forwarding, cross-GameWorld ownership and
      exact-handoff tests, including failed-admission claim reconciliation and
      retained-cleanup protection during duplicate/local removal, stale lease
      abort reconciliation, and definitive false-release cleanup.
- [ ] 3.3 Run focused tests, build checks, and strict OpenSpec validation.
