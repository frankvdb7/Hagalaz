## Why

Contacts presence is currently keyed only by `MasterId`. A lobby sign-out can
therefore remove a newer world presence, and a world sign-in can be discarded
because the stale lobby presence has not been removed yet.

## What changes

- Carry the existing game connection ID through lobby/world presence messages.
- Store the connection ID as the owner of each Contacts presence.
- Replace an existing presence when a world sign-in establishes a newer owner.
- Remove presence only when the sign-out owner matches the stored connection.
- Add deterministic transition-order, stale-logout, replacement, and message
  propagation tests.

## Non-goals

- Do not add a second presence registry, retry worker, ordering framework, or
  lifecycle state machine.
- Do not change GameWorld authentication sequencing beyond carrying the existing
  session connection ID into sign-out messages.
- Do not change contact notification meaning: a successful replacement emits
  the new sign-in once and does not emit an intermediate sign-out.

## Acceptance criteria

- World sign-in followed by stale lobby sign-out leaves the world presence.
- Stale lobby sign-out followed by world sign-in establishes the world presence.
- Stale world sign-out cannot remove a newer world presence.
- Current-owner sign-out removes the presence exactly once.
- Duplicate sign-in for the current connection does not duplicate presence
  notifications.
- The affected Contacts and GameWorld tests compile and pass.
