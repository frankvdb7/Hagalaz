## Why

Completed GameClient #142 work establishes that revision-742 world reconnect uses authentication opcode 16 with a reconnect flag of 1. The existing Hagalaz registration for authentication opcode 18 and the decoder's ignored flag contradict that request contract. Issue #478 authorizes correcting this request-side boundary while production reconnect behavior remains unproven.

## What Changes

- Remove the stale authentication-protocol opcode-18 decoder registration.
- Preserve the opcode-16 reconnect flag by producing explicit fresh-world or reconnect intent messages.
- Keep reconnect intent out of the existing `WorldSignInRequest` handler through Raido's exact concrete-type dispatch.
- Retain the secret-safe controlled-peer and client-source characterization fixture/tests.
- Leave response generation, authentication orchestration, cipher transitions, session ownership, transport handoff, lifecycle, and resynchronization for follow-up work in #478.

## Capabilities

### New Capabilities

- `world-reconnect-intent`: Preserve the verified revision-742 request-side reconnect intent without implementing server reconnect.

### Modified Capabilities

None.

## Impact

- GameWorld handshake decoder, authentication protocol registration, and minimal request message type.
- Focused GameWorld and generic Raido dispatch tests.
- Secret-safe characterization fixture and OpenSpec records.
- No Raido production changes, GameWorld session changes, response-15 implementation, cipher transition, transport handoff, or resynchronization.

## Acceptance criteria

- Revision-742 world authentication remains registered on opcode 16 only.
- Opcode 16 with flag 0 produces `WorldSignInRequest`.
- Opcode 16 with flag 1 produces `WorldReconnectRequest` and cannot invoke the fresh-world sign-in handler.
- Authentication opcode 18 is absent from the revision-742 authentication registration.
- Characterization values remain qualified as controlled-peer/client observations or client-code facts.
- Unknown production reconnect behavior remains explicitly deferred.
- Issue #478 remains open and is not claimed as closed by this change.

## Stop conditions

- Do not add a reconnect handler, response message/encoder, abort/error response, authentication flow, session lookup, token, cipher transition, transport handoff, lifecycle change, or resynchronization payload.
- Do not modify Raido production behavior or replace #477's reconnect/grace mechanism.
- Do not turn controlled-peer stimuli, client observations, or client-code facts into production-server requirements.
- Report any required production behavior as follow-up work for issue #478.
