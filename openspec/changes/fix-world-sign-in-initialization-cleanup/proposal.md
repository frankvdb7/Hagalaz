## Why

World entry publishes `WorldSignInCommand` as fire-and-forget work after the
authentication service has committed the character and world session. If
character initialization then fails, the client can remain connected while the
normal disconnect/sign-out owner has not yet been invoked.

## What changes

- Abort the connection when post-authentication world initialization fails so
  the normal disconnect/sign-out owner cleans up the character, session, and
  world presence.
- Preserve the original initialization exception for mediator error handling.
- Add regression coverage for failed cleanup and successful one-time startup.

## Impact

This affects the GameWorld world-entry initialization boundary. It reuses the
existing connection and authentication cleanup owner and does not change
authentication, region membership validation, or the reconnect protocol.

## Acceptance criteria

- A failure after world authentication aborts the connection and delegates
  character, region, distributed/local session, persistence, and world-sign-out
  cleanup to the normal disconnect/sign-out owner.
- The initialization failure remains observable to the mediator.
- Successful world initialization still runs once and publishes contacts and
  world presence.
- Focused tests, the affected build, strict OpenSpec validation, and a clean
  manual retry from the lobby pass.

## Non-goals

- Do not make `MapRegion.Add` idempotent.
- Do not redesign authentication, map loading, session ownership, or reconnect.
- Do not change the unrelated MySQL inbox cleanup query.
