## Why

Completed GameClient #142 work provides separate request-trace and client-source evidence for revision-742 world reconnect. The client trace shows handshake opcode 14, fresh world authentication on opcode 16 with flag 0, reconnect world authentication on opcode 16 with flag 1, and no authentication-header opcode 18. Client source inspection shows that authentication opcode 18 is absent from the revision-742 authentication registry, while a separate ISAAC-framed game-channel opcode 18 exists. The existing Hagalaz registration for authentication opcode 18 and the decoder's ignored flag contradict that request contract. Issue #478 authorizes correcting this request-side boundary while production reconnect behavior remains unproven.

## What Changes

- Remove the stale authentication-protocol opcode-18 decoder registration.
- Preserve the opcode-16 reconnect flag by producing explicit fresh-world or reconnect intent messages.
- Keep reconnect intent out of the existing `WorldSignInRequest` handler through Raido's exact concrete-type dispatch.
- Retain the secret-safe characterization fixture/tests with request-trace facts, controlled-peer stimuli, client-side observations, client-code facts, and unknown production-server behavior kept separate.
- Leave response generation, authentication orchestration, cipher transitions, session ownership, transport handoff, lifecycle, and resynchronization for follow-up work in #478.

## Evidence categories

- **Observed client requests and trace facts:** handshake opcode 14; fresh world authentication opcode 16 with reconnect flag 0; reconnect world authentication opcode 16 with reconnect flag 1; and no authentication-header opcode 18 in the characterized reconnect trace.
- **Controlled-peer stimuli:** fresh-login response 2 and reconnect response 15 supplied by the controlled peer.
- **Client-side observations:** fresh world-entry read size 4,656 bytes; reconnect `readEnterWorldPacket(true)` read size 4,608 bytes; authentication reset; protocol preservation; temporary-key clearing; fresh client/server ISAAC instances; RSA/XTEA boundaries; server-key `+50` transform; and client-observed event ordering.
- **Client-code facts:** authentication opcode 18 is absent from the revision-742 authentication protocol registry; a separate ISAAC-framed game-channel opcode 18 exists; and that game-channel packet is not claimed as part of the reconnect trace unless captured there.
- **Unknown production-server behavior:** whether production sends response 15, production response/payload ordering, cipher transition, authentication/session ownership, transport handoff/adoption, resumed reads/writes, lifecycle behavior beyond #477, and resynchronization payload/order.

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
- Characterization evidence remains assigned to the correct request-trace, controlled-peer-stimulus, client-observation, client-code-fact, or unknown-production category.
- Unknown production reconnect behavior remains explicitly deferred.
- Issue #478 remains open and is not claimed as closed by this change.

## Stop conditions

- Do not add a reconnect handler, response message/encoder, abort/error response, authentication flow, session lookup, token, cipher transition, transport handoff, lifecycle change, or resynchronization payload.
- Do not modify Raido production behavior or replace #477's reconnect/grace mechanism.
- Do not turn controlled-peer stimuli, client observations, or client-code facts into production-server requirements.
- Report any required production behavior as follow-up work for issue #478.
