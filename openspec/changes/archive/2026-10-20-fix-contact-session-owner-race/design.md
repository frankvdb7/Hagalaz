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
active world owner or compete with another lobby. The lobby response carries the
opaque exact claim ID, and the client presents it in the world handshake when
the selected world is on another GameWorld process. A world promotion keeps
the existing lobby claim while world initialization is pending, then atomically
replaces that exact lobby claim with the world claim while committing the local
session. If the commit fails, the claim store restores the lobby claim. A
missing, stale, or unrelated handoff ID cannot replace the current owner.
Pending lobby-to-world promotions are excluded from world-claim renewal; the
active lobby continues renewing its own claim until promotion completes.

Initial contact snapshots carry the exact session generation and connection ID
for online friends. Each `IContactsFeature` applies the full friends snapshot
and rebuilds its local presence projection under the same feature-owned gate as
incremental sign-in/sign-out transitions. Snapshot replacement therefore prunes
owners for removed or offline contacts, and a sign-out is accepted only when an
exact current local owner exists.

When local lobby admission fails after its claim was acquired and exact release
cannot be confirmed, the existing pending claim-reconciliation path retains the
exact session record. A definitive release result of either true or false
resolves the old local lifecycle: true removes the exact current claim, while
false proves that exact claim is absent or belongs to another owner. Only an
exception or other uncertain distributed outcome requires deferred cleanup.
Lease reconciliation uses compare-and-remove semantics, so a newer owner is
never removed. The same retained record is not an ordinary local pending
lifecycle entry: local removal operations leave it in place, abort
reconciliation cannot replace it with a pending-abort record, and only the exact
reconciliation operation may remove it after release succeeds or the claim store
proves that owner is no longer current.

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

Live friend membership uses the same feature-owned presence gate as snapshot
and sign-in/sign-out updates. `IContactsFeature.AddFriend` receives the exact
session identity when the contacts service reports the friend online, while
`RemoveFriend` removes both the friend and any owner entry. The contacts
consumer projects world data and session generation/connection from one
`ContactSessionContext`, so an online add cannot create a friend without its
presence owner. The Hub no longer mutates `Friends` directly.
