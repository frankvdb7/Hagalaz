## Context

`ContactSessionStore` is a singleton keyed by `MasterId`, while lobby and
world sign-in/out messages currently carry no session identity. The existing
`IGameSession.ConnectionId` is already stable for the logical game connection
and is available when GameWorld publishes each lifecycle message.

## Decision

Add `ConnectionId` to `ContactSessionContext` and to the existing lobby/world
presence messages. `ContactSessionService` uses the existing store as the
single owner of presence state:

- lobby sign-in adds only when no presence exists;
- world sign-in replaces an older presence unless it is a duplicate for the
  current connection;
- sign-out compares the message owner with the stored owner before removal;
- world-status cleanup retains compare-and-remove against the snapshotted
  context.

The store serializes its owner-changing operations with one local gate and
uses the existing compare-and-remove primitive for stale cleanup. Replacement
publishes only the new `ContactSignInMessage`; it does not manufacture a
sign-out event for the replaced owner.

GameWorld passes the already available connection ID from `IGameSession` or
`ICharacter.Session` through its existing mediator consumers. No new owner
lookup or retry mechanism is introduced.
