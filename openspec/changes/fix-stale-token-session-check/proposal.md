## Why

After an authorization or GameWorld process restart, a previously issued OpenIddict token can remain valid in persistent storage even though no live game session exists. The authorization sign-in consumer currently mistakes that token for an active login, so a legitimate retry is rejected as already logged in.

## What Changes

- Make successful credential sign-in issue a fresh authorization response without using persisted token existence as an online-session check.
- Keep active-login rejection in the existing lobby/world session stores and distributed world-session claim mechanism.
- Preserve graceful logout token revocation and existing reconnect token validation.
- Add regression coverage for a valid stale token and for the existing active-session rejection path.

## Capabilities

### New Capabilities

- `authentication-session-liveness`: Distinguishes persisted authorization-token validity from the presence of a live game session.

### Modified Capabilities

None.

## Impact

The affected boundary is `Hagalaz.Services.Authorization` sign-in handling, with regression coverage in its authorization tests and existing GameWorld session tests. No protocol, persistence schema, dependency, token-revocation, reconnect, or distributed-session mechanism changes are required.

Acceptance criteria:

- Valid credentials are not rejected solely because a valid token for the same user/client remains persisted.
- A live duplicate lobby/world session is still rejected by the existing GameWorld session ownership checks.
- Graceful logout still sends the existing revocation request, and reconnect validation still uses token validation.
- Focused authorization/GameWorld tests and strict OpenSpec validation pass.

Stop conditions: do not add a second session registry, change token lifetimes or revocation semantics, alter reconnect protocol behavior, or repair unrelated messaging/database cleanup errors in this change.
