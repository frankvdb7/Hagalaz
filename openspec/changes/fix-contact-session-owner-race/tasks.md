## 1. Presence ownership

- [x] 1.1 Add connection ownership to `ContactSessionContext` and the existing
      store's owner-changing operations.
- [x] 1.2 Require exact connection ownership for lobby/world sign-out and
      preserve compare-and-remove for world-status cleanup.

## 2. Message propagation

- [x] 2.1 Add connection IDs to existing lobby/world presence messages and
      mediator sign-out commands.
- [x] 2.2 Pass the existing `IGameSession.ConnectionId` through GameWorld and
      Contacts consumers.

## 3. Regression coverage

- [x] 3.1 Add both lobby/world message-ordering tests.
- [x] 3.2 Add stale-owner, current-owner, duplicate-notification, and message
      forwarding tests.
- [ ] 3.3 Run focused tests, build checks, and strict OpenSpec validation.
