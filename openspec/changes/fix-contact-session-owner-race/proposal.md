## Why

Contacts presence is currently keyed only by `MasterId` and treats the network
`ConnectionId` as the lifecycle owner. A lobby sign-out can therefore remove a
newer world presence, same-connection lobby-to-world promotion can be ignored,
and a delayed old sign-in can replace the current presence.

## What changes

- Allocate one monotonic session generation per account at the GameWorld
  session-ownership boundary.
- Carry the generation and connection ID through lobby/world presence messages.
- Store the generation as the authoritative Contacts lifecycle owner, with the
  connection ID retained as session data and an exact sign-out check.
- Replace only with a higher generation and ignore duplicate or stale sign-ins.
- Add deterministic transition-order, stale-sign-in, stale-sign-out, and
  message-propagation tests.

## Non-goals

- Do not add a second presence registry, retry worker, ordering framework, or
  lifecycle state machine.
- Do not change GameWorld authentication sequencing beyond allocating and
  mechanically propagating the session generation.
- Do not change contact notification meaning: a successful replacement emits
  the new sign-in once and does not emit an intermediate sign-out.

## Acceptance criteria

- World sign-in followed by stale lobby sign-out leaves the world presence.
- Stale lobby sign-out followed by world sign-in establishes the world presence.
- Stale world sign-out cannot remove a newer world presence.
- Current-owner sign-out removes the presence exactly once.
- Duplicate sign-in for the current generation does not duplicate presence
  notifications.
- The affected Contacts and GameWorld tests compile and pass.
