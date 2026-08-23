## Why

The GameWorld handshake registers opcode 18 as a reconnect request, but the shared decoder drops the selected opcode and always creates `WorldSignInRequest`. Application dispatch therefore cannot distinguish a fresh world login from a reconnect attempt.

## What Changes

- Preserve the existing opcode-to-decoder registration boundary while producing distinct typed messages for world login and world reconnect.
- Parse opcode 18 with the same revision-742 payload fields verified in `Hagalaz.GameClient`.
- Keep opcode 16 on the existing authentication and world sign-in path.
- Route reconnect requests to a safe unsupported result until session rebind work is implemented.
- Reject malformed reconnect payloads with the existing decoder failure behavior.
- Add focused decoder and hub-dispatch regression coverage.

## Capabilities

### New Capabilities

- `world-reconnect-intent`: Distinguishes fresh world login and reconnect intent at the GameWorld application boundary.

### Modified Capabilities

None.

## Impact

The change is limited to the GameWorld handshake message/decoder registrations, handshake hub dispatch, and focused GameWorld tests. It reuses the existing Raido opcode registry, RSA/XTEA parsing, sign-in request fields, and failure response. It does not add a dependency or change the revision-742 wire payload.

## Acceptance Criteria

- Opcode 16 decodes to the existing fresh-login message and continues through the existing world sign-in handler.
- Opcode 18 decodes to a distinct reconnect message carrying exactly the verified shared payload fields.
- Reconnect dispatch returns the current safe failure/unsupported response and aborts the connection.
- Malformed reconnect input returns `false` with no message.
- No reconnect token, session id, stateful session, rebind, replay, or character-resume behavior is introduced.

## Stop Conditions

Stop and record a follow-up if supporting opcode 18 requires inventing wire fields, changing RSA/XTEA framing, implementing logical session lifetime, or changing GameWorld persistence/logout behavior.
