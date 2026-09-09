## Why

Contacts presence is currently keyed only by `MasterId` and treats the network
`ConnectionId` as the lifecycle owner. A lobby sign-out can therefore remove a
newer world presence, same-connection lobby-to-world promotion can be ignored,
and a delayed old sign-in can replace the current presence.

## What changes

- Admit lobby and world lifecycles through the existing distributed per-account
  session claim, and allocate one monotonic session generation per account at
  that GameWorld session-ownership boundary.
- Carry the exact lobby claim from the lobby response into the world handshake
  so a different GameWorld process can perform the same compare-and-replace
  promotion.
- Carry the generation and connection ID through lobby/world presence messages.
- Store the generation as the authoritative Contacts lifecycle owner, with the
  connection ID retained as session data and an exact sign-out check.
- Replace only with a higher generation and ignore duplicate or stale sign-ins.
- Add deterministic transition-order, stale-sign-in, stale-sign-out, and
  message-propagation tests.

## Non-goals

- Do not add a second presence registry, retry worker, ordering framework, or
  lifecycle state machine.
- Do not change credential validation or character initialization. Extend the
  existing handshake only with the opaque exact lobby claim required by the
  session-ownership promotion boundary.
- Do not change contact notification meaning: a successful replacement emits
  the new sign-in once and does not emit an intermediate sign-out.

## Acceptance criteria

- World sign-in followed by stale lobby sign-out leaves the world presence.
- Stale lobby sign-out followed by world sign-in establishes the world presence.
- Stale world sign-out cannot remove a newer world presence.
- Current-owner sign-out removes the presence exactly once.
- Duplicate sign-in for the current generation does not duplicate presence
  notifications.
- A GameWorld instance cannot admit a lobby lifecycle while another instance
  still owns an active world lifecycle for the account.
- After exact ownership release, another GameWorld instance can admit a newer
  lobby lifecycle.
- Lobby-to-world promotion transfers the existing lobby claim atomically with
  the local session replacement, including when lobby and world are on
  different GameWorld processes.
- A failed exact claim release remains represented for lease-worker
  reconciliation and cannot remove a newer exact owner; local lifecycle cleanup
  cannot discard or replace the sole reconciliation record with an abort-only
  reservation before that resolution.
- The affected Contacts and GameWorld tests compile and pass.
