## Context

`ContactSessionStore` is a singleton keyed by `MasterId`, while lobby and
world sign-in/out messages previously carried only the network connection ID.
That ID is not a lifecycle identity: lobby and world can share it, and an old
message can arrive after a newer session. Contacts therefore needs an ordered
owner fact from the GameWorld session boundary.

## Decision

Allocate a monotonic `SessionGeneration` under the existing per-account
distributed game-session lock. Keep its counter in the distributed cache with
an infinite lifetime so process restarts and service-instance changes cannot
reuse an older generation. `GameSession` carries that immutable generation,
and existing lobby/world presence messages and sign-out commands propagate it
alongside `ConnectionId`.

`ContactSessionContext` stores the generation and connection data. The existing
store remains the single owner of presence state:

- any sign-in adds when no presence exists and atomically replaces only a lower
  generation;
- equal-generation duplicates and lower-generation stale sign-ins are ignored;
- sign-out compares both generation and connection with the stored owner before
  removal;
- world-status cleanup retains compare-and-remove against the snapshotted
  context.

The store serializes its owner-changing operations with one local gate and
uses the existing compare-and-remove primitive for stale cleanup. Replacement
publishes only the new `ContactSignInMessage`; it does not manufacture a
sign-out event for the replaced owner.

GameWorld passes the session generation and already available connection ID from
`IGameSession` or `ICharacter.Session` through its existing mediator consumers.
No new owner lookup, retry mechanism, or generic ordering framework is
introduced. The generated `Guid SessionId` in `ContactSessionContext` is removed
because it was created too late to identify the upstream lifecycle.
