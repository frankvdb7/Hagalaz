## Why

World entry publishes `WorldSignInCommand` as fire-and-forget work after the
authentication service has committed the character and world session. If
character initialization then fails, the client remains on "Logging In - Please
Wait" while the character, region membership, session, and external world
presence can remain registered. A retry can then fail with a duplicate region
registration.

## What changes

- Clean up the character, session, and world presence when post-authentication
  world initialization fails.
- Preserve the original initialization exception for mediator error handling.
- Add regression coverage for failed cleanup and successful one-time startup.

## Impact

This affects the GameWorld world-entry initialization boundary. It reuses the
existing character and game-session services and does not change authentication,
region membership validation, or the reconnect protocol.

## Acceptance criteria

- A failure after world authentication does not leave the character in the
  character store or its region.
- A failed world initialization releases both the distributed/local session
  state and publishes world sign-out cleanup.
- The initialization failure remains observable to the mediator.
- Successful world initialization still runs once and publishes contacts and
  world presence.
- Focused tests, the affected build, strict OpenSpec validation, and a clean
  manual retry from the lobby pass.

## Non-goals

- Do not make `MapRegion.Add` idempotent.
- Do not redesign authentication, map loading, session ownership, or reconnect.
- Do not change the unrelated MySQL inbox cleanup query.
