## Context

Completed GameClient #142 work provides distinct revision-742 request-trace and client-source evidence. The client trace shows handshake opcode 14, world authentication opcode 16 with flag 0 for fresh login and flag 1 for reconnect, and no authentication-header opcode 18. Client source inspection shows no authentication opcode 18 entry in the revision-742 authentication registry and a separate ISAAC-framed game-channel opcode 18. Issue #478 remains blocked on the server-side response, cipher, session, transport, lifecycle, and resynchronization contract.

## Goals / Non-Goals

**Goals:**

- Remove the stale authentication opcode-18 registration.
- Preserve the opcode-16 reconnect flag as explicit request intent.
- Prevent reconnect intent from entering `HandshakeHub.SignInWorld`.
- Preserve the secret-safe characterization with request-trace facts, controlled-peer stimuli, client-side observations, client-code facts, and unknown production behavior kept separate.
- Leave #477 and all Raido production ownership/lifetime behavior unchanged.

**Non-Goals:**

- A reconnect handler, response 15 implementation, authentication/session orchestration, cipher transition, transport handoff, lifecycle behavior, or resynchronization.
- Any change to the separate ISAAC-framed game-channel opcode 18.
- A Startup refactor or new production testability seam.

## Decisions

### Remove only the stale authentication registration

`Startup` will retain `WorldHandshakeRequestDecoder` on authentication opcode 16 and remove its opcode-18 registration. No replacement authentication handler is added. The game-channel protocol is outside this change.

### Preserve intent with one minimal message type

`WorldHandshakeRequestDecoder` will keep one parser and one common field extraction path. The parsed boolean is named `isReconnect`; it selects `WorldSignInRequest` for false and a sealed `WorldReconnectRequest : ClientSignInRequest` for true. The reconnect message has no additional fields and no handler.

### Rely on exact concrete-type dispatch

Raido's dispatcher indexes handlers by `message.GetType()`. A `WorldReconnectRequest` therefore has no `WorldSignInRequest` descriptor and is ignored until a future #478 implementation supplies an explicit handler. This intentional no-op is safer than authenticating reconnect input as a fresh login. A generic BaseMessage/DerivedMessage regression test covers this existing Raido behavior without GameWorld knowledge.

### Preserve evidence boundaries

The fixture keeps these categories separate:

- Observed client requests and trace facts: handshake opcode 14, authentication opcode 16 with flags 0 and 1, and the absence of authentication-header opcode 18 in the characterized reconnect trace.
- Controlled-peer stimuli: fresh-login response 2 and reconnect response 15.
- Client-side observations: world-entry read sizes, authentication reset, protocol preservation, temporary-key clearing, fresh client/server ISAAC instances, RSA/XTEA boundaries, server-key `+50`, and client-observed event ordering.
- Client-code facts: the absent authentication-registry opcode 18 and the separate ISAAC-framed game-channel opcode 18, which is not claimed as observed in the reconnect trace.
- Unknown production-server behavior: production response acceptance, response/payload ordering, cipher transition, authentication/session ownership, transport handoff/adoption, resumed reads/writes, lifecycle behavior beyond #477, and resynchronization payload/order.

Response 15 is a controlled-peer stimulus, not a client-side observation. The 4,608-byte read is a client-side observation, not a production payload requirement. RSA/XTEA ciphertext and ISAAC keys remain excluded.

## Risks / Trade-offs

- [Reconnect input is temporarily ignored] → This is intentional until #478 establishes response and failure behavior; it prevents accidental fresh-login authentication.
- [A client-code opcode 18 is mistaken for an authentication opcode] → Keep authentication registry/trace facts separate from the ISAAC-framed game channel.
- [A controlled-peer stimulus or client observation is treated as a server contract] → Qualify every fixture key and OpenSpec scenario.

## Migration Plan

No migration or rollout is required. The only runtime changes are the request-side registration and intent distinction.
