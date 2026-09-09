## Context

`ContactSessionStore` is a singleton keyed by `MasterId`, while lobby and
world sign-in/out messages previously carried only the network connection ID.
That ID is not a lifecycle identity: lobby and world can share it, and an old
message can arrive after a newer session. Contacts therefore needs an ordered
owner fact from the GameWorld session boundary.

## Decision

Admit every lobby and world lifecycle through the existing per-account
distributed session claim. Keep the generation counter in the distributed
cache with an infinite lifetime so process restarts and service-instance
changes cannot reuse an older generation. `GameSession` carries that immutable
generation and the exact claim ID, and existing lobby/world presence messages
and sign-out commands propagate the generation alongside `ConnectionId`.

The generation is allocated before local object construction when necessary,
so failed attempts may consume gaps. A failed attempt never publishes its
generation: lobby presence is published only after the lobby claim and local
admission succeed, while a world generation is authoritative only after its
claim is acquired and world commit succeeds. This makes a higher generation a
valid newer account lifecycle rather than merely a later allocation attempt.

GameWorld uses one claim key for both lobby and world sessions. A lobby login
claims the account globally, so a lobby on another GameWorld cannot replace an
active world owner or compete with another lobby. A world promotion keeps the
existing lobby claim while world initialization is pending, then atomically
replaces that exact lobby claim with the world claim while committing the local
session. If the commit fails, the claim store restores the lobby claim.
Pending lobby-to-world promotions are excluded from world-claim renewal; the
active lobby continues renewing its own claim until promotion completes.

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
